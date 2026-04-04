using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PESpy.View;

namespace PESpy.Tests
{
    //Tests where we perform assertions that we're confirming to our required rules

    [TestClass]
    public class AssemblyTests
    {
        [TestMethod]
        public void AssertAllStructuresAreTested()
        {
            bool ShouldExclude(Type t)
            {
                if (t.Namespace == "PESpy.Ecma335" && t.Name.EndsWith("Row"))
                    return true;

                if (t.IsNested)
                    return true;

                if (t.IsGenericType)
                    return true;

                return false;
            }

            var expected = typeof(PEFile).Assembly.GetTypes()
                .Where(t => typeof(IValue).IsAssignableFrom(t) && !t.IsInterface && !ShouldExclude(t))
                .Select(v => v.Name)
                .ToArray();

             var actual = GetType().Assembly.GetTypes()
                .SelectMany(t => t
                    .GetMethods()
                    .Where(m => m.GetCustomAttribute<TestMethodAttribute>() != null && m.Name.Contains("_"))
                    .Select(m => m.Name))
                .ToArray();

            var missing = expected.Where(k => !actual.Any(a => a.StartsWith($"{k}_") || a.EndsWith($"_{k}") || a.Contains($"_{k}_"))).OrderBy(v => v).ToArray();

            if (missing.Length > 0)
            {
                Assert.Fail($"The following structs are not being tested:" + Environment.NewLine + Environment.NewLine + string.Join(Environment.NewLine, missing));
            }
        }

        [TestMethod]
        public void AssertAllArraysAreAllowed()
        {
            //Properties should not return arrays unless explicitly permitted (e.g. CvFileCheckSum[]) and should instead
            //return custom list/collection types to avoid allocations
            
            var types = typeof(PEFile).Assembly.GetTypes()
                .Where(t => (typeof(IValue).IsAssignableFrom(t) || typeof(IFile).IsAssignableFrom(t)) && !t.IsInterface)
                .ToArray();

            var arrayProperties = types.SelectMany(t => t.GetProperties()).Where(p => p.PropertyType.IsArray).OrderBy(p => p.DeclaringType.Name).ThenBy(p => p.Name).ToArray();

            if (arrayProperties.Length > 0)
            {
                Assert.Fail($"The following {arrayProperties.Length} properties are using illegal arrays:" + Environment.NewLine + Environment.NewLine + string.Join(Environment.NewLine, arrayProperties.Select(p => $"{p.DeclaringType.Name}.{p.Name}")));
            }
        }

        [TestMethod]
        public void AssertAllXRefsTested()
        {
            //For each test where T in TestStruct<T> has a property that implements IRVA or IVA, the test
            //calling TestStruct<T> should also have a method TestXRefs<T> that validates that all XRefs
            //were indeed written

            var nameToTypeMap = typeof(PEFile).Assembly.GetTypes().Where(t => t.Namespace != null && !t.Namespace.Contains("Native")).GroupBy(t =>
            {
                if (t.DeclaringType == null)
                    return t.Name;

                var parts = new List<string>();

                var current = t;

                while (current != null)
                {
                    parts.Add(current.Name);
                    current = current.DeclaringType;
                }

                parts.Reverse();

                return string.Join(".", parts);
            }).ToDictionary(g => g.Key, g => g.ToArray());

            //Some types might have multiple overloads; as long as one test does cover the xrefs, it's all good
            var withXRefs = new HashSet<string>();
            var withoutXRefs = new HashSet<string>();

            var allXRefProperties = new List<PropertyInfo>();

            WithSemanticModels(semanticModel =>
            {
                var methods = semanticModel.SyntaxTree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().ToArray();

                foreach (var method in methods)
                {
                    string typeArg = null;

                    foreach (var invocation in method.DescendantNodes().OfType<InvocationExpressionSyntax>())
                    {
                        if (invocation.Expression is GenericNameSyntax g && g.Identifier.Text == "TestStruct" && g.TypeArgumentList.Arguments.Count == 1)
                        {
                            typeArg = g.TypeArgumentList.Arguments[0].ToString();
                            break;
                        }
                    }

                    if (typeArg == null)
                        continue;

                    var type = nameToTypeMap[typeArg].Single();

                    var xrefProperties = type.GetProperties().Where(p => typeof(IRVA).IsAssignableFrom(p.PropertyType) || typeof(IVA).IsAssignableFrom(p.PropertyType)).ToArray();

                    if (xrefProperties.Length > 0)
                    {
                        allXRefProperties.AddRange(xrefProperties);

                        //Assert that TestXRefs is being called

                        var hasTestXref = method.DescendantNodes().OfType<InvocationExpressionSyntax>().Where(i => i.Expression is GenericNameSyntax g && g.Identifier.Text == "TestXRefs").Any();

                        if (hasTestXref)
                            withXRefs.Add(typeArg);
                        else
                            withoutXRefs.Add(typeArg);
                    }
                }
            }, "PESpy.Tests");

            var str = string.Join(Environment.NewLine, allXRefProperties.Select(v => $"{v.DeclaringType.Name}.{v.Name}"));

            var missing = withoutXRefs.Except(withXRefs).OrderBy(v => v).ToArray();

            if (missing.Length > 0)
            {
                Assert.Fail($"The following {missing.Length} methods are not testing their XRefs:" + Environment.NewLine + Environment.NewLine + string.Join(Environment.NewLine, missing));
            }
        }

