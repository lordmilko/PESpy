@{
    ####################
    ####   Global   ####
    ####################

    # Required. The name of the project/GitHub repository
    Name = 'PESpy'

    # Required. The prefix to use for all build environment cmdlets
    CmdletPrefix = 'PE'

    # Required. The copyright author and year to display in the build environment
    Copyright = 'lordmilko, 2026'

    # Optional. The name of the Visual Studio Solution. Required when a project contains multiple solutions
    # SolutionName = ''

    # Optional. A wildcard expression indicating the projects that should be built in CI
    # BuildFilter = ''

    # Optional. The target framework that is used in debug mode when the project conditionally multi-targets only on Release
    # DebugTargetFramework = ''

    # Optional. Features to enable in the build environment. By default all features are allowed, and can be negated with ~. Valid values include: Dependency, Build, Test, Coverage, Package, Version
    Features = @('~Coverage', '~Dependency')

    # Optional. Commands to enable in the build environment. By default all commands are allowed, and can be negated with ~. Valid values include: CommandList, Coverage, ClearBuild, GetVersion, GitStatus, InstallDependency, InvokeBuild, InvokePSAnalyzer, InvokeTest, LaunchModule, Log, NewPackage, SimulateCI, OpenWiki, SetVersion, TestResult, UpdateVersion
    Commands = '~InvokePSAnalyzer'

    # Optional. The value to use for the prompt in the build environment. If not specified, Name will be used
    # Prompt = ''

    # Optional. The name of the folder that the source code is contained in. If not specified, will automatically be calculated
    # SourceFolder = ''

    # Optional. The minimum coverage threshold that must be met under CI
    # CoverageThreshold = ''

    ####################
    ####   CSharp   ####
    ####################

    # Optional. Files to exclude from the C# NuGet Package when building legacy packages
    # CSharpLegacyPackageExcludes = @()

    ####################
    #### PowerShell ####
    ####################

    # Optional. Indicates that a PowerShell package should be built containing both coreclr and fullclr subfolders
    PowerShellMultiTargeted = $true

    # Optional. The name of the PowerShell module. If not specified, Name will be used
    # PowerShellModuleName = ''

    # Optional. The name of the PowerShell project. If not specified, will automatically be calculated
    # PowerShellProjectName = ''

    # Optional. A ScriptBlock that takes a FileInfo/DirectoryInfo as $_ and returns whether or not to process unit tests for that file/folder
    # PowerShellUnitTestFilter = $null

    ####################
    ####    Test    ####
    ####################

    # Optional. The languages to perform unit tests for. If not specified, C# and PowerShell will be tested
    TestTypes = @('C#')

    # Optional. The name of the Unit Test project. If not specified, will automatically be calculated
    # UnitTestProjectName = ''

    ####################
    ####   Package  ####
    ####################

    # Optional. The types of packages to produce. If not specified, C#/PowerShell *.nupkg and Redist *.zip files will be produced
    PackageTypes = @('C#','PowerShell')

    # Optional. The tests to perform for each type of package
    PackageTests = @{
        # We can't test C# because we depend on ClrDebug which we won't be able to import into PowerShell since it's not part of our nupkg
        "C#" = @{ kind="skip" }
        "PowerShell"=@(
            @{ command = "(Get-PEFile C:\Windows\system32\ntdll.dll).OptionalHeader.Magic"; result = "IMAGE_NT_OPTIONAL_HDR64_MAGIC"; kind = "Cmdlet" }
            @{ command = "Get-PEFile"                                                     ; kind = "CmdletExport" }
        )
    }

    # Optional. The files that are expected to exist in each tyoe of package
    PackageFiles = @{
        "C#"=@(
            "lib\net9.0\PESpy.dll"
            "lib\net9.0\PESpy.xml"
            "lib\netstandard2.0\PESpy.dll"
            "lib\netstandard2.0\PESpy.xml"
            "LICENSE"
            "package\*"
            "_rels\*"
            "PESpy.nuspec",
            "[Content_Types].xml"
        )

        "PowerShell"=@(
            "PESpy.nuspec"
            "PESpy.psd1"
            "PESpy.Types.ps1xml"
            "package\*"
            "_rels\*"
            "[Content_Types].xml"

            @{ Name = "ClrDebug.dll"                              ; Condition = { $_.IsDebug } }
            @{ Name = "PESpy.dll"                                 ; Condition = { $_.IsDebug } }
            @{ Name = "PESpy.PowerShell.dll"                      ; Condition = { $_.IsDebug } }
            @{ Name = "System.Buffers.dll"                        ; Condition = { $_.IsDebug } }
            @{ Name = "System.Management.Automation.dll"          ; Condition = { $_.IsDebug } }
            @{ Name = "System.Memory.dll"                         ; Condition = { $_.IsDebug } }
            @{ Name = "System.Numerics.Vectors.dll"               ; Condition = { $_.IsDebug } }
            @{ Name = "System.Runtime.CompilerServices.Unsafe.dll"; Condition = { $_.IsDebug } }
            @{ Name = "System.Threading.Tasks.Extensions.dll"     ; Condition = { $_.IsDebug } }

            @{ Name = "coreclr\ClrDebug.dll"                              ; Condition = { $_.IsMultiTargeting } }
            @{ Name = "coreclr\PESpy.dll"                                 ; Condition = { $_.IsMultiTargeting } }
            @{ Name = "coreclr\PESpy.PowerShell.dll"                      ; Condition = { $_.IsMultiTargeting } }
            # We don't strictly need these files, but they get brought in due to CopyLocalLockFileAssemblies which we have set to bring in ClrDebug
            # (which _will_ be copied when it's available locally, but wouldn't otherwise be copied when being consumed from a NuGet package)
            @{ Name = "coreclr\System.Buffers.dll"                        ; Condition = { $_.IsMultiTargeting } }
            @{ Name = "coreclr\System.Management.Automation.dll"          ; Condition = { $_.IsMultiTargeting } }
            @{ Name = "coreclr\System.Memory.dll"                         ; Condition = { $_.IsMultiTargeting } }
            @{ Name = "coreclr\System.Numerics.Vectors.dll"               ; Condition = { $_.IsMultiTargeting } }
            @{ Name = "coreclr\System.Runtime.CompilerServices.Unsafe.dll"; Condition = { $_.IsMultiTargeting } }
            @{ Name = "coreclr\System.Threading.Tasks.Extensions.dll"     ; Condition = { $_.IsMultiTargeting } }

            @{ Name = "fullclr\ClrDebug.dll"                              ; Condition = { $_.IsMultiTargeting } }
            @{ Name = "fullclr\PESpy.dll"                                 ; Condition = { $_.IsMultiTargeting } }
            @{ Name = "fullclr\PESpy.PowerShell.dll"                      ; Condition = { $_.IsMultiTargeting } }
            @{ Name = "fullclr\System.Buffers.dll"                        ; Condition = { $_.IsMultiTargeting } }
            @{ Name = "fullclr\System.Management.Automation.dll"          ; Condition = { $_.IsMultiTargeting } }
            @{ Name = "fullclr\System.Memory.dll"                         ; Condition = { $_.IsMultiTargeting } }
            @{ Name = "fullclr\System.Numerics.Vectors.dll"               ; Condition = { $_.IsMultiTargeting } }
            @{ Name = "fullclr\System.Runtime.CompilerServices.Unsafe.dll"; Condition = { $_.IsMultiTargeting } }
            @{ Name = "fullclr\System.Threading.Tasks.Extensions.dll"     ; Condition = { $_.IsMultiTargeting } }
        )

        "Redist"=@(
            "foo"
        )
    }
}
