using System;
using System.Diagnostics;
using ClrDebug;
using PESpy.View;

namespace PESpy.Ecma335
{
    [DebuggerDisplay("Flags = {Flags}, TypeName = {TypeName.ToString(),nq}, TypeNamespace = {TypeNamespace.ToString(),nq}, Extends = {Extends}, FieldList = {FieldList}, MethodList = {MethodList}")]
    public readonly struct TypeDefRow : IValue, IViewable
    {
        public TypeDefIndex RowIndex { get; }

        public CorTypeAttr Flags => table.GetFlags(RowIndex);

        public StringIndex TypeName => table.GetTypeName(RowIndex);

        public StringIndex TypeNamespace => table.GetTypeNamespace(RowIndex);

        public CodedIndex Extends => table.GetExtends(RowIndex);

        public FieldIndex FieldList => table.GetFieldList(RowIndex);

        public MethodDefIndex MethodList => table.GetMethodList(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        private readonly TypeDefTable table;

        internal TypeDefRow(TypeDefIndex index, TypeDefTable table)
        {
            //II.22.37

            RowIndex = index;
            this.table = table;
        }

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
    }
}
