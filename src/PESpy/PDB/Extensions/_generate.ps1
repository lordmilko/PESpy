$str = (gc "$PSScriptRoot\_config.json"|where { !$_.ToString().TrimStart().StartsWith("//") }) -join "`r`n"

# Strip comments
$str = $str -replace '(?m)(?<=^([^"]|"[^"]*")*)//.*' -replace '(?ms)/\*.*?\*/'

$config = $str | convertfrom-json

$symbolMap = @{}

$symbolFiles = (gci $PSScriptRoot\..\Symbols *.cs) + (gci $PSScriptRoot\..\Types *.cs)

$typeToFieldMap = @{}

# Get the fields associated with each type
foreach($symbolFile in $symbolFiles)
{
    $lines = gc $symbolFile.FullName

    $rawFields = $lines|where { $_ -like "*public*" -and $_ -notlike "* struct *" -and $_ -notlike "*ToString*" -and !$_.Trim().StartsWith("/*") }

    $pattern = " +public (.+?) (.+?)( .+|$)"

    $fields = $rawFields | foreach {
        [pscustomobject]@{
            Type = $_ -replace $pattern,'$1'
            Name = $_ -replace $pattern,'$2'
        }
    } | where { $_.Name -ne "reclen" -and $_.Name -ne "rectyp" }

    $typeToFieldMap.$($symbolFile.BaseName) = $fields
}

$typeInfoMap = @{}

# Build up a master info object that maps the type name to both its associated SYM_ENUM_e records and the fields that are found on the type

foreach($item in "symbols","types")
{
    foreach($type in $config.$item.psobject.properties.Name)
    {
        if($type -eq "SymType")
        {
            continue
        }

        if(!$typeToFieldMap.ContainsKey($type))
        {
            throw "File '$type.cs' does not exist"
        }

        $fields = $typeToFieldMap.$type
        $enumValues = $config.$item.$type

        if(!$enumValues)
        {
            throw
        }

        $typeInfoMap.$type = [pscustomobject]@{
            Name = $type
            EntityType = [char]::ToUpper($item[0]) + $item.Substring(1)
            EnumValues = $enumValues | sort
            Fields = $fields
        }
    }
}

# Build up a mapping of every field type and name to the parent types that contain that field
$fieldTypeToOwningTypeMap = @{}
$typesWithFieldName = @{}

function AddToMap($map, $key, $value)
{
    $list = $null

    if($map.ContainsKey($key))
    {
        $list = $map.$key
    }
    else
    {
        $list = @()
    }

    $list += $value
    $map.$key = $list
}

foreach($typeName in $typeInfoMap.Keys)
{
    $typeInfo = $typeInfoMap.$typeName

    $fields = $typeInfo.Fields

    foreach($field in $fields)
    {
        AddToMap $fieldTypeToOwningTypeMap $field.Type $typeInfo
        AddToMap $typesWithFieldName $field.name $typeInfo
    }
}

$diaFieldMap = @{}

#ClrDebug must be cloned next to this repo for this command to work

$diaFields = (gc "$PSScriptRoot\..\..\..\..\..\ClrDebug\ClrDebug\Managed\DIA\DiaSymbol.cs"|where { $_ -like "*public*HRESULT*pRetVal*" }) | foreach { $pattern = " +public HRESULT TryGet(.+?)\(out (.+?) pRetVal.*\).*"; [pscustomobject]@{name = $_ -replace $pattern,'$1'; type = $_ -replace $pattern,'$2'} }
$diaFields | foreach { $diaFieldMap.$($_.name) = $_.type }

<#
For each field, intuit the meaning of it. There are several patterns that we use
- CV_fldattr_t.access: For any type that has a field of type "CV_fldattr_t", all enum values associated with that type can get the field by doing ((<Type>) symType).<field>.access
- COMPILESYM.verBuild: For any enum value that is associated with the COMPILESYM type, get the field by doing ((COMPILESYM) symType).verBuild
- .off               : For any type that has a field called "off", all enum values associated with that type can get the field by doing ((<Type>) symType).off
- { value = ".off", condition = "LocIsRegRel" }: for any type that has a field called off whose LocationType is LocIsRegRel. Another scenario you can have is requiring that another field is present
                                                 e.g. an .off is an AddressOffset when a .seg is also present
