using System;

namespace PESpy
{
    public static partial class FileOverview
    {
        public class NativeSymbol
        {
            public int RVA { get; }

            public string Name { get; } //The symbol file is going to be closed so we need to save the name

            public int Displacement { get; }

            internal NativeSymbol(int rva, SymString name, int displacement)
            {
                RVA = rva;
                Name = name.ToString();
                Displacement = displacement;
            }

            public override string ToString()
            {
                if (Name == null)
                    return $"Unknown (0x{RVA:X})";

                using var builder = new ValueStringBuilder();

                builder.Append(Name);

                if (Displacement != 0)
                {
                    if (Displacement < 0)
                    {
                        builder.Append('-');
                        builder.AppendHex((uint) Math.Abs(Displacement));
                    }
                    else
                    {
                        builder.Append('+');
                        builder.AppendHex((uint) Displacement);
                    }
                }

                builder.Append(" (");
                builder.Append("0x");
                builder.AppendHex((uint) RVA);
                builder.Append(")");

                return builder.ToString();
            }
        }
    }
}
