using ClrDebug.DIA;

#nullable disable

namespace PESpy
{
    public static partial class Demangler
    {
        public class PrimitiveTypeNode : TypeNode
        {
            public PrimitiveKind PrimitiveKind { get; internal set; }

            public PrimitiveTypeNode() : base(NodeKind.PrimitiveType)
            {
            }

            public override void OutputPre(ref Utf8StringBuilder builder, UNDNAME flags)
            {
                var value = PrimitiveKind switch
                {
                    PrimitiveKind.Void => "void",
                    PrimitiveKind.Bool => "bool",
                    PrimitiveKind.Char => "char",
                    PrimitiveKind.Schar => "signed char",
                    PrimitiveKind.Uchar => "unsigned char",
                    PrimitiveKind.Char8 => "char8_t",
                    PrimitiveKind.Char16 => "char16_t",
                    PrimitiveKind.Char32 => "char32_t",
                    PrimitiveKind.Short => "short",
                    PrimitiveKind.Ushort => "unsigned short",
                    PrimitiveKind.Int => "int",
                    PrimitiveKind.Uint => "unsigned int",
                    PrimitiveKind.Long => "long",
                    PrimitiveKind.Ulong => "unsigned long",
                    PrimitiveKind.Int64 => "__int64",
                    PrimitiveKind.Uint64 => "unsigned __int64",
                    PrimitiveKind.Wchar => "wchar_t",
                    PrimitiveKind.Float => "float",
                    PrimitiveKind.Double => "double",
                    PrimitiveKind.Ldouble => "long double",
                    PrimitiveKind.Nullptr => "std::nullptr_t",
                    PrimitiveKind.Auto => "auto",
                    PrimitiveKind.DecltypeAuto => "decltype(auto)"
                };
                builder.Append(value);

                var skipFirstSpaceBefore = false;
                OutputQualifiers(ref builder, flags, Qualifiers, true, false, ref skipFirstSpaceBefore);
            }

            public override void OutputPost(ref Utf8StringBuilder builder, UNDNAME flags)
            {
                //Empty
            }

            public override void Reset()
            {
                base.Reset();

                PrimitiveKind = default;
            }
        }
    }
}
