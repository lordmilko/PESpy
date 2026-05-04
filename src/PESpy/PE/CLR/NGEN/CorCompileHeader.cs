using ClrDebug;

namespace PESpy
{
    //IXCLRDataProcess has a method DumpNativeImage that calls into NativeImageDumper
    //and can dump a whole NGEN image, and it's even present in .NET Framewrk 4 mscordacwks!
    //https://github.com/JetBrains/coreclr/blob/master/src/debug/daccess/nidump.cpp#L625

    //CORCOMPILE_HEADER
    [Source(SourceKind.corcompile_h)]
    public class CorCompileHeader : IValue
    {
        internal const int NGESignature = 0x0045474E; //NGE

        public uint Signature => chunk.PeekUInt32(0);

        public ushort MajorVersion => chunk.PeekUInt16(4);

        public ushort MinorVersion => chunk.PeekUInt16(6);

        /// <summary>
        /// Table of function pointers to JIT helpers indexed by helper number
        /// </summary>
        public ImageDataDirectory HelperTable => new ImageDataDirectory(chunk.Slice(8));

        /// <summary>
        /// points to array of code:CORCOMPILE_IMPORT_SECTION
        /// </summary>
        public ImageDataDirectory ImportSections => new ImageDataDirectory(chunk.Slice(16));

        /// <summary>
        /// points to table CORCOMPILE_IMPORT_TABLE_ENTRY
        /// </summary>
        public ImageDataDirectory ImportTable => new ImageDataDirectory(chunk.Slice(24));

        /// <summary>
        /// contains the value to register with the stub manager for the delegate stubs &amp; AMD64 tail call stubs
        /// </summary>
        public ImageDataDirectory StubsData => new ImageDataDirectory(chunk.Slice(32));

        /// <summary>
        /// points to a code:CORCOMPILE_VERSION_INFO
        /// </summary>
        public ImageDataDirectory VersionInfo => new ImageDataDirectory(chunk.Slice(40));

        /// <summary>
        /// points to an array of code:CORCOMPILE_DEPENDENCY
        /// </summary>
        public ImageDataDirectory Dependencies => new ImageDataDirectory(chunk.Slice(48));

        /// <summary>
        /// points to an array of code:CORCOMPILE_DEBUG_RID_ENTRY hashed by method RID
        /// </summary>
        public ImageDataDirectory DebugMap => new ImageDataDirectory(chunk.Slice(56));

        /// <summary>
        /// points to the freeze dried  Module structure
        /// </summary>
        public ImageDataDirectory ModuleImage => new ImageDataDirectory(chunk.Slice(64));

        /// <summary>
        /// points to a code:CORCOMPILE_CODE_MANAGER_ENTRY
        /// </summary>
        public ImageDataDirectory CodeManagerTable => new ImageDataDirectory(chunk.Slice(72));

        /// <summary>
        /// points to the list of code:CORCOMPILE_METHOD_PROFILE_LIST
        /// </summary>
        public ImageDataDirectory ProfileDataList => new ImageDataDirectory(chunk.Slice(80));

        /// <summary>
        /// points to the native manifest metadata
        /// </summary>
        public ImageDataDirectory ManifestMetaData => new ImageDataDirectory(chunk.Slice(88));

        /// <summary>
        /// List of CORCOMPILE_VIRTUAL_SECTION_INFO. Contains a list of Section
        /// ranges for debugging purposes. There is one entry in this table per
        /// ZapVirtualSection in the NGEN image.  This data is used to fire ETW
        /// events that describe the various VirtualSection in the NGEN image. These
        /// events are used for diagnostics and performance purposes. Some of the
        /// questions these events help answer are like : how effective is IBC
        /// training data. They can also be used to have better nidump support for
        /// decoding virtual section information ( start - end ranges for each
        /// virtual section )
        /// </summary>
        public ImageDataDirectory VirtualSectionsTable => new ImageDataDirectory(chunk.Slice(96));

        /// <summary>
        /// Actual image base address (ASLR fakes the image base in PE header while applying relocations in kernel)
        /// </summary>
        public ulong ImageBase => chunk.PeekPointer(104); //The ImageBase can look at bit weird, but it is correct

        /// <summary>
        /// Flags
        /// </summary>
        public CorCompileHeaderFlags Flags => (CorCompileHeaderFlags) chunk.PeekUInt32(104 + chunk.PointerSize);

        /// <summary>
        /// CorPEKind of the original IL image
        /// </summary>
        public CorPEKind PEKind => (CorPEKind) chunk.PeekUInt32(108 + chunk.PointerSize);

        /// <summary>
        /// Cached value of code:IMAGE_COR20_HEADER.Flags from original IL image
        /// </summary>
        public COMIMAGE_FLAGS COR20Flags => (COMIMAGE_FLAGS) chunk.PeekUInt32(112 + chunk.PointerSize);

        /// <summary>
        /// Cached value of _IMAGE_FILE_HEADER.Machine from original IL image
        /// </summary>
        public IMAGE_FILE_MACHINE Machine => (IMAGE_FILE_MACHINE) chunk.PeekUInt16(116 + chunk.PointerSize);

        /// <summary>
        /// Cached value of _IMAGE_FILE_HEADER.Characteristics from original IL image
        /// </summary>
        public IMAGE_FILE Characteristics => (IMAGE_FILE) chunk.PeekUInt16(118 + chunk.PointerSize);

        /// <summary>
        /// points to a code:CORCOMPILE_EE_INFO_TABLE
        /// </summary>
        public ImageDataDirectory EEInfoTable => new ImageDataDirectory(chunk.Slice(120 + chunk.PointerSize));

        //For backward compatibility

        public ImageDataDirectory Dummy1 => new ImageDataDirectory(chunk.Slice(128 + chunk.PointerSize));
        public ImageDataDirectory Dummy2 => new ImageDataDirectory(chunk.Slice(136 + chunk.PointerSize));
        public ImageDataDirectory Dummy3 => new ImageDataDirectory(chunk.Slice(144 + chunk.PointerSize));
        public ImageDataDirectory Dummy4 => new ImageDataDirectory(chunk.Slice(152 + chunk.PointerSize));

        public long Offset => chunk.AbsoluteOffset;

        private readonly MemoryChunk chunk;

        internal CorCompileHeader(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }
    }
}