- { value = "LocIsEnregistered", condition = "S_*REGISTER" }: the constant value LocIsEnregistered is used for any type whose SYM_ENUM_e ends in "REGISTER"

Additionally, the following complex expressions are supported
- !CV_PROCFLAGS.CV_PFLAG_NOFPO                                           : invert the boolean value pointed to by the specified field
- ExpandEncodedBasePointerReg(FRAMEPROCSYM.flags.encodedLocalBasePointer): call the specified function in order to decode the value
- AttrRegRel, BlockSym*                                                  : special cased fields (like Name) that want to know the names of every type that should be targeted
#>

function SanitizeKeyword($str)
{
    if($str -in @("sealed"))
    {
        $str = "@" + $str
    }

    return $str
}

function CreateCaseGroup($typeInfo, $fieldName)
{
    $parameterName = $null

    $dot = $fieldName.LastIndexOf('.')

    if($dot)
    {
        $lastProperty = SanitizeKeyword $fieldName.Substring($dot + 1)
        $fieldName = $fieldName.Substring(0, $dot + 1) + $lastProperty
    }

    if($typeInfo.EntityType -eq "Symbols")
    {
        $parameterName = "symType"
    }
    else
    {
        $parameterName = "lfEasy"
    }

    [pscustomobject]@{
        EnumValues = $typeInfo.EnumValues
        TypeInfo = $typeInfo
        EntityType = $typeInfo.EntityType
        Body = "(($($typeInfo.Name)) $parameterName).$fieldName"
    }
}

function ParseExpression($expr)
{
    if($expr.value)
    {
        if($expr.value.StartsWith("."))
        {
            # A simple field access that is conditional upon another condition being true

            $inner = ParseExpression $expr.value

            # Now we need to reduce this list to the ones that meet the criteria

            $conditions = @($expr.condition)

            foreach($condition in $conditions)
            {
                if($condition.StartsWith("."))
                {
                    # Only types that _also_ have the given field are allowed
                    $fieldName = $condition.Substring(1)

                    $inner = $inner | where { $_.TypeInfo.Fields|where { $_.Name -eq $fieldName } }
                }
                elseif($condition -eq "LocIsRegRel")
                {
                    # Modelling full LocationType in our config file seems to hard because we also need to call into IsProc.
                    # So we're just going to hard code how to handle various location types here. LocIsRegRel matches any symbol type
                    # that is *REL* or *REL32*
                    
                    $toInclude = @()

                    foreach($item in $inner)
                    {
                        $allowed = $item.EnumValues | where { $_ -like "S_*REL" -or $_ -like "S_*_REL_*" -or $_ -like "S_*REL16*" -or $_ -like "S_*REL32*" }

                        if($allowed)
                        {
                            $item.EnumValues = $allowed
                            $toInclude += $item
                        }
                    }

                    $toInclude
                    continue
                }
                elseif($condition -eq "LocIsBitField")
                {
                    $toInclude = @()

                    foreach($item in $inner)
                    {
                        $allowed = $item.EnumValues | where { $_ -like "LF_BITFIELD*" }

                        if($allowed)
                        {
                            $item.EnumValues = $allowed
                            $toInclude += $item
                        }
                    }

                    $toInclude
                    continue
                }
                elseif($condition -eq "LEAF_ENUM_e")
                {
                    $toInclude = $inner | where EntityType -eq "Types"

                    $toInclude
                    continue
                }
                else
                {
                    throw "Don't know how to handle condition '$condition'"
                }
            }

            return
        }
        else
        {
            # It's a constant enum value dependant on one of several possible conditions being true

            $conditions = @($expr.condition)
            $matches = @()

            foreach($condition in $conditions)
            {
                if($condition.StartsWith("S_"))
                {
                    # They want all kinds that match the specified SYM_ENUM_e wildcard

                    foreach($key in $typeInfoMap.Keys)
                    {
                        $typeInfo = $typeInfoMap[$key]

                        $matches += $typeInfo.EnumValues | where { $_ -like $condition }
                    }
                }
                elseif($condition.StartsWith("Is"))
                {
                    # They want all kinds that match the specified category
                    throw
                }
                else
                {
                    throw
                }
            }

            if($matches)
            {
                [pscustomobject]@{
                    EnumValues = $matches
                    TypeInfo = $null
                    EntityType = "Symbols"
                    Body = $expr.value
                }
            }
        }
    }
    elseif($expr.StartsWith("."))
    {
        $exprs = @($expr)

        foreach($item in $exprs)
        {
            $fieldName = $item.Substring(1)

            # Simple field access. Anyone type that contains a field with this name is supported
            $matches = $typesWithFieldName.$fieldName

            if(!$matches)
            {
                throw "Could not find any fields with field '$fieldName'"
            }

            foreach($typeInfo in $matches)
            {
                CreateCaseGroup $typeInfo $fieldName
            }
        }
    }
    elseif($expr.StartsWith("!"))
    {
        # Negated sub-expression

        $results = ParseExpression $expr.Substring(1)

        @($results) | foreach {
            $_.Body = "!" + $_.Body
        }

        $results
    }
    else
    {
        # Either the name of a type, or the name of a type that one of our fields has

        $dot = $expr.IndexOf(".")

        if($dot -eq -1)
        {
            throw "Expected a field access"
        }

        $typeName = $expr.Substring(0, $dot)
        $fieldName = $expr.Substring($dot + 1)

        if($typeInfoMap.ContainsKey($typeName))
        {
            # The type refers to "this" SymType. All enums that are represented by this type are supported
            $typeInfo = $typeInfoMap.$typeName

            CreateCaseGroup $typeInfo $fieldName
        }
        elseif($fieldTypeToOwningTypeMap.ContainsKey($typeName))
        {
            # The type refers to a field that a given SymType might have. Get all types that have a field of this type

            $typeInfos = $fieldTypeToOwningTypeMap.$typeName

            foreach($typeInfo in $typeInfos)
            {
                # Get the field that is of the type
                $field = $typeInfo.Fields | where Type -eq $typeName

                if(!$field)
                {
                    throw
                }

                CreateCaseGroup $typeInfo "$($field.Name).$fieldName"
            }
        }
        else
        {
            throw "Could not figure out what type '$typeName' refers to"
        }
    }
}

