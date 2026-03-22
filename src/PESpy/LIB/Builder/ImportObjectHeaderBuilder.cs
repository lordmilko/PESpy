using ClrDebug;

namespace PESpy
{
    internal class ImportObjectHeaderBuilder
    {
        public IMAGE_FILE_MACHINE Sig1 { get; set; }

        public short Sig2 { get; set; }

        public short Version { get; set; }

        public IMAGE_FILE_MACHINE Machine { get; set; }

        public Timestamp TimeDateStamp { get; set; }

        public int SizeOfData { get; set; }

        public short Ordinal { get; set; }

        public IMPORT_OBJECT_TYPE Type { get; set; }

        public IMPORT_OBJECT_NAME_TYPE NameType { get; set; }

        public int Reserved { get; set; }

        public ImportObjectHeaderBuilder(ImportObjectHeader importObjectHeader)
        {
            Sig1 = importObjectHeader.Sig1;
            Sig2 = importObjectHeader.Sig2;
            Version = importObjectHeader.Version;
            Machine = importObjectHeader.Machine;
            TimeDateStamp = importObjectHeader.TimeDateStamp;
            SizeOfData = importObjectHeader.SizeOfData;
            Ordinal = importObjectHeader.Ordinal;
            Type = importObjectHeader.Type;
            NameType = importObjectHeader.NameType;
            Reserved = importObjectHeader.Reserved;
        }

        public void WriteTo(FileWriter writer)
        {
            writer.WriteUInt16((ushort) Sig1);
            writer.WriteUInt16((ushort) Sig2);
            writer.WriteUInt16((ushort) Version);
            writer.WriteUInt16((ushort) Machine);
            writer.WriteUInt32(TimeDateStamp);
            writer.WriteUInt32((uint) SizeOfData);
            writer.WriteUInt16((ushort) Ordinal);

            var data = (ushort) (((ushort) Type) | (((ushort) NameType) << 2) | (Reserved << 5));
            writer.WriteUInt16(data);
        }
    }
}
