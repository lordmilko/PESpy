using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Emit;
using System.Text;

namespace PESpy.Tests
{
    class TypeWriter
    {
        private int indent;
        private bool isNewLine = true;

        private StringBuilder builder = new StringBuilder();
        private StringBuilder captureBodyBuilder = new StringBuilder();

        private string DebuggerDisplay => builder.ToString();

        public void WriteType(StructBuilder structBuilder)
        {
            Indent();

            WriteLine($"public readonly struct {structBuilder.Name}");
            WriteLine("{");
            Indent();

            var fixedOffset = 0;
            var numPointers = 0;

            string GetOffset(int off = 0) //off allows the caller to adjust the known fixed offset
            {
                off += fixedOffset;

                if (numPointers == 0)
                    return off.ToString();

                var builder = new StringBuilder();

                if (off > 0)
                    builder.Append(off).Append(" + ");

                if (numPointers == 1)
                    builder.Append("chunk.PointerSize");
                else
                {
                    if (off > 0)
                        builder.Append("(");

                    builder.Append($"{numPointers} * chunk.PointerSize");

                    if (off > 0)
                        builder.Append(")");
                }

                return builder.ToString();
            }

            static string GetPeekName(FieldBuilder builder)
            {
                if (builder.IsArray)
                    return $"Span<{FieldBuilder.GetDisplayName(builder.Type)}>";

                if (builder.IsPointer)
                    return "Pointer";

                if (builder.Type.IsPrimitive)
                    return builder.Type.Name;

                if (builder.Type.IsEnum)
                    return builder.EnumImpl.Name;

                return null;
            }

            StringBuilder oldBuilder = null;
            var eagerStatements = new List<string>();

            for (var i = 0; i < structBuilder.Fields.Count; i++)
            {
                var field = structBuilder.Fields[i];

                if (field.Eager)
                {
                    WriteLine($"public readonly {field.TypeName} {field.Name};");
                    isNewLine = false;

                    oldBuilder = builder;
                    builder = new StringBuilder();

                    Write($"{field.Name} = ");
                }
                else
                    Write($"public {field.TypeName} {field.Name} => ");

                if (field.Type.IsEnum)
                    Write($"({field.TypeName}) ");

                var peekName = GetPeekName(field);

                if (field.x86Only)
                {
                    WriteLine($"chunk.Is32Bit ? chunk.Peek{peekName}({GetOffset()}) : 0;");
                }
                else if (i > 0 && structBuilder.Fields[i - 1].x86Only)
                {
                    Debug.Assert(field.IsPointer);

                    //When the pointer size is 4, we're after them. Otherwise, we
                    //overlap them
                    var previousSize = structBuilder.Fields[i - 1].Size;
                    WriteLine($"chunk.Is32Bit ? chunk.PeekPointer({GetOffset()}) : chunk.PeekPointer({GetOffset(-previousSize)});");

                    //We're going to increase the offset by 8, so decrease the current offset by 4 so we're not doubling up
                    fixedOffset -= previousSize;
                }
                else
                {
                    if (peekName != null)
                        Write($"chunk.Peek{peekName}({GetOffset()}");
                    else
                        Write($"new {field.TypeName}(chunk.Slice({GetOffset()})");

                    if (field.IsArray)
                        Write(", " + field.NumElems);

                    WriteLine(");");
                }

                if (!field.Eager)
                    WriteLine();

                if (field.IsPointer)
                    numPointers++;
                else
                    fixedOffset += field.Size;

                if (field.Eager)
                {
                    eagerStatements.Add(builder.ToString());
                    builder = oldBuilder;
                    WriteLine();
                }
            }

            WriteLine("private readonly MemoryChunk chunk;");
            WriteLine();

            WriteLine($"internal {structBuilder.Name}(in MemoryChunk chunk)");
            WriteLine("{");
            Indent();

            WriteLine("this.chunk = chunk;");

            foreach (var statement in eagerStatements)
            {
                Write(statement);
                isNewLine = true;
            }

            Dedent();
            WriteLine("}");

            Dedent();
            WriteLine("}");

            Dedent();
        }

        private void Write(string str)
        {
            EnsureIndentation();
            builder.Append(str);
        }

        public void WriteLine()
        {
            builder.AppendLine();
            isNewLine = true;
        }

        public void WriteLine(string str)
        {
            EnsureIndentation();

            builder.AppendLine(str);
            isNewLine = true;
        }

        private void Indent() => indent++;
        private void Dedent() => indent--;

        private void EnsureIndentation()
        {
            if (isNewLine)
            {
                for (var i = 0; i < indent; i++)
                    builder.Append("    ");

                isNewLine = false;
            }
        }

        public override string ToString()
        {
            return builder.ToString();
        }
    }
}
