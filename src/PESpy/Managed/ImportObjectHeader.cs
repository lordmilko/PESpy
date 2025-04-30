using ClrDebug;
using PESpy.Native;
using PESpy.View;

namespace PESpy
{
    public class ImportObjectHeader : IValue, IViewable
    {
        public IMAGE_FILE_MACHINE Sig1 => (IMAGE_FILE_MACHINE) chunk.PeekUInt16(0);

        public short Sig2 => chunk.PeekInt16(2);

        public short Version => chunk.PeekInt16(4);

        public IMAGE_FILE_MACHINE Machine => (IMAGE_FILE_MACHINE) chunk.PeekUInt16(6);

        public uint TimeDateStamp => chunk.PeekUInt32(8);

        public int SizeOfData => chunk.PeekInt32(12);

        public short Ordinal => chunk.PeekInt16(16);

        public short Hint => chunk.PeekInt16(16);

        private short data => chunk.PeekInt16(18);

        public IMPORT_OBJECT_TYPE Type => (IMPORT_OBJECT_TYPE) (data & 0x3);
        public IMPORT_OBJECT_NAME_TYPE NameType => (IMPORT_OBJECT_NAME_TYPE) ((data >> 2) & 0x7);
        public int Reserved => (data >> 5) & 0x07FF;

        public int Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(short) + //Sig1
            sizeof(short) + //Sig2
            sizeof(short) + //Version
            sizeof(short) + //Machine
            sizeof(int) +   //TimeDateStamp
            sizeof(int) +   //SizeOfData
            sizeof(short) +   //Ordinal / Hint
            sizeof(short);    //data

        private readonly MemoryChunk chunk;

        internal ImportObjectHeader(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct(nameof(IMPORT_OBJECT_HEADER), this, ViewKind.ImportObjectHeader);

            s.WriteField(nameof(Sig1), Sig1, sizeof(short));
            s.WriteField(nameof(Sig2), Sig2);
            s.WriteField(nameof(Version), Version);
            s.WriteField(nameof(Machine), Machine, sizeof(short));
            s.WriteField(nameof(TimeDateStamp), TimeDateStamp);
            s.WriteField(nameof(SizeOfData), SizeOfData);
            s.WriteField("Ordinal / Hint", Ordinal); //Don't know what "grf" refers to
            
            using (var bitField = s.WriteBitFields<ushort>())
            {
                bitField.WriteField(nameof(Type), Type, 2);
                bitField.WriteField(nameof(NameType), NameType, 3);
                bitField.WriteField(nameof(Reserved), Reserved, 11);
            }
        }
    }
}
