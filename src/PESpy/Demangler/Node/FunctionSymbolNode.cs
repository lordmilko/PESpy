using ClrDebug.DIA;

#nullable disable

namespace PESpy
{
    public static partial class Demangler
    {
        public class FunctionSymbolNode : SymbolNode
        {
            public FunctionSignatureNode Signature { get; internal set; }

            public FunctionSymbolNode() : base(NodeKind.FunctionSymbol)
            {
            }

            public override void Output(ref Utf8StringBuilder builder, UNDNAME flags)
            {
                //If this is a user defined conversion, we don't want to write the return type; this is emitted after the word "operator" by ConversionOperatorIdentifierNode
                Signature.OutputPre(ref builder, flags | (Name.UnqualifiedIdentifier is ConversionOperatorIdentifierNode ? UNDNAME.UNDNAME_NO_FUNCTION_RETURNS : 0));
                OutputSpaceIfNecessary(ref builder);
                Name.Output(ref builder, flags);
                Signature.OutputPost(ref builder, flags);
            }

            public override void Reset()
            {
                base.Reset();

                Signature = default;
            }
        }
    }
}
