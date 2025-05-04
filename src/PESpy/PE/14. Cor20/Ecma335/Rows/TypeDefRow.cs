using System.Diagnostics;
using ClrDebug;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    [DebuggerDisplay("Flags = {Flags}, TypeName = {TypeName.ToString(),nq}, TypeNamespace = {TypeNamespace.ToString(),nq}, Extends = {Extends}, FieldList = {FieldList}, MethodList = {MethodList}")]
    public readonly struct TypeDefRow : IValue, IViewable
    {
        public TypeDefIndex RowIndex { get; }

        public CorTypeAttr Flags => table.GetFlags(RowIndex);

        public StringIndex TypeName => table.GetTypeName(RowIndex);

        public StringIndex TypeNamespace => table.GetTypeNamespace(RowIndex);

        public int Extends => table.GetExtends(RowIndex);

        public int FieldList => table.GetFieldList(RowIndex);

        public int MethodList => table.GetMethodList(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        private readonly TypeDefTable table;

        internal TypeDefRow(TypeDefIndex index, TypeDefTable table)
        {
            //II.22.37

            RowIndex = index;
            this.table = table;
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("TypeDef Row", this, ViewKind.Metadata_TypeDefRow);

            s.WriteValue(nameof(Flags), Flags, sizeof(int));
            s.WriteStringHeapIndex(nameof(TypeName), TypeName);
            s.WriteStringHeapIndex(nameof(TypeNamespace), TypeNamespace);
            s.WriteTypeDefOrRefIndex(nameof(Extends), Extends);
            s.WriteSimpleIndex(nameof(FieldList), FieldList, TableKind.Field);
            s.WriteSimpleIndex(nameof(MethodList), MethodList, TableKind.MethodDef);
        }
    }
}