function GetDiaNumber($name)
{
    switch($name)
    {
        { $_ -in "FramePadOffset","FramePadSize" } {
            "9"
        }
    }
}

foreach($field in $config.properties.PSObject.properties.Name)
{
    write-host "Processing $field"

    $sources = @($config.properties.$field)

    if ($sources.Count -eq 0)
    {
        # field is not yet implemented
        continue
    }

    $caseGroups = @()

    $defaultCases = @()

    if($field -eq "Name")
    {
        # Name is special cased; sources is a list of types that contain a name
        Write-Host "Processing Name is not implemented"
    }
    else
    {
        foreach($source in $sources)
        {
            $expr = ParseExpression $source

            $caseGroups += $expr

            $childKind = $null

            if($source -like "*FRAMEPROCSYM*")
            {
                $childKind = "S_FRAMEPROC"
            }
            elseif($source -like "*POGOINFO*")
            {
                $childKind = "S_POGODATA"
            }

            if($childKind)
            {
                # Any expression that targets FrameProcSym may also apply when you're looking at the parent function
                $defaultCases += [pscustomobject]@{
                    EntityType = $expr.EntityType
                    Body = @"
if (symType.IsProc() && TryGetChild((BlockSym) symType, $childKind, out var child))
{{
    {0} = $($expr.Body.Replace("symType", "child"));
    return true;
}}

break;
"@
                }
            }
        }
    }

    if($caseGroups.Count -gt 0)
    {
        # Split the body out based on whether it's for SymType or TypType

        $groups = $caseGroups | sort { $_.EnumValues | select -first 1 } | group { $_.EntityType }

        $builder = New-Object System.Text.StringBuilder

        $strs = @()
        $statics = @()

        foreach($group in $groups)
        {
            $entityCases = $group.Group

            $enumType = $null
            $entityTypeName = $null
            $extensionTypeName = $null
            $entityTypeParameter = $null
            $recordTypeExpr = $null
            $diaIface = "IDiaSymbol$(GetDiaNumber $field)"

            if($field.StartsWith("PGO"))
            {
                $lowerFieldName = $field
            }
            else
            {
                $lowerFieldName = SanitizeKeyword ([char]::ToLower($field[0]) + $field.Substring(1))
            }

            $fieldType = $diaFieldMap.$field

            if($fieldType -eq "DiaSymbol" -and ($field -like "*type*" -or $field -like "*class*"))
            {
                $fieldType = "TypOrEnumType"
            }

            if($group.Name -eq "Symbols")
            {
                $enumType = "SYM_ENUM_e"
                $entityTypeName = "SymType"
                $extensionTypeName = "SymType"
                $entityTypeParameter = "symType"
                $recordTypeExpr = "symType.rectyp"
            }
            else
            {
                $enumType = "LEAF_ENUM_e"
                $entityTypeName = "LfEasy"
                $extensionTypeName = "TypType"
                $entityTypeParameter = "lfEasy"
                $recordTypeExpr = "lfEasy.leaf"
            }

            $indent = "                "

            $body = ($entityCases | foreach {
                $caseGroup = $_

                $cases = ($caseGroup.EnumValues | foreach { "$($indent)case $($_):" })

                $builder.Clear() | Out-Null

                # If we're an enum we aren't going to have a specific TypeInfo
                if($caseGroup.TypeInfo)
                {
                    $builder.AppendLine("$indent//$($caseGroup.TypeInfo.Name)") | Out-Null
                }

                foreach($case in $cases)
                {
                    $builder.AppendLine($case) | Out-Null
                }

                $body = $caseGroup.Body

                if(!$caseGroup.TypeInfo)
                {
                    # It's an enum
                    $body = "$fieldType.$($body)"
                }

                if($field -eq "Offset" -and $caseGroup.EntityType -eq "Types")
                {
                    $body = "(int) $body"
                }

                $builder.AppendLine("$($indent)    $lowerFieldName = $body;") | Out-Null
                $builder.AppendLine("$($indent)    return true;") | Out-Null

                return $builder.ToString()
            }) -join "`r`n"

            $groupDefaultCases = $defaultCases|where EntityType -eq $group.Name

            if($groupDefaultCases)
            {
                if (@($groupDefaultCases).Count -gt 1)
                {
                    throw
                }

                $builder.Clear() | Out-Null
                $builder.Append($body) | Out-Null
                $builder.AppendLine() | Out-Null
                $builder.AppendLine("$indent$("default:")") | Out-Null

                $split = [string]::Format($groupDefaultCases.Body.Replace("`r", ""), $lowerFieldName) -split "`n"

                foreach($line in $split)
                {
                    if($line.Length -eq 0)
                    {
                        $builder.AppendLine() | Out-Null
                    }
                    else
                    {
                        $builder.AppendLine("$indent    $line") | Out-Null
                    }
                }

                $body = $builder.ToString()
            }

            $body = $body.TrimEnd()

            $statics += $enumType

            $xmlDoc = @"
        /// <summary>
        /// <inheritdoc cref="$diaIface.get_$($lowerFieldName.TrimStart('@'))"/><para/>
        /// Corresponds to <see cref="$diaIface.get_$($lowerFieldName.TrimStart('@'))"/>
        /// </summary>
"@

            if($extensionTypeName -eq "TypType")
            {
                $extra = @"

$xmlDoc
        public static bool TryGet$field(in this TypType typType, out $fieldType $lowerFieldName) =>
            TryGet$field((LfEasy) typType, out $lowerFieldName);

"@
            }
            else
            {
                $extra = $null
            }

            $str = @"
    public static partial class $($extensionTypeName)Extensions
    {$extra
$xmlDoc
        public static bool TryGet$field(in this $entityTypeName $entityTypeParameter, out $fieldType $lowerFieldName)
        {
            switch ($recordTypeExpr)
            {
$body
            }

            $lowerFieldName = default;
            return false;
        }
    }
"@

            $strs += $str
        }

        $fullBody = $strs -join "`r`n`r`n"

        $output = @"
﻿/*************************************************************************
 * This code was generated by a tool.                                    *
 * Please do not modify this file directly - modify _config.json instead *
 *************************************************************************/
using ClrDebug;
using ClrDebug.DIA;
using ClrDebug.PDB;
$(($statics | foreach { "using static ClrDebug.PDB.$_;" }) -join "`r`n")

namespace PESpy.PDB
{
$fullBody
}
"@

        $output | Set-Content "$PSScriptRoot\$field.cs" -Encoding UTF8
    }
}