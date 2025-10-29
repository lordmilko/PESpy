using System.Diagnostics;

namespace PESpy.Ecma335
{
    [DebuggerDisplay("[{TableKind}] {RowId}")]
    public readonly struct CodedIndex
    {
        public CodedIndexType CodedIndexType { get; }

        public TableKind TableKind
        {
            get
            {
                return CodedIndexType switch
                {
                    CodedIndexType.TypeDefOrRef              => TypeDefOrRefTag.GetTableKind(Value),
                    CodedIndexType.HasConstant               => HasConstantTag.GetTableKind(Value),
                    CodedIndexType.HasCustomAttribute        => HasCustomAttributeTag.GetTableKind(Value),
                    CodedIndexType.HasFieldMarshal           => HasFieldMarshalTag.GetTableKind(Value),
                    CodedIndexType.HasDeclSecurity           => HasDeclSecurityTag.GetTableKind(Value),
                    CodedIndexType.MemberRefParent           => MemberRefParentTag.GetTableKind(Value),
                    CodedIndexType.HasSemantics              => HasSemanticsTag.GetTableKind(Value),
                    CodedIndexType.MethodDefOrRef            => MethodDefOrRefTag.GetTableKind(Value),
                    CodedIndexType.MemberForwarded           => MemberForwardedTag.GetTableKind(Value),
                    CodedIndexType.Implementation            => ImplementationTag.GetTableKind(Value),
                    CodedIndexType.CustomAttributeType       => CustomAttributeTypeTag.GetTableKind(Value),
                    CodedIndexType.ResolutionScope           => ResolutionScopeTag.GetTableKind(Value),
                    CodedIndexType.TypeOrMethodDef           => TypeOrMethodDefTag.GetTableKind(Value),
                    CodedIndexType.HasCustomDebugInformation => HasCustomDebugInformationTag.GetTableKind(Value),

                                
                };
            }
        }

        public int RowId
        {
            get
            {
                var numberOfBits = CodedIndexType switch
                {
                    CodedIndexType.TypeDefOrRef              => TypeDefOrRefTag.LogN,
                    CodedIndexType.HasConstant               => HasConstantTag.LogN,
                    CodedIndexType.HasCustomAttribute        => HasCustomAttributeTag.LogN,
                    CodedIndexType.HasFieldMarshal           => HasFieldMarshalTag.LogN,
                    CodedIndexType.HasDeclSecurity           => HasDeclSecurityTag.LogN,
                    CodedIndexType.MemberRefParent           => MemberRefParentTag.LogN,
                    CodedIndexType.HasSemantics              => HasSemanticsTag.LogN,
                    CodedIndexType.MethodDefOrRef            => MethodDefOrRefTag.LogN,
                    CodedIndexType.MemberForwarded           => MemberForwardedTag.LogN,
                    CodedIndexType.Implementation            => ImplementationTag.LogN,
                    CodedIndexType.CustomAttributeType       => CustomAttributeTypeTag.LogN,
                    CodedIndexType.ResolutionScope           => ResolutionScopeTag.LogN,
                    CodedIndexType.TypeOrMethodDef           => TypeOrMethodDefTag.LogN,
                    CodedIndexType.HasCustomDebugInformation => HasCustomDebugInformationTag.LogN
                };

                return Value >> numberOfBits;
            }
        }

        public int Value { get; }

        internal CodedIndex(int value, CodedIndexType type)
        {
            CodedIndexType = type;
            Value = value;
        }

        public static implicit operator int(CodedIndex index) => index.Value;
    }
}
