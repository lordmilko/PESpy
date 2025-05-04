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

        public int Parent => table.GetParent(RowIndex);

        public BlobIndex PermissionSet => table.GetPermissionSet(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        private readonly DeclSecurityTable table;

        internal DeclSecurityRow(DeclSecurityIndex index, DeclSecurityTable table)
        {
            //II.22.11

            RowIndex = index;
            this.table = table;
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("DeclSecurity Row", this, ViewKind.Metadata_DeclSecurityRow);

            s.WriteValue(nameof(Action), Action, sizeof(short));
            s.WriteHasDeclSecurityIndex(nameof(Parent), Parent);
            s.WriteBlobHeapIndex(nameof(PermissionSet), PermissionSet);
        }
    }
}
