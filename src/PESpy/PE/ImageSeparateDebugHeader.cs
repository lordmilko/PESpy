using System;
using System.Diagnostics;
using ClrDebug;
using PESpy.View;

namespace PESpy
{
    public readonly struct ImageSeparateDebugHeader : IValue, IViewable
    {
        public const ushort IMAGE_SEPARATE_DEBUG_SIGNATURE = 0x4944; //DI

        private const int SignatureOffset = 0;
        private const int FlagsOffset = 2;
        private const int MachineOffset = 4;
        private const int CharacteristicsOffset = 6;
        private const int TimeDateStampOffset = 8;
        private const int CheckSumOffset = 12;
        private const int ImageBaseOffset = 16;
        private const int SizeOfImageOffset = 20;
        private const int NumberOfSectionsOffset = 24;
        private const int ExportedNamesSizeOffset = 28;
        private const int DebugDirectorySizeOffset = 32;
        private const int SectionAlignmentOffset = 36;
        private const int ReservedOffset = 40;

        public ushort Signature => chunk.PeekUInt16(SignatureOffset);

        public short Flags => chunk.PeekInt16(FlagsOffset);

        public IMAGE_FILE_MACHINE Machine => (IMAGE_FILE_MACHINE) chunk.PeekUInt16(MachineOffset);

        public IMAGE_FILE Characteristics => (IMAGE_FILE) chunk.PeekUInt16(CharacteristicsOffset);

        public uint TimeDateStamp => chunk.PeekUInt32(TimeDateStampOffset);

        public uint CheckSum => chunk.PeekUInt32(CheckSumOffset);

        public uint ImageBase => chunk.PeekUInt32(ImageBaseOffset);

        public int SizeOfImage => chunk.PeekInt32(SizeOfImageOffset);

        public int NumberOfSections => chunk.PeekInt32(NumberOfSectionsOffset);

        public int ExportedNamesSize => chunk.PeekInt32(ExportedNamesSizeOffset);

        public int DebugDirectorySize => chunk.PeekInt32(DebugDirectorySizeOffset);

        public int SectionAlignment => chunk.PeekInt32(SectionAlignmentOffset);

        public NativeSpan<int> Reserved => chunk.PeekNativeSpan<int>(ReservedOffset, 2);

        public int Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(short) + //Signature
            sizeof(short) + //Flags
            sizeof(short) + //Machine
            sizeof(short) + //Characteristics
            sizeof(int) + //TimeDateStamp
            sizeof(int) + //CheckSum
            sizeof(int) + //ImageBase
            sizeof(int) + //SizeOfImage
            sizeof(int) + //NumberOfSections
            sizeof(int) + //ExportedNamesSize
            sizeof(int) + //DebugDirectorySize
            sizeof(int) + //SectionAlignment
            8; //Reserved

        private readonly MemoryChunk chunk;

        internal ImageSeparateDebugHeader(in MemoryChunk chunk)
        {
            this.chunk = chunk;

            if (Signature != IMAGE_SEPARATE_DEBUG_SIGNATURE)
                throw new BadImageFormatException("Invalid Debug Signature");
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.IMAGE_SEPARATE_DEBUG_HEADER, this, ViewKind.ImageSeparateDebugHeader, StructSize);

        int IViewable.NumChildren() => 13;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(Signature), SignatureOffset, Signature);
                    break;

                case 1:
                    structWriter.WriteField(nameof(Flags), FlagsOffset, Flags);
                    break;

                case 2:
                    structWriter.WriteField(nameof(Machine), MachineOffset, Machine, sizeof(short));
                    break;

                case 3:
                    structWriter.WriteField(nameof(Characteristics), CharacteristicsOffset, Characteristics, sizeof(short));
                    break;

                case 4:
                    structWriter.WriteField(nameof(TimeDateStamp), TimeDateStampOffset, TimeDateStamp);
                    break;

                case 5:
                    structWriter.WriteField(nameof(CheckSum), CheckSumOffset, CheckSum);
                    break;

                case 6:
                    structWriter.WriteField(nameof(ImageBase), ImageBaseOffset, ImageBase);
                    break;

                case 7:
                    structWriter.WriteField(nameof(SizeOfImage), SizeOfImageOffset, SizeOfImage);
                    break;

                case 8:
                    structWriter.WriteField(nameof(NumberOfSections), NumberOfSectionsOffset, NumberOfSections);
                    break;

                case 9:
                    structWriter.WriteField(nameof(ExportedNamesSize), ExportedNamesSizeOffset, ExportedNamesSize);
                    break;

                case 10:
                    structWriter.WriteField(nameof(DebugDirectorySize), DebugDirectorySizeOffset, DebugDirectorySize);
                    break;

                case 11:
                    structWriter.WriteField(nameof(SectionAlignment), SectionAlignmentOffset, SectionAlignment);
                    break;

                case 12:
                    structWriter.WriteField(nameof(Reserved), ReservedOffset, Reserved);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
