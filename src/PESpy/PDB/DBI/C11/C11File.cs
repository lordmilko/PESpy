namespace PESpy.PDB
{
    //Type is made up
    public readonly struct C11File
    {
        private const int SPBOffset = 0;
        private int StartEndOffset => SPB.StructSize;
        private unsafe int FileNameOffset => SPB.StructSize + (SPB.cSeg * sizeof(SE));
        private int SPOOffset
        {
            get
            {
                var fileNameOffset = FileNameOffset;

                var str = chunk.PeekSymString(fileNameOffset);

                return fileNameOffset + str.Length + 1;
            }
        }

        public SPB SPB => new SPB(chunk);

        public NativeSpan<SE> StartEnd => chunk.PeekNativeSpan<SE>(SPB.StructSize, SPB.cSeg);

        //Mod1::QueryLines has special logic depending on whether or not the PDB is SZ format or not,
        //and indeed I've seen that you can have a Modi60 with null terminated strings, but a Modi (v4)
        //with length prefixed
        public unsafe SymString FileName => chunk.PeekSymString(FileNameOffset);

        //FileName needs to be 32-bit aligned

        public unsafe SPO[] SPO
        {
            get
            {
                //The offsets are basically 4 byte aligned positions located after the name,
                //except the gotcha here is that they're relative to the root!
                var offsets = SPB.baseSrcLn;

                var results = new SPO[offsets.Length];

                for (var i = 0; i < results.Length; i++)
                    results[i] = new SPO(chunk.Slice(offsets[i] - fileOffset));

                return results;
            }
        }

        //If cPair is odd, align another short

        private readonly MemoryChunk chunk;
        private readonly int fileOffset; //The offset of this file from the root

        internal C11File(in MemoryChunk chunk, int fileOffset)
        {
            this.chunk = chunk;
            this.fileOffset = fileOffset;
        }

        public override string ToString()
        {
            return FileName.ToString();
        }
    }
}
