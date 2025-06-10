#if PEFAST
using ClrDebug;

namespace PESpy
{
    public class CorCompileHeader : IValue
    {
        internal const int NGESignature = 0x0045474E; //NGE

        public uint Signature => chunk.PeekUInt32(0);

        public ushort MajorVersion => chunk.PeekUInt16(4);

        public ushort MinorVersion => chunk.PeekUInt16(6);

        public ImageDataDirectory HelperTable => new ImageDataDirectory(chunk.Slice(8));
        public ImageDataDirectory ImportSections => new ImageDataDirectory(chunk.Slice(16));
        public ImageDataDirectory Dummy0 => new ImageDataDirectory(chunk.Slice(24));
        public ImageDataDirectory StubsData => new ImageDataDirectory(chunk.Slice(32));
        public ImageDataDirectory VersionInfo => new ImageDataDirectory(chunk.Slice(40));
        public ImageDataDirectory Dependencies => new ImageDataDirectory(chunk.Slice(48));
        public ImageDataDirectory DebugMap => new ImageDataDirectory(chunk.Slice(56));
        public ImageDataDirectory ModuleImage => new ImageDataDirectory(chunk.Slice(64));
        public ImageDataDirectory CodeManagerTable => new ImageDataDirectory(chunk.Slice(72));
        public ImageDataDirectory ProfileDataList => new ImageDataDirectory(chunk.Slice(80));
        public ImageDataDirectory ManifestMetaData => new ImageDataDirectory(chunk.Slice(88));
        public ImageDataDirectory VirtualSectionsTable => new ImageDataDirectory(chunk.Slice(96));

        public ulong ImageBase => chunk.PeekPointer(104); //The ImageBase can look at bit weird, but it is correct

        public CorCompileHeaderFlags Flags => (CorCompileHeaderFlags) chunk.PeekUInt32(104 + chunk.PointerSize);

        public CorPEKind PEKind => (CorPEKind) chunk.PeekUInt32(108 + chunk.PointerSize);

        public COMIMAGE_FLAGS COR20Flags => (COMIMAGE_FLAGS) chunk.PeekUInt32(112 + chunk.PointerSize);

        public IMAGE_FILE_MACHINE Machine => (IMAGE_FILE_MACHINE) chunk.PeekUInt16(116 + chunk.PointerSize);

        public ImageFile Characteristics => (ImageFile) chunk.PeekUInt16(118 + chunk.PointerSize);

        public ImageDataDirectory EEInfoTable => new ImageDataDirectory(chunk.Slice(120 + chunk.PointerSize));
        public ImageDataDirectory Dummy1 => new ImageDataDirectory(chunk.Slice(128 + chunk.PointerSize));
        public ImageDataDirectory Dummy2 => new ImageDataDirectory(chunk.Slice(136 + chunk.PointerSize));
        public ImageDataDirectory Dummy3 => new ImageDataDirectory(chunk.Slice(144 + chunk.PointerSize));
        public ImageDataDirectory Dummy4 => new ImageDataDirectory(chunk.Slice(152 + chunk.PointerSize));

        public int Offset => chunk.AbsoluteOffset;

        private readonly MemoryChunk chunk;

        internal CorCompileHeader(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }
    }
}
#endif
