using System;
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

        int IViewable.NumChildren => 9;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(HashAlgId), table.HashAlgIdOffset, HashAlgId, sizeof(int));
                    break;

                case 1:
                    structWriter.WriteField(nameof(MajorVersion), table.MajorVersionOffset, MajorVersion);
                    break;

                case 2:
                    structWriter.WriteField(nameof(MinorVersion), table.MinorVersionOffset, MinorVersion);
                    break;

                case 3:
                    structWriter.WriteField(nameof(BuildNumber), table.BuildNumberOffset, BuildNumber);
                    break;

                case 4:
                    structWriter.WriteField(nameof(RevisionNumber), table.RevisionNumberOffset, RevisionNumber);
                    break;

                case 5:
                    structWriter.WriteField(nameof(Flags), table.FlagsOffset, Flags, sizeof(int));
                    break;

                case 6:
                    structWriter.WriteBlobHeapIndex(nameof(PublicKey), table.PublicKeyOffset, PublicKey);
                    break;

                case 7:
                    structWriter.WriteStringHeapIndex(nameof(Name), table.NameOffset, Name);
                    break;

                case 8:
                    structWriter.WriteStringHeapIndex(nameof(Culture), table.CultureOffset, Culture);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
