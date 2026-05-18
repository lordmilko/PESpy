using System;
using System.Reflection;
using ClrDebug;
using PESpy.View;
using AssemblyHashAlgorithm = System.Configuration.Assemblies.AssemblyHashAlgorithm;

namespace PESpy.Ecma335
{
    public readonly struct AssemblyRow : IValue, IViewable
    {
        public AssemblyIndex RowIndex { get; }

        public AssemblyHashAlgorithm HashAlgId => table.GetHashAlgId(RowIndex);

        public short MajorVersion => table.GetMajorVersion(RowIndex);
        public short MinorVersion => table.GetMinorVersion(RowIndex);
        public short BuildNumber => table.GetBuildNumber(RowIndex);
        public short RevisionNumber => table.GetRevisionNumber(RowIndex);

        public Version Version => new Version(MajorVersion, MinorVersion, BuildNumber, RevisionNumber);

        public CorAssemblyFlags Flags => table.GetFlags(RowIndex);

        public BlobIndex PublicKey => table.GetPublicKey(RowIndex);

        public StringIndex Name => table.GetName(RowIndex);

        public StringIndex Culture => table.GetCulture(RowIndex);

        public AssemblyName AssemblyName => ModelHeap.GetAssemblyName(Name, Version, Culture, PublicKey, HashAlgId, Flags);

        public long Offset => table.GetRowOffset(RowIndex);

        private readonly AssemblyTable table;

        internal AssemblyRow(AssemblyIndex index, AssemblyTable table)
        {
            //II.22.2

            RowIndex = index;
            this.table = table;
        }

        public CustomAttributeList CustomAttributes => table.GetCustomAttributes(RowIndex);

        public DeclSecurityAttributeList DeclSecurityAttributes => table.GetDeclSecurityAttributes(RowIndex);

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.Metadata_AssemblyRow, table.RowSize);

        int IViewable.NumChildren() => 9;

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

        public override string ToString() => AssemblyName.ToString();
    }
}
