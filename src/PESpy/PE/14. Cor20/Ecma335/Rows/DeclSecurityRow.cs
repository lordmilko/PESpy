using System.Diagnostics;
using ClrDebug;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    [DebuggerDisplay("Action = {Action}, Parent = {Parent}, PermissionSet = {PermissionSet}")]
    public readonly struct DeclSecurityRow : IValue, IViewable
    {
        public DeclSecurityIndex RowIndex { get; }

        //SecurityAction does not have all of the values that CorDeclSecurity has
        public CorDeclSecurity Action => table.GetAction(RowIndex);

        public Index Parent => table.GetParent(RowIndex);

        public BlobIndex PermissionSet => table.GetPermissionSet(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

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
            writer.NewStruct(Strings.DeclSecurityRow, this, ViewKind.Metadata_DeclSecurityRow, table.RowSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateMetadataRow(parent);

            s.WriteValue(nameof(Action), Action, sizeof(short));
            s.WriteHasDeclSecurityIndex(nameof(Parent), (int) Parent);
            s.WriteBlobHeapIndex(nameof(PermissionSet), PermissionSet);

            return s.ToArray();
        }
    }
}
