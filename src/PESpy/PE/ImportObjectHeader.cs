using System;
using System.Diagnostics;
using ClrDebug;
using PESpy.View;

namespace PESpy
{
    public class ImportObjectHeader : IValue, IViewable
    {
        private const int Sig1Offset = 0;
        private const int Sig2Offset = 2;
        private const int VersionOffset = 4;
        private const int MachineOffset = 6;
        private const int TimeDateStampOffset = 8;
        private const int SizeOfDataOffset = 12;
        private const int OrdinalOffset = 16;
        private const int HintOffset = 16;
        private const int dataOffset = 18;

        public IMAGE_FILE_MACHINE Sig1 => (IMAGE_FILE_MACHINE) chunk.PeekUInt16(Sig1Offset);

        public short Sig2 => chunk.PeekInt16(Sig2Offset);

        public short Version => chunk.PeekInt16(VersionOffset);

        public IMAGE_FILE_MACHINE Machine => (IMAGE_FILE_MACHINE) chunk.PeekUInt16(MachineOffset);

        public uint TimeDateStamp => chunk.PeekUInt32(TimeDateStampOffset);

        public int SizeOfData => chunk.PeekInt32(SizeOfDataOffset);

        public short Ordinal => chunk.PeekInt16(OrdinalOffset);

        public short Hint => chunk.PeekInt16(HintOffset);

        private short data => chunk.PeekInt16(dataOffset);

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

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.IMPORT_OBJECT_HEADER, this, ViewKind.ImportObjectHeader, StructSize);

        int IViewable.NumChildren() => 10;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(Sig1), Sig1Offset, Sig1, sizeof(short));
                    break;

                case 1:
                    structWriter.WriteField(nameof(Sig2), Sig2Offset, Sig2);
                    break;

                case 2:
                    structWriter.WriteField(nameof(Version), VersionOffset, Version);
                    break;

                case 3:
                    structWriter.WriteField(nameof(Machine), MachineOffset, Machine, sizeof(short));
                    break;

                case 4:
                    structWriter.WriteField(nameof(TimeDateStamp), TimeDateStampOffset, TimeDateStamp);
                    break;

                case 5:
                    structWriter.WriteField(nameof(SizeOfData), SizeOfDataOffset, SizeOfData);
                    break;

                case 6:
                    structWriter.WriteField("Ordinal / Hint", Ordinal/HintOffset, Ordinal); //Don't know what "grf" refers to
                    break;

                case 7:
                    structWriter.WriteBitField(nameof(Type), dataOffset, Type, sizeof(ushort), 2);
                    break;

                case 8:
                    structWriter.WriteBitField(nameof(NameType), dataOffset, NameType, sizeof(ushort), 3);
                    break;

                case 9:
                    structWriter.WriteBitField(nameof(Reserved), dataOffset, Reserved, sizeof(ushort), 11);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
