using ClrDebug.DIA;

namespace PESpy
{
    public static partial class Demangler
    {
        public class CSymbolNode : SymbolNode
        {
            public CallingConv CallingConvention { get; internal set; }

            public int ParameterListSize { get; internal set; }

            private NamedIdentifierNode _identifier;

            public CSymbolNode() : base(NodeKind.CSymbol)
            {
                _identifier = new NamedIdentifierNode();

                Name = new QualifiedNameNode
                {
                    Components = new NodeArrayNode
                    {
                        count = 1,
                        rentedNodes = new[]
                        {
                            _identifier
                        }
                    }
                };
            }

            internal void SetName(FixedUtf8String name)
            {
                _identifier.Name = name;
            }

            public override void Output(ref Utf8StringBuilder builder, UNDNAME flags)
            {
                if ((flags & UNDNAME.UNDNAME_NAME_ONLY) == 0 && (flags & UNDNAME.UNDNAME_NO_MS_KEYWORDS) == 0 && (flags & UNDNAME.UNDNAME_NO_ALLOCATION_LANGUAGE) == 0)
                {
                    OutputCallingConvention(ref builder, CallingConvention);
                    EnsureSpace(ref builder);
                }

                base.Output(ref builder, flags);
            }

            public override void Reset()
            {
                //Do not call base.Reset(); we handle our name in a special way
                _identifier.Reset();

                CallingConvention = default;
                ParameterListSize = default;
            }
        }
    }
}
