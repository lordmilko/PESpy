namespace PESpy
{
    //CORCOMPILE_IMPORT_SECTION

    /// <summary>
    /// CORCOMPILE_IMPORT_SECTION describes image range with references to other assemblies or runtime data structures
    /// 
    /// There is number of different types of these ranges: eagerly initialized at image load vs. lazily initialized at method entry 
    /// vs. lazily initialized on first use; hot vs. cold, handles vs. code pointers, etc.
    /// </summary>
    [Source(SourceKind.corcompile_h)]
    public readonly struct CorCompileImportSection
    {
        private const int SectionOffset = 0;
        private const int FlagsOffset = 8;
        private const int TypeOffset = 10;
        private const int EntrySizeOffset = 11;
        private const int SignaturesOffset = 12;
        private const int AuxiliaryDataOffset = 16;

        //Need to read correct data based on Flags/Type (see nidump.cpp for some examples)

        /// <summary>
        /// Section containing values to be fixed up
        /// </summary>
        public ImageDataDirectory Section => new ImageDataDirectory(chunk);

        /// <summary>
        /// One or more of CorCompileImportFlags
        /// </summary>
        public CorCompileImportFlags Flags => (CorCompileImportFlags) chunk.PeekUInt16(FlagsOffset);

        /// <summary>
        /// One of CorCompileImportType
        /// </summary>
        public CorCompileImportType Type => (CorCompileImportType) chunk.PeekByte(TypeOffset);

        public byte EntrySize => chunk.PeekByte(EntrySizeOffset);

        //Not sure what this is an RVA to

        /// <summary>
        /// RVA of optional signature descriptors
        /// </summary>
        public int Signatures => chunk.PeekInt32(SignaturesOffset);

        //Not sure what this is an RVA to; seems to be a pGCRefMap?

        /// <summary>
        /// RVA of optional auxiliary data (typically GC info)*/
        /// </summary>
        public int AuxiliaryData => chunk.PeekInt32(AuxiliaryDataOffset);

        public int Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            8 + //Section
            sizeof(short) + //Flags
            sizeof(byte) + //Type
            sizeof(byte) + //EntrySize
            sizeof(int) + //Signatures
            sizeof(int); //AuxiliaryData

        private readonly MemoryChunk chunk;

        internal CorCompileImportSection(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }
    }
}
