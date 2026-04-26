using System;
using System.Diagnostics;
using ClrDebug;
using PESpy.View;

namespace PESpy.Ecma335
{
    [DebuggerDisplay("Action = {Action}, Parent = {ParentRow}, PermissionSet = {PermissionSet}")]
    public readonly struct DeclSecurityRow : IValue, IViewable
    {
        public DeclSecurityIndex RowIndex { get; }

        //SecurityAction does not have all of the values that CorDeclSecurity has
        public CorDeclSecurity Action => table.GetAction(RowIndex);

        public CodedIndex Parent => table.GetParent(RowIndex);

        public BlobIndex PermissionSet => table.GetPermissionSet(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        //Extensions
        public object ParentRow => Parent.GetRow(table.CompressedModelHeap);

        private readonly DeclSecurityTable table;

        internal DeclSecurityRow(DeclSecurityIndex index, DeclSecurityTable table)
        {
            //II.22.11

            RowIndex = index;
            this.table = table;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.Metadata_DeclSecurityRow, table.RowSize);

        int IViewable.NumChildren() => 3;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(Action), table.ActionOffset, Action, sizeof(short));
                    break;

                case 1:
                    structWriter.WriteHasDeclSecurityIndex(nameof(Parent), table.ParentOffset, Parent);
                    break;

                case 2:
                    structWriter.WriteBlobHeapIndex(nameof(PermissionSet), table.PermissionSetOffset, PermissionSet);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