        [TestMethod]
        public void AssertAllDataDirectoriesViewed()
        {
            //All ImageDataDirectory properties should be used to create directory regions in PEViewWriter.Finalize()

            var compilation = CreateCompilation();

            var peViewWriter = compilation.GetTypeByMetadataName("PESpy.View.PEViewWriter");

            var finalize = (IMethodSymbol) peViewWriter.GetMembers("Finalize")[0];

            var finalizeSyntax = (MethodDeclarationSyntax) finalize.DeclaringSyntaxReferences[0].GetSyntax();

            var semanticModel = compilation.GetSemanticModel(finalizeSyntax.SyntaxTree, true);

            var propertiesUsed = new HashSet<string>();

            foreach (var memberAccess in finalizeSyntax.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
            {
                var symbol = semanticModel.GetSymbolInfo(memberAccess).Symbol as IPropertySymbol;

                if (symbol != null)
                {
                    var name = symbol.Type.Name;

                    if (name == "ImageDataDirectory")
                        propertiesUsed.Add(symbol.ToString());
                }
            }

            var expectedProperties = typeof(PEFile).Assembly.GetTypes()
                .Where(t => !t.IsInterface)
                .SelectMany(t => t.GetProperties())
                .Where(p => p.PropertyType == typeof(ImageDataDirectory))
                .Select(p => $"{p.DeclaringType}.{p.Name}")
                .ToArray();

            var ignore = new[]
            {
                "PESpy.ImageOptionalHeader.NullDirectory"
            };

            var missingProperties = expectedProperties.Except(propertiesUsed).Except(ignore).ToArray();

            if (missingProperties.Length > 0)
            {
                Assert.Fail($"The following {nameof(ImageDataDirectory)} properties are not being written in {nameof(PEViewWriter)}.{nameof(PEViewWriter.Finalize)}" + Environment.NewLine + Environment.NewLine + string.Join(Environment.NewLine, missingProperties));
            }
        }
                        var withChild = type.DescendantNodes().OfType<MethodDeclarationSyntax>().FirstOrDefault(m => m.Identifier.Text == "WriteChild");

                        if (withChild == null)
                            continue;

                        var writes = withChild.DescendantNodes()
                            .OfType<InvocationExpressionSyntax>()
                            .Where(i => i.Expression is MemberAccessExpressionSyntax m && m.Name.Identifier.Text.StartsWith("Write")).ToArray();

                        foreach (var write in writes)
                        {
                            var identifiers = write.DescendantNodes().OfType<IdentifierNameSyntax>();

                            foreach (var identifier in identifiers)
                            {
                                var symbol = m.GetSymbolInfo(identifier);

                                if (symbol.Symbol is IPropertySymbol p)
                                {
                                    //Insert into the argument list of this invocation an identifier for the offset field
                                    modifications[write.ArgumentList] = write.ArgumentList.WithArguments(write.ArgumentList.Arguments.Insert(1, SyntaxFactory.Argument(SyntaxFactory.IdentifierName(p.Name + "Offset")).WithLeadingTrivia(SyntaxFactory.Whitespace(" "))));
                                    break;
                                }
            var regionStack = new Stack<(string regionName, List<object> children)>();
            regionStack.Push((null, new List<object>()));

            var longestEnumName = 0;
            var longestStructName = 0;

            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];

                if (line.Contains("///"))
                {
                    //This is the start of a summary region
                    Debug.Assert(line.Contains("<summary>"));

                    //Read lines until the </summary>

                    for (var j = i + 1; j < lines.Length; j++)
                    {
                        var nextLine = lines[j];

                        if (nextLine.Contains("</summary>"))
                        {
                            //The lines in-between are the summary. We expect a single line (after
                            //factoring in the end line we're now up to)

                            string commentLine;

                            var diff = (j - i) - 1;

                            if (diff == 1)
                            {
                                commentLine = lines[i + 1].TrimStart(' ', '/');
                            }
                            else
                            {
                                //It's a multiline comment, so grab all lines up to the </summary>

                                var strBuilder = new StringBuilder();

                                for (var k = 0; k < diff; k++)
                                {
                                    var str = lines[k + i + 1].TrimStart(' ', '/');

                                    strBuilder.Append(str);

                                    if (k < diff - 1)
                                        strBuilder.Append(' ');
                                }

                                commentLine = strBuilder.ToString();
                            }

                            //And the next line is the enum field
                            var enumValue = lines[j + 1].Trim(' ', ',');
                            Debug.Assert(!enumValue.Contains("///"));

                            if (enumValue.StartsWith("//"))
                            {
                                i = j + 1;
                                continue; //Commented out; ignore
                            }

                            void tryExtractViewType(string name, ViewType viewType)
                            {
                                var crefs = Regex.Matches(commentLine, "<see cref=\"(.+?)\"/>");

                                if ((crefs.Count == 2 || (crefs.Count == 3 && crefs[1].Groups[1].Value == "IntPtr")) && crefs[0].Groups[1].Value == name)
                                {
                                    var structKind = crefs[1].Groups[1].Value.Replace("PESpy.", string.Empty);

                                    regionStack.Peek().children.Add((enumValue, structKind, viewType));

                                    longestEnumName = Math.Max(longestEnumName, enumValue.Length);
                                    longestStructName = Math.Max(longestStructName, structKind.Length);
                                }
                                else if (crefs.Count == 3 && crefs[0].Groups[1].Value == name && crefs[1].Groups[1].Value == "NativeSpan{T}")
                                {
                                    var structKind = "NativeSpan<" + crefs[2].Groups[1].Value.Replace("PESpy.", string.Empty) + ">";

                                    regionStack.Peek().children.Add((enumValue, structKind, viewType));

                                    longestEnumName = Math.Max(longestEnumName, enumValue.Length);
                                    longestStructName = Math.Max(longestStructName, structKind.Length);
                                }
                            }

                            //This will also filter out the fact there's a summary on the ViewKind type itself
                            if (commentLine.Contains("IStructView"))
                            {
                                tryExtractViewType("IStructView", ViewType.Struct);
                            }
                            else if (commentLine.Contains("IValueView"))
                            {
                                tryExtractViewType("IValueView", ViewType.Value);
                            }
                            else if (commentLine.Contains("ByteBlob"))
                            {
                                regionStack.Peek().children.Add((enumValue, (string) null, ViewType.ByteBlob));
                            }

                            i = j + 1;

                            break;
                        }
                    }
                }
                else if (line.Contains("#region"))
                {
                    var index = line.IndexOf("#region");

                    var regionName = line.Substring(index + 8);
                    var region = (regionName, new List<object>());
                    regionStack.Peek().children.Add(region);
                    regionStack.Push(region);
                }
                else if (line.Contains("#endregion"))
                {
                    regionStack.Pop();
                }
                else
                {
                    var region = regionStack.Peek();

                    if (region.regionName == "Symbols" || region.regionName == "Types" || region.regionName == "Metadata Rows")
                    {
                        //When we're in the symbols region, any enums we find we should immediately treat as being an IStructView to the type
                        //indicated by their enum name
                        if (line.EndsWith(','))
                        {
                            var enumValue = line.Trim(' ', ',');

                            if (enumValue.StartsWith("//"))
                                continue; //Commented out; ignore

                            //The JIT is smart and even though we've got a switch expression, it will detect we have the same case
                            //and optimize the check
                            region.children.Add((enumValue, "IStructView", ViewType.Struct));
                        }
                    }
                    else
                    {
                        if (line.EndsWith(','))
                        {
                            var enumValue = line.Trim(' ', ',');

                            if (enumValue.StartsWith("//"))
                                continue; //Commented out; ignore

                            switch (enumValue)
                            {
                                case "XFG":
                                    region.children.Add((enumValue, "IValueView", ViewType.Value));
                                    break;

                                case "ExDllCharacteristics":
                                    region.children.Add((enumValue, "IMAGE_DLLCHARACTERISTICS_EX", ViewType.Value));
                                    break;

                                case nameof(ViewKind.ImageExportDirectory_Name):
                                case nameof(ViewKind.ImageExportDirectory_AddressOfNames_Entry):
                                case nameof(ViewKind.ImageExportDirectory_AddressOfFunctions_Entry):
                                case nameof(ViewKind.ImageExportDirectory_AddressOfNameOrdinals_Entry):
                                case nameof(ViewKind.ImageExportDirectory_AddressOfNames_Name):
                                case nameof(ViewKind.ImageDelayLoadDescriptor_DllNameRVA):
                                case nameof(ViewKind.ImageExportDirectory_ForwarderName):
                                case nameof(ViewKind.ImageImportDescriptor_Name):
                                case nameof(ViewKind.Manifest):
                                    region.children.Add((enumValue, string.Empty, ViewType.Value));
                                    break;
                            }
                        }
                    }
                }
            }

            Debug.Assert(regionStack.Count == 1);

            //Now we just need to traverse the tree and construct an expression to write each value

            var globalRegion = regionStack.Pop();

            var builder = new StringBuilder();

            var lastWriteWasRegion = false;

            ProcessRegion(globalRegion.regionName, globalRegion.children);

            void ProcessRegion(string regionName, List<object> children)
            {
                if (children.Count == 0)
                    return;

                if (regionName != null)
                    builder.AppendLine("                #region " + regionName).AppendLine();

                for (var i = 0; i < children.Count; i++)
                {
                    var child = children[i];

                    if (child is (string childRegionName, List<object> childChildren))
                    {
                        if (!lastWriteWasRegion && builder.Length > 0 && childChildren.Count > 0)
                            builder.AppendLine();

                        lastWriteWasRegion = false;

                        ProcessRegion(childRegionName, childChildren);
                    }
                    else
                    {
                        if (lastWriteWasRegion)
                        {
                            builder.AppendLine();
                            lastWriteWasRegion = false;
                        }

                        var (enumValue, structKind, viewType) = ((string, string, ViewType)) child;

                        switch (enumValue)
                        {
                            case "ProdItem":
                            case "UnwindCode":
                                continue; //Complex and not top level
                        }

                        string skip = string.Empty;

                        switch (enumValue)
                        {
                            //Complex and we're skipping them for now
                            case "MessageResourceBlock":
                            case "FuncInfoHeader":
                            case "ImageDynamicRelocationV2":
                            case "ImageFunctionOverrideHeader":
                            case "ImageEpilogueDynamicRelocationHeader":
                            case "ImageCorILMethodSect":
                            case "ImageCorILMethodSectEHClause":
                            case "StorageStream":
                            case "CompressedModelHeap":
                            case "StringPoolHeap":
                            case "USBlobPoolHeap":
                            case "BlobPoolHeap":
                            case "GuidPoolHeap":
                            case "RTTIBaseClassArray":
                            case "RTTIClassHierarchyDescriptor":
                            case "RTTICompleteObjectLocator":
                            case "SC20":
                            case "SC40":
                            case "SC":
                            case "SC2":
                                skip = "//";
                                break;
                        }

                        builder.Append($"                {skip}ViewKind.").Append(enumValue.PadRight(longestEnumName)).Append(" => ");

                        switch (enumValue)
                        {
                            //Special case complex types

                            case nameof(ViewKind.RichHeader):
                            case nameof(ViewKind.ImageResourceDirectory):
                            case nameof(ViewKind.ImageResourceDirectoryEntry):
                            case nameof(ViewKind.ImageResourceDataEntry):
                            case nameof(ViewKind.UnwindCode):
                            case nameof(ViewKind.ImageThunkData):
                            case nameof(ViewKind.StorageHeader):
                            case nameof(ViewKind.StreamTable):
                            case nameof(ViewKind.Modi60Persist):
                            case nameof(ViewKind.OMFFileIndex):
                            case nameof(ViewKind.PdbChecksum):
                            case nameof(ViewKind.EmbeddedPortablePdb):
                            case nameof(ViewKind.PogoData):
                            case nameof(ViewKind.CoffSymbolTable):
                            case nameof(ViewKind.ImageSymbol):
                            case nameof(ViewKind.BundleFileEntry):
                            case nameof(ViewKind.BundleFileEntryFixed):
                                builder.AppendLine($"Get{enumValue}(chunk, viewWriter),");
                                break;

                            case "AppHostSignature":
                                builder.AppendLine("Write(chunk.PEFile().AppHostSignature, viewWriter),");
                                break;

                            case "SectionContribsV40":
                            case "SectionContribsV60":
                                builder.AppendLine($"Write(({structKind}) chunk.PDBFile().DBI.SectionContribs, viewWriter),");
                                break;

                            case "DbgDataHdr":
                                builder.AppendLine($"Write(chunk.PDBFile().DBI.DbgHdr, viewWriter),");
                                break;

                            case "PDBStream":
                            case "PDBStream70":
                                builder.AppendLine($"Write(({structKind}) chunk.PDBFile().PDB.PDBHeader, viewWriter),");
                                break;

                            case "StreamNameTable":
                                builder.AppendLine($"Write(chunk.PDBFile().PDB.StreamNameTable, viewWriter),");
                                break;

                            case "XFG":
                                builder.AppendLine("viewWriter.NewValue(chunk.AbsoluteOffset, chunk.PeekUInt64(0), sizeof(long), kind),");
                                break;

                            case "GuardAddressTakenIatEntryTable":
                            case "GuardCFFunctionTable":
                            case "GuardEHContinuationTable":
                            case "GuardLongJumpTargetTable":
                                builder.AppendLine($"Write(chunk.PEFile().LoadConfigTable.{enumValue}.Value, viewWriter),");
                                break;

                            default:
                                if (viewType == ViewType.Value)
                                {
                                    //Peek and cast the appropriate unsigned size
                                    string peekKind;
                                    string size;

                                    switch (enumValue)
                                    {
                                        case "CvSignature":
                                        case "ExDllCharacteristics":
                                            peekKind = "UInt32";
                                            size = "int";
                                            break;

                                        case nameof(ViewKind.ImageExportDirectory_AddressOfNames_Entry):
                                        case nameof(ViewKind.ImageExportDirectory_AddressOfFunctions_Entry):
                                        case nameof(ViewKind.ImageExportDirectory_AddressOfNameOrdinals_Entry):
                                            peekKind = "Int32";
                                            size = "int";
                                            break;

                                        default:
                                            switch (structKind)
                                            {
                                                case "long":
                                                    peekKind = "Int64";
                                                    size = "long";
                                                    break;

                                                case "ulong":
                                                    peekKind = "UInt64";
                                                    size = "ulong";
                                                    break;

                                                case "NativeSpan<int>":
                                                    builder.AppendLine("viewWriter.NewValue(chunk.AbsoluteOffset, chunk.PeekNativeSpan<int>(0, length / 4), length, kind),");
                                                    continue;

                                                case "AnsiString":
                                                    builder.AppendLine("WriteAnsiNullTerminated(chunk, viewWriter, kind),");
                                                    continue;

                                                case "FixedUtf8String":
                                                    builder.AppendLine("WriteFixedUtf8String(chunk, viewWriter, length, kind),");
                                                    continue;

                                                case "IntPtr":
                                                    builder.AppendLine($"viewWriter.NewValue(chunk.AbsoluteOffset, chunk.PeekPointer(0), chunk.PointerSize, kind),");
                                                    continue;

                                                default:
                                                    throw new NotImplementedException();
                                            }
                                            break;
                                    }

                                    //builder.Append($"");
                                    var cast = structKind == string.Empty ? string.Empty : $"({structKind}) ";
                                    builder.AppendLine($"viewWriter.NewValue(chunk.AbsoluteOffset, {cast}chunk.Peek{peekKind}(0), sizeof({size}), kind),");
                                }
                                else if (viewType == ViewType.ByteBlob)
                                {
                                    builder.AppendLine("GetBytes(chunk, viewWriter, length, kind),");
                                }
                                else
                                {
                                    Debug.Assert(viewType == ViewType.Struct);

                                    if (regionName == "Symbols")
                                        builder.AppendLine("WriteSymbol(chunk, viewWriter),");
                                    else if (regionName == "Types")
                                        builder.AppendLine("WriteType(chunk, viewWriter),");
                                    else if (regionName == "Metadata Rows")
                                        builder.AppendLine("WriteRow(chunk, viewWriter, kind),");
                                    else
                                        builder.AppendLine($"Write({($"new {structKind}(chunk),").PadRight(longestStructName + 12)} viewWriter),");
                                }
                                break;
                        }
                    }
                }

                if (regionName != null)
                {
                    builder.AppendLine().AppendLine("                #endregion");
                    lastWriteWasRegion = true;
                }
                else
                {
                    lastWriteWasRegion = false;
                }
            }

            builder.AppendLine();
            builder.AppendLine("                _ => throw new InvalidOperationException($\"Don't know how to handle kind '{kind}'\")");

            var result = builder.ToString().TrimEnd();
            var done = false;

            for (var i = 0; i < viewProviderLines.Count; i++)
            {
                var line = viewProviderLines[i];

                if (line.Contains("CreateStructView"))
                {
                    for (var j = i + 1; j < viewProviderLines.Count; j++)
                    {
                        var nextLine = viewProviderLines[j];

                        if (nextLine.Contains("return kind switch"))
                        {
                            var startLine = j + 1;

                            //Continue until we find the closing };

                            for (var k = j + 1; k < viewProviderLines.Count; k++)
                            {
                                nextLine = viewProviderLines[k];

                                if (nextLine.Contains("};"))
                                {
                                    var endLine = k - 1;

                                    viewProviderLines.RemoveRange(startLine + 1, endLine - startLine);

                                    var linesToInsert = result.Split(Environment.NewLine);
                                    viewProviderLines.InsertRange(startLine + 1, linesToInsert);

                                    done = true;
                                    break;
                                }
                            }

                            if (done)
                                break;
                        }
                    }
                }

                if (done)
                    break;
            }

            File.WriteAllLines(path, viewProviderLines.ToArray());
        }
        private void WithSemanticModels(Action<SemanticModel> action, string projectName = "PESpy")
        {
            var compilation = CreateCompilation(projectName);

            foreach (var syntaxTree in compilation.SyntaxTrees)
            {
                var semanticModel = compilation.GetSemanticModel(syntaxTree);

                action(semanticModel);
            }
        }

        private CSharpCompilation CreateCompilation(string projectName = "PESpy")
        {
            var solutionDir = Path.GetFullPath(Path.Combine(typeof(AssemblyTests).Assembly.Location, "..\\..\\..\\..\\..\\"));

            var projectDir = Path.Combine(solutionDir, projectName);

            var files = Directory.EnumerateFiles(projectDir, "*.cs", SearchOption.AllDirectories);

            var syntaxTrees = files.Where(f => !f.Contains("\\obj\\") && !f.Contains("\\Native\\")).Select(f => CSharpSyntaxTree.ParseText(File.ReadAllText(f), path: f)).ToArray();

            var compilation = CSharpCompilation.Create("PESpy", syntaxTrees);

            return compilation;
        }
    }
}
