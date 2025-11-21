using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy.Ecma335
{
    [DebuggerDisplay("Parent = {Parent}, Type = {Type}, Value = {Value}")]
    public readonly struct CustomAttributeRow : IValue, IViewable
    {
        public CustomAttributeIndex RowIndex { get; }

        public CodedIndex Parent => table.GetParent(RowIndex);

        //While this property is called Type as per II.22.10,
        //it's really a MethodDef or MemberRef describing
        //the constructor of the attribute
        public CodedIndex Type => table.GetType(RowIndex);

        public BlobIndex Value => table.GetValue(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        private readonly CustomAttributeTable table;

        internal CustomAttributeRow(CustomAttributeIndex index, CustomAttributeTable table)
        {
            //II.22.10

            RowIndex = index;
            this.table = table;
        }

        public bool TryGetName(out StringIndex namespaceIndex, out StringIndex nameIndex)
        {
            if (!TryGetType(out var typeIndex))
            {
                namespaceIndex = default;
                nameIndex = default;
                return false;
            }

            return GetTypeNamespaceAndName(typeIndex, out namespaceIndex, out nameIndex);
        }

        private bool TryGetType(out CodedIndex typeIndex)
        {
            var ctor = Type;

            switch (ctor.TableKind)
            {
                case TableKind.MemberRef:
                    typeIndex = table.CompressedModelHeap.MemberRefTable[ctor.RowId].Class;
                    return true;
                default:
                    typeIndex = default;
                    return false;
            }
        }

        private bool GetTypeNamespaceAndName(CodedIndex index, out StringIndex namespaceIndex, out StringIndex nameIndex)
        {
            namespaceIndex = default;
            nameIndex = default;

            if (index.TableKind == TableKind.TypeDef)
            {
                var typeDef = table.CompressedModelHeap.TypeDefTable[index.RowId];

                namespaceIndex = typeDef.TypeNamespace;
                nameIndex = typeDef.TypeName;
                return true;
            }
            else if (index.TableKind == TableKind.TypeRef)
            {
                var typeRef = table.CompressedModelHeap.TypeRefTable[index.RowId];

                var resolutionScopeKind = typeRef.ResolutionScope.TableKind;

                //If it's a nested type, it's too complex for us to resolve just based on simple metadata
                if (resolutionScopeKind == TableKind.TypeRef || resolutionScopeKind == TableKind.TypeDef)
                    return false;

                namespaceIndex = typeRef.TypeNamespace;
                nameIndex = typeRef.TypeName;
                return true;
            }

            throw new NotImplementedException($"Don't know how to get the type and namespace for an index of type '{index.TableKind}'");
        }

        //Note: will throw if the custom attribute references an enum or a typeof(something)
        public CustomAttributeValue<object> DecodeValue() => DecodeValue<object>();

        public CustomAttributeValue<TType> DecodeValue<TType>(ICustomAttributeTypeProvider<TType>? provider = null)
        {
            var decoder = new CustomAttributeDecoder<TType>(table.CompressedModelHeap, provider);
            return decoder.DecodeValue(Type, Value);
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.CustomAttributeRow, this, ViewKind.Metadata_CustomAttributeRow, table.RowSize);

        int IViewable.NumChildren() => 3;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteHasCustomAttributeIndex(nameof(Parent), table.ParentOffset, Parent);
                    break;

                case 1:
                    structWriter.WriteCustomAttributeTypeIndex(nameof(Type), table.TypeOffset, Type);
                    break;

                case 2:
                    structWriter.WriteBlobHeapIndex(nameof(Value), table.ValueOffset, Value);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
