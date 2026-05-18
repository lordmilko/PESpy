namespace PESpy
{
    public static partial class Demangler
    {
        public enum NodeKind
        {
            Unknown,

            SymbolStart,
            Md5Symbol = SymbolStart,
            EncodedStringLiteral,
            FunctionSymbol,
            LocalStaticGuardVariable,
            SpecialTableSymbol,
            VariableSymbol,
            CSymbol,
            SymbolEnd = VariableSymbol,

            IdentifierStart,
            ConversionOperatorIdentifier = IdentifierStart,
            DynamicStructorIdentifier,
            IntrinsicFunctionIdentifier,
            LiteralOperatorIdentifier,
            LocalStaticGuardIdentifier,
            NamedIdentifier,
            RttiBaseClassDescriptor,
            StructorIdentifier,
            VcallThunkIdentifier,
            IdentifierEnd = VcallThunkIdentifier,

            TypeStart,
            ArrayType = TypeStart,
            Custom,

            FunctionSignature,
            ThunkSignature,
            FunctionSignatureEnd = ThunkSignature,

            ManagedArray,
            PointerType,
            PrimitiveType,
            TagType,
            TypeEnd = TagType,

            PointerAuthQualifier,

            IntegerLiteral,

            NodeArray,

            QualifiedName,

            TemplateParameterReference,

            //Not in llvm-undname

            AllocWinRTQualifiedBaseIdentifier,
            ScopedIdentifier
        }
    }
}
