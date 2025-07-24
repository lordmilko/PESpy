using System;
using ClrDebug;
using PESpy.Native;
using PESpy.View;

namespace PESpy
{
    public readonly struct ImageSeparateDebugHeader : IValue, IViewable
    {
        public const ushort IMAGE_SEPARATE_DEBUG_SIGNATURE = 0x4944; //DI

        public ushort Signature => chunk.PeekUInt16(0);

        public short Flags => chunk.PeekInt16(2);

        public IMAGE_FILE_MACHINE Machine => (IMAGE_FILE_MACHINE) chunk.PeekUInt16(4);

        public ImageFile Characteristics => (ImageFile) chunk.PeekUInt16(6);

        public uint TimeDateStamp => chunk.PeekUInt32(8);

        public uint CheckSum => chunk.PeekUInt32(12);

        public uint ImageBase => chunk.PeekUInt32(16);

        public int SizeOfImage => chunk.PeekInt32(20);

        public int NumberOfSections => chunk.PeekInt32(24);

        public int ExportedNamesSize => chunk.PeekInt32(28);

        public int DebugDirectorySize => chunk.PeekInt32(32);

        public int SectionAlignment => chunk.PeekInt32(36);

        public NativeSpan<int> Reserved => chunk.PeekNativeSpan<int>(40, 2);

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

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(Signature), Signature);
            s.WriteField(nameof(Flags), Flags);
            s.WriteField(nameof(Machine), Machine, sizeof(short));
            s.WriteField(nameof(Characteristics), Characteristics, sizeof(short));
            s.WriteField(nameof(TimeDateStamp), TimeDateStamp);
            s.WriteField(nameof(CheckSum), CheckSum);
            s.WriteField(nameof(ImageBase), ImageBase);
            s.WriteField(nameof(SizeOfImage), SizeOfImage);
            s.WriteField(nameof(NumberOfSections), NumberOfSections);
            s.WriteField(nameof(ExportedNamesSize), ExportedNamesSize);
            s.WriteField(nameof(DebugDirectorySize), DebugDirectorySize);
            s.WriteField(nameof(SectionAlignment), SectionAlignment);
            s.WriteField(nameof(Reserved), Reserved);

            return s.ToArray();
        }
    }
}
