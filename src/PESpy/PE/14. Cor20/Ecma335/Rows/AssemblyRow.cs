using System.Configuration.Assemblies;
using System.Diagnostics;
using ClrDebug;
using PESpy.View;

namespace PESpy.Ecma335
{
    [DebuggerDisplay("HashAlgId = {HashAlgId}, Version = {MajorVersion.ToString(),nq}.{MinorVersion.ToString(),nq}.{BuildNumber.ToString(),nq}.{RevisionNumber.ToString(),nq}, Flags = {Flags}, PublicKey = {PublicKey}, Name = {Name.ToString(),nq}, Culture = {Culture.ToString(),nq}")]
    public readonly struct AssemblyRow : IValue, IViewable
    {
        public AssemblyIndex RowIndex { get; }

        public AssemblyHashAlgorithm HashAlgId => table.GetHashAlgId(RowIndex);

        public short MajorVersion => table.GetMajorVersion(RowIndex);
        public short MinorVersion => table.GetMinorVersion(RowIndex);
        public short BuildNumber => table.GetBuildNumber(RowIndex);
        public short RevisionNumber => table.GetRevisionNumber(RowIndex);

        public AssemblyFlags Flags => table.GetFlags(RowIndex);

        public BlobIndex PublicKey => table.GetPublicKey(RowIndex);

        public StringIndex Name => table.GetName(RowIndex);

        public StringIndex Culture => table.GetCulture(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        private readonly AssemblyTable table;

        internal AssemblyRow(AssemblyIndex index, AssemblyTable table)
        {
            //II.22.2

            RowIndex = index;
            this.table = table;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.AssemblyRow, this, ViewKind.Metadata_AssemblyRow, table.RowSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateMetadataRow(parent);

            s.WriteValue(nameof(HashAlgId), HashAlgId, sizeof(int));
            s.WriteValue(nameof(MajorVersion), MajorVersion);
            s.WriteValue(nameof(MinorVersion), MinorVersion);
            s.WriteValue(nameof(BuildNumber), BuildNumber);
            s.WriteValue(nameof(RevisionNumber), RevisionNumber);
            s.WriteValue(nameof(Flags), Flags, sizeof(int));
            s.WriteBlobHeapIndex(nameof(PublicKey), PublicKey);
            s.WriteStringHeapIndex(nameof(Name), Name);
            s.WriteStringHeapIndex(nameof(Culture), Culture);

            return s.ToArray();
        }
    }
}
