namespace PESpy.LE
{
    //exe_vhd.h
    //o32_obj
    public readonly struct o32_obj
    {
        private const int sizeOffset = 0;
        private const int baseOffset = 4;
        private const int flagsOffset = 8;
        private const int pageMapOffset = 12;
        private const int mapSizeOffset = 16;
        private const int reservedOffset = 20;

        /// <summary>
        /// Object virtual size
        /// </summary>
        public int o32_size => chunk.PeekInt32(sizeOffset);

        /// <summary>
        /// Object base virtual address
        /// </summary>
        public int o32_base => chunk.PeekInt32(baseOffset);

        /// <summary>
        /// Attribute flags
        /// </summary>
        public ObjectTableFlags o32_flags => (ObjectTableFlags) chunk.PeekUInt32(flagsOffset);

        /// <summary>
        /// Object page map index
        /// </summary>
        public int o32_pagemap => chunk.PeekInt32(pageMapOffset);

        /// <summary>
        /// Number of entries in object page map
        /// </summary>
        public int o32_mapsize => chunk.PeekInt32(mapSizeOffset); //Number of pages that this object's data spans

        //The first 4 characters of the IMAGE_SECTION_HEADER are copied
        //into reserved by the linker; this is then how IDA Pro knows to show
        //this as the section name
        public FixedAnsiString reserved => chunk.PeekAnsiFixedLength(reservedOffset, 4);

        public int PhysicalOffset => chunk.LEFile().GetPhysicalOffset(this, 0);

        //Gets the code/data bytes that are associated with this object
        public NativeSpan<byte> Bytes
        {
            get
            {
                var leFile = chunk.LEFile();

                return leFile.GetBytes(this);
            }
        }

        public int Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(int) + //Size
            sizeof(int) + //Base
            sizeof(int) + //Flags
            sizeof(int) + //PageMap
            sizeof(int) + //MapSize
            sizeof(int); //Reserved

        private readonly MemoryChunk chunk;

        internal o32_obj(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        public override string ToString()
        {
            return reserved.ToString();
        }
    }
}
