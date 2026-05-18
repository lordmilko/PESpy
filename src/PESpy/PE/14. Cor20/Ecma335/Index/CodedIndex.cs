using System.Diagnostics;
using ClrDebug;

namespace PESpy.Ecma335
{
    [DebuggerDisplay("{DebuggerDisplay(),nq}")]
    public readonly struct CodedIndex
    {
        private string DebuggerDisplay()
        {
            return $"[{TableKind}] {RowId}";
        }

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

        public object GetRow(CompressedModelHeap heap)
        {
            if (RowId == 0)
                return null;

            return TableKind switch
            {
                TableKind.Module                 => heap.ModuleTable[this],
                TableKind.TypeRef                => heap.TypeRefTable[this],
                TableKind.TypeDef                => heap.TypeDefTable[this],
                TableKind.FieldPtr               => heap.FieldPtrTable[this],
                TableKind.Field                  => heap.FieldTable[this],
                TableKind.MethodPtr              => heap.MethodPtrTable[this],
                TableKind.MethodDef              => heap.MethodDefTable[this],
                TableKind.ParamPtr               => heap.ParamPtrTable[this],
                TableKind.Param                  => heap.ParamTable[this],
                TableKind.InterfaceImpl          => heap.InterfaceImplTable[this],
                TableKind.MemberRef              => heap.MemberRefTable[this],
                TableKind.Constant               => heap.ConstantTable[this],
                TableKind.CustomAttribute        => heap.CustomAttributeTable[this],
                TableKind.FieldMarshal           => heap.FieldMarshalTable[this],
                TableKind.DeclSecurity           => heap.DeclSecurityTable[this],
                TableKind.ClassLayout            => heap.ClassLayoutTable[this],
                TableKind.FieldLayout            => heap.FieldLayoutTable[this],
                TableKind.StandAloneSig          => heap.StandAloneSigTable[this],
                TableKind.EventMap               => heap.EventMapTable[this],
                TableKind.EventPtr               => heap.EventPtrTable[this],
                TableKind.Event                  => heap.EventTable[this],
                TableKind.PropertyMap            => heap.PropertyMapTable[this],
                TableKind.PropertyPtr            => heap.PropertyPtrTable[this],
                TableKind.Property               => heap.PropertyTable[this],
                TableKind.MethodSemantics        => heap.MethodSemanticsTable[this],
                TableKind.MethodImpl             => heap.MethodImplTable[this],
                TableKind.ModuleRef              => heap.ModuleRefTable[this],
                TableKind.TypeSpec               => heap.TypeSpecTable[this],
                TableKind.ImplMap                => heap.ImplMapTable[this],
                TableKind.FieldRva               => heap.FieldRvaTable[this],
                TableKind.EncLog                 => heap.EncLogTable[this],
                TableKind.EncMap                 => heap.EncMapTable[this],
                TableKind.Assembly               => heap.AssemblyTable[this],
                TableKind.AssemblyProcessor      => heap.AssemblyProcessorTable[this],
                TableKind.AssemblyOS             => heap.AssemblyOSTable[this],
                TableKind.AssemblyRef            => heap.AssemblyRefTable[this],
                TableKind.AssemblyRefProcessor   => heap.AssemblyRefProcessorTable[this],
                TableKind.AssemblyRefOS          => heap.AssemblyRefOSTable[this],
                TableKind.File                   => heap.FileTable[this],
                TableKind.ExportedType           => heap.ExportedTypeTable[this],
                TableKind.ManifestResource       => heap.ManifestResourceTable[this],
                TableKind.NestedClass            => heap.NestedClassTable[this],
                TableKind.GenericParam           => heap.GenericParamTable[this],
                TableKind.MethodSpec             => heap.MethodSpecTable[this],
                TableKind.GenericParamConstraint => heap.GenericParamConstraintTable[this],

                //Portable PDB
                TableKind.Document               => heap.DocumentTable[this],
                TableKind.MethodDebugInformation => heap.MethodDebugInformationTable[this],
                TableKind.LocalScope             => heap.LocalScopeTable[this],
                TableKind.LocalVariable          => heap.LocalVariableTable[this],
                TableKind.LocalConstant          => heap.LocalConstantTable[this],
                TableKind.ImportScope            => heap.ImportScopeTable[this],
                TableKind.StateMachineMethod     => heap.StateMachineMethodTable[this],
                TableKind.CustomDebugInformation => heap.CustomDebugInformationTable[this],
            };
        }

        public static explicit operator int(CodedIndex index) => index.Value;

        //Note that not all table kinds map onto token types, hence this is an explicit cast
        public static explicit operator mdToken(CodedIndex index)
        {
            var type = index.TableKind switch
            {
                TableKind.Module => CorTokenType.mdtModule,
                TableKind.TypeRef => CorTokenType.mdtTypeRef,
                TableKind.TypeDef => CorTokenType.mdtTypeDef,
                TableKind.Field => CorTokenType.mdtFieldDef,
                TableKind.MethodDef => CorTokenType.mdtMethodDef,
                TableKind.Param => CorTokenType.mdtParamDef,
                TableKind.InterfaceImpl => CorTokenType.mdtInterfaceImpl,
                TableKind.MemberRef => CorTokenType.mdtMemberRef,
                TableKind.CustomAttribute => CorTokenType.mdtCustomAttribute,
                //Is mdtSignature TableKind.StandAloneSig?
                TableKind.Event => CorTokenType.mdtEvent,
                TableKind.Property => CorTokenType.mdtProperty,
                TableKind.MethodImpl => CorTokenType.mdtMethodImpl,
                TableKind.ModuleRef => CorTokenType.mdtModuleRef,
                TableKind.TypeSpec => CorTokenType.mdtTypeSpec,
                TableKind.Assembly => CorTokenType.mdtAssembly,
                TableKind.AssemblyRef => CorTokenType.mdtAssemblyRef,
                TableKind.File => CorTokenType.mdtFile,
                TableKind.ExportedType => CorTokenType.mdtExportedType,
                TableKind.ManifestResource => CorTokenType.mdtManifestResource,
                TableKind.GenericParam  => CorTokenType.mdtGenericParam,
                TableKind.MethodSpec => CorTokenType.mdtMethodSpec,
                TableKind.GenericParamConstraint => CorTokenType.mdtGenericParamConstraint
            };

            return Extensions.TokenFromRid(index.RowId, type);
        }
    }
}
