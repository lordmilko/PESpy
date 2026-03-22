using System;
using ClrDebug;
using PESpy.View;

namespace PESpy.Ecma335
{
    public readonly struct TypeDefRow : IValue, IViewable
    {
        public TypeDefIndex RowIndex { get; }

        public CorTypeAttr Flags => table.GetFlags(RowIndex);

        public StringIndex TypeName => table.GetTypeName(RowIndex);

        public StringIndex TypeNamespace => table.GetTypeNamespace(RowIndex);

        public CodedIndex Extends => table.GetExtends(RowIndex);

        //This is really just the first entry in the type's fields.
        //It runs to either the last row of the field table, or the start of the next type's
        //FieldList
        public FieldIndex FieldList => table.GetFieldList(RowIndex);

        //This is really just the first entry in the type's methods.
        //It runs to either the last row of the method table, or the start of the next type's
        //MethodList
        public MethodDefIndex MethodList => table.GetMethodList(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        //Extensions
        public object ExtendsRow => Extends.GetRow(table.CompressedModelHeap);

        private readonly TypeDefTable table;

        internal TypeDefRow(TypeDefIndex index, TypeDefTable table)
        {
            //II.22.37

            RowIndex = index;
            this.table = table;
        }

        public CustomAttributeList CustomAttributes => table.GetCustomAttributes(RowIndex);

        //System.Reflection.Metadata has a custom TypeLayout type that just contains some of the members
        //of the ClassLayoutRow item
        public ClassLayoutRow? Layout
        {
            get
            {
                var rowId = table.CompressedModelHeap.ClassLayoutTable.FindRow(RowIndex);

                if (rowId.RowId == 0)
                    return default;

                return table.CompressedModelHeap.ClassLayoutTable[rowId];
            }
        }

        public TypeDefRow? DeclaringType
        {
            get
            {
                var index = table.CompressedModelHeap.NestedClassTable.FindEnclosingType(RowIndex);

                if (index.RowId == 0)
                    return null;

                return table[index];
            }
        }

        public GenericParamList GenericParameters => table.CompressedModelHeap.GenericParamTable.FindGenericParameters(TypeOrMethodDefTag.CreateIndex(RowIndex.RowId, TableKind.TypeDef));

        public MethodDefList Methods => new MethodDefList(RowIndex, table.CompressedModelHeap);

        public FieldDefList Fields => new FieldDefList(RowIndex, table.CompressedModelHeap);

        public PropertyList Properties => new PropertyList(RowIndex, table.CompressedModelHeap);

        public EventList Events => new EventList(RowIndex, table.CompressedModelHeap);

        public TypeDefIndex[] NestedTypes => table.GetNestedTypes(RowIndex);

        public MethodImplList MethodImplementations => new MethodImplList(RowIndex, table.CompressedModelHeap);

        public InterfaceImplList InterfaceImplementations => new InterfaceImplList(RowIndex, table.CompressedModelHeap);

        public DeclSecurityAttributeList DeclSecurityAttributes => table.GetDeclSecurityAttributes(RowIndex);

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.TypeDefRow, this, ViewKind.Metadata_TypeDefRow, table.RowSize);

        int IViewable.NumChildren() => 6;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(Flags), table.FlagsOffset, Flags, sizeof(int));
                    break;

                case 1:
                    structWriter.WriteStringHeapIndex(nameof(TypeName), table.TypeNameOffset, TypeName);
                    break;

                case 2:
                    structWriter.WriteStringHeapIndex(nameof(TypeNamespace), table.TypeNamespaceOffset, TypeNamespace);
                    break;

                case 3:
                    structWriter.WriteTypeDefOrRefIndex(nameof(Extends), table.ExtendsOffset, Extends);
                    break;

                case 4:
                    structWriter.WriteSimpleIndex(nameof(FieldList), table.FieldListOffset, (int) FieldList, TableKind.Field);
                    break;

                case 5:
                    structWriter.WriteSimpleIndex(nameof(MethodList), table.MethodListOffset, (int) MethodList, TableKind.MethodDef);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }

        public override string ToString() => CompressedModelHeap.FormatType(TypeNamespace, TypeName);
    }
}
