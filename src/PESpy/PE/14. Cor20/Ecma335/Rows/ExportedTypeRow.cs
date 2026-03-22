using System;
using System.Diagnostics;
using ClrDebug;
using PESpy.View;

namespace PESpy.Ecma335
{
    [DebuggerDisplay("Flags = {Flags}, TypeDefId = {TypeDefId}, TypeName = {TypeName.ToString(),nq}, TypeNamespace = {TypeNamespace.ToString(),nq}, Implementation = {ImplementationRow}")]
    public readonly struct ExportedTypeRow : IValue, IViewable
    {
        public ExportedTypeIndex RowIndex { get; }

        public CorTypeAttr Flags => table.GetFlags(RowIndex);

        public int TypeDefId => table.GetTypeDefId(RowIndex);

        public StringIndex TypeName => table.GetTypeName(RowIndex);

        public StringIndex TypeNamespace => table.GetTypeNamespace(RowIndex);

        public CodedIndex Implementation => table.GetImplementation(RowIndex);

        public bool IsForwarder => (Flags & CorTypeAttr.tdForwarder) != 0 && Implementation.TableKind == TableKind.AssemblyRef;

        public int Offset => table.GetRowOffset(RowIndex);

        //Extensions
        public object ImplementationRow => Implementation.GetRow(table.CompressedModelHeap);

        private readonly ExportedTypeTable table;

        internal ExportedTypeRow(ExportedTypeIndex index, ExportedTypeTable table)
        {
            //II.22.14

            RowIndex = index;
            this.table = table;
        }

        public CustomAttributeList CustomAttributes => table.GetCustomAttributes(RowIndex);

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.ExportedTypeRow, this, ViewKind.Metadata_ExportedTypeRow, table.RowSize);

        int IViewable.NumChildren() => 5;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(Flags), table.FlagsOffset, Flags, sizeof(int));
                    break;

                case 1:
                    structWriter.WriteSimpleIndex(nameof(TypeDefId), table.TypeDefIdOffset, TypeDefId, TableKind.TypeDef);
                    break;

                case 2:
                    structWriter.WriteStringHeapIndex(nameof(TypeName), table.TypeNameOffset, TypeName);
                    break;

                case 3:
                    structWriter.WriteStringHeapIndex(nameof(TypeNamespace), table.TypeNamespaceOffset, TypeNamespace);
                    break;

                case 4:
                    structWriter.WriteImplementationIndex(nameof(Implementation), table.ImplementationOffset, Implementation);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }

        public override string ToString() => CompressedModelHeap.FormatType(TypeNamespace, TypeName);
    }
}
