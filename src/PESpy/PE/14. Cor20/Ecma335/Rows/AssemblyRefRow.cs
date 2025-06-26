using System.Diagnostics;
using ClrDebug;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    [DebuggerDisplay("Version = {MajorVersion.ToString(),nq}.{MinorVersion.ToString(),nq}.{BuildNumber.ToString(),nq}.{RevisionNumber.ToString(),nq}, Flags = {Flags}, PublicKeyOrToken = {PublicKeyOrToken}, Name = {Name.ToString(),nq}, Culture = {Culture.ToString(),nq}, HashValue = {HashValue}")]
    public readonly struct AssemblyRefRow : IValue, IViewable
    {
        public AssemblyRefIndex RowIndex { get; }

        public short MajorVersion => table.GetMajorVersion(RowIndex);
        public short MinorVersion => table.GetMinorVersion(RowIndex);
        public short BuildNumber => table.GetBuildNumber(RowIndex);
        public short RevisionNumber => table.GetRevisionNumber(RowIndex);

        public AssemblyFlags Flags => table.GetFlags(RowIndex);

        public BlobIndex PublicKeyOrToken => table.GetPublicKeyOrToken(RowIndex);

        public StringIndex Name => table.GetName(RowIndex);

        public StringIndex Culture => table.GetCulture(RowIndex);

        public BlobIndex HashValue => table.GetHashValue(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        private readonly AssemblyRefTable table;

        internal AssemblyRefRow(AssemblyRefIndex index, AssemblyRefTable table)
        {
            //II.22.5

            RowIndex = index;
            this.table = table;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct("AssemblyRef Row", this, ViewKind.Metadata_AssemblyRefRow, table.RowSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateMetadataRow(parent);

            s.WriteValue(nameof(MajorVersion), MajorVersion);
            s.WriteValue(nameof(MinorVersion), MinorVersion);
            s.WriteValue(nameof(BuildNumber), BuildNumber);
            s.WriteValue(nameof(RevisionNumber), RevisionNumber);
            s.WriteValue(nameof(Flags), Flags, sizeof(int));
            s.WriteBlobHeapIndex(nameof(PublicKeyOrToken), PublicKeyOrToken);
            s.WriteStringHeapIndex(nameof(Name), Name);
            s.WriteStringHeapIndex(nameof(Culture), Culture);
            s.WriteBlobHeapIndex(nameof(HashValue), HashValue);

            return s.ToArray();
        }
    }
}
