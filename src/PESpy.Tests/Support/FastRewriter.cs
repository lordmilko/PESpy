using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace PESpy.Tests
{
    class FastRewriter
    {
        private static HashSet<string> ignored = new HashSet<string>
        {
            "ByteBlob"
        };

        private Dictionary<string, TypeDeclarationSyntax> types;

        public FastRewriter(TypeDeclarationSyntax[] types)
        {
            this.types = types.ToDictionary(v => v.Identifier.Text, v => v);
        }

        public void AddFastRegions()
        {
            //For each property, wrap it in an #if FASTPE region
            //Also wrap all ctors in a FASTPE region as well

            var files = GetFilesOfInterest();

            foreach (var file in files)
            {
                ProcessFile(file);
            }
        }

        private void ProcessFile(string file)
        {
            var fileName = Path.GetFileNameWithoutExtension(file);

            if (ignored.Contains(fileName))
                return;

            if (!types.TryGetValue(fileName, out var newType))
                return;

            var newPropertiesAndFields = newType
                .Members
                .Where(v => v is PropertyDeclarationSyntax || v is FieldDeclarationSyntax)
                .ToDictionary(v =>
                {
                    if (v is PropertyDeclarationSyntax p)
                        return p.Identifier.Text;

                    var f = (FieldDeclarationSyntax) v;

                    return f.Declaration.Variables[0].Identifier.Text;
                }, v => v);

            var newCtor = newType.DescendantNodes().OfType<ConstructorDeclarationSyntax>().First();

            var rawText = File.ReadAllText(file);

            if (rawText.Contains("PEFAST"))
                return; //Assume it's already been processed

            var text = SourceText.From(rawText);
            var syntaxTree = CSharpSyntaxTree.ParseText(text);

            var root = syntaxTree.GetRoot();

            var originalType = root.DescendantNodes().OfType<TypeDeclarationSyntax>().First();

            var properties = originalType.Members.OfType<PropertyDeclarationSyntax>();
            var ctors = originalType.Members.OfType<ConstructorDeclarationSyntax>().ToArray();

            var changes = new Dictionary<SyntaxNode, SyntaxNode>();

            foreach (var property in properties)
            {
                var fullMemberName = $"{fileName}.{property.Identifier.Text}";

                switch (fullMemberName)
                {
                    case "ImageExportDirectory.Exports":
                        continue;
                }

                if (property.Identifier.Text == "Offset")
                {
                    var newProp = ParseMemberDeclaration(@"
#if PEFAST
        public RawOffset Offset => chunk.AbsoluteOffset;
#else
        public RawOffset Offset { get; }
#endif
");

                    newProp = newProp.WithTrailingTrivia(newProp.GetTrailingTrivia().Add(Trivia(EndIfDirectiveTrivia(false))).Add(EndOfLine(Environment.NewLine)));

                    changes[property] = newProp;

                    continue;
                }

                var existingLeadingTrivia = property.GetLeadingTrivia();
                existingLeadingTrivia = existingLeadingTrivia.RemoveAt(existingLeadingTrivia.Count - 1) //Remove the space at the end
                    .Add(Trivia(IfDirectiveTrivia(IdentifierName(" PEFAST"), true, true, true)))
                    .Add(EndOfLine(Environment.NewLine))
                    .Add(Whitespace("        "));

                var newProperty = newPropertiesAndFields[property.Identifier.Text];

                newProperty = newProperty
                    .WithLeadingTrivia(existingLeadingTrivia)
                    .WithTrailingTrivia(
                        TriviaList(
                            EndOfLine(Environment.NewLine),
                            Trivia(ElseDirectiveTrivia(false, false)),
                            EndOfLine(Environment.NewLine),
                            Whitespace("        "),
                            DisabledText(property.WithoutLeadingTrivia().ToFullString()),
                            Trivia(EndIfDirectiveTrivia(false)),
                            EndOfLine(Environment.NewLine)
                        )
                    );

                var str = newProperty.ToFullString();

                changes[property] = newProperty;
            }

            if (ctors.Length == 1)
            {
                //Easy case
                var existingLeadingTrivia = ctors[0].GetLeadingTrivia();
                existingLeadingTrivia = existingLeadingTrivia.RemoveAt(existingLeadingTrivia.Count - 1) //Remove the space at the end
                    .Add(Trivia(IfDirectiveTrivia(IdentifierName(" PEFAST"), true, true, true)))
                    .Add(EndOfLine(Environment.NewLine))
                    .Add(DisabledText("        private readonly MemoryChunk chunk;"))
                    .Add(EndOfLine(Environment.NewLine))
                    .Add(EndOfLine(Environment.NewLine))
                    .Add(Whitespace("        "));

                newCtor = newCtor
                    .WithLeadingTrivia(existingLeadingTrivia)
                    .WithTrailingTrivia(
                        TriviaList(
                            EndOfLine(Environment.NewLine),
                            Trivia(ElseDirectiveTrivia(false, false)),
                            EndOfLine(Environment.NewLine),
                            Whitespace("        "),
                            DisabledText(ctors[0].WithoutLeadingTrivia().ToFullString()),
                            Trivia(EndIfDirectiveTrivia(false)),
                            EndOfLine(Environment.NewLine)
                        )
                    );

                changes[ctors[0]] = newCtor;

                var str = newCtor.ToFullString();
            }
            else
            {
            }

            var result = root.ReplaceNodes(changes.Keys, (a, b) => changes[a]);

            var finalStr = result.ToFullString();

            File.WriteAllText(file, finalStr, Encoding.UTF8);
        }

        private string[] GetFilesOfInterest()
        {
            //Get all files under the Managed folder, excluding certain files

            var solutionDir = Path.GetFullPath(Path.Combine(GetType().Assembly.Location, "..\\..\\..\\..\\..\\"));

            var files = Directory.EnumerateFiles(Path.Combine(solutionDir, "PESpy\\Managed"), "*.cs", SearchOption.AllDirectories);

            return files.ToArray();
        }
    }
}
