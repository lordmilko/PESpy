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

            var syntaxTrees = files.Where(f => !f.Contains("\\obj\\") && !f.Contains("\\Native\\")).Select(f => CSharpSyntaxTree.ParseText(File.ReadAllText(f))).ToArray();

            var compilation = CSharpCompilation.Create("PESpy", syntaxTrees);

            return compilation;
        }
    }
}
