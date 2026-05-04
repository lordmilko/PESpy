using System;
using System.Reflection;
using ClrDebug;
using PESpy.View;
using AssemblyHashAlgorithm = System.Configuration.Assemblies.AssemblyHashAlgorithm;

namespace PESpy.Ecma335
{
    public readonly struct AssemblyRefRow : IValue, IViewable
    {
        public AssemblyRefIndex RowIndex { get; }

        public short MajorVersion => table.GetMajorVersion(RowIndex);
        public short MinorVersion => table.GetMinorVersion(RowIndex);
        public short BuildNumber => table.GetBuildNumber(RowIndex);
        public short RevisionNumber => table.GetRevisionNumber(RowIndex);

        public Version Version => new Version(MajorVersion, MinorVersion, BuildNumber, RevisionNumber);

        public CorAssemblyFlags Flags => table.GetFlags(RowIndex);

        public BlobIndex PublicKeyOrToken => table.GetPublicKeyOrToken(RowIndex);

        public StringIndex Name => table.GetName(RowIndex);

        public StringIndex Culture => table.GetCulture(RowIndex);

        public BlobIndex HashValue => table.GetHashValue(RowIndex);

        public AssemblyName AssemblyName => CompressedModelHeap.GetAssemblyName(Name, Version, Culture, PublicKeyOrToken, AssemblyHashAlgorithm.None, Flags);

        public long Offset => table.GetRowOffset(RowIndex);

        private readonly AssemblyRefTable table;

        internal AssemblyRefRow(AssemblyRefIndex index, AssemblyRefTable table)
        {
            //II.22.5

            RowIndex = index;
            this.table = table;
        }

        public CustomAttributeList CustomAttributes => table.GetCustomAttributes(RowIndex);

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.Metadata_AssemblyRefRow, table.RowSize);

        int IViewable.NumChildren() => 9;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(MajorVersion), table.MajorVersionOffset, MajorVersion);
                    break;

                case 1:
                    structWriter.WriteField(nameof(MinorVersion), table.MinorVersionOffset, MinorVersion);
                    break;

                case 2:
                    structWriter.WriteField(nameof(BuildNumber), table.BuildNumberOffset, BuildNumber);
                    break;

                case 3:
                    structWriter.WriteField(nameof(RevisionNumber), table.RevisionNumberOffset, RevisionNumber);
                    break;

                case 4:
                    structWriter.WriteField(nameof(Flags), table.FlagsOffset, Flags, sizeof(int));
                    break;

                case 5:
                    structWriter.WriteBlobHeapIndex(nameof(PublicKeyOrToken), table.PublicKeyOrTokenOffset, PublicKeyOrToken);
                    break;

                case 6:
                    structWriter.WriteStringHeapIndex(nameof(Name), table.NameOffset, Name);
                    break;

                case 7:
                    structWriter.WriteStringHeapIndex(nameof(Culture), table.CultureOffset, Culture);
                    break;

                case 8:
                    structWriter.WriteBlobHeapIndex(nameof(HashValue), table.HashValueOffset, HashValue);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }

        public override string ToString() => AssemblyName.ToString();
    }
}
