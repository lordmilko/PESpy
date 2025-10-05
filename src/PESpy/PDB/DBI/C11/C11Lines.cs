namespace PESpy.PDB
{
    //The format of C11 lines is defined in mli.cpp and mod.cpp

    //Type is made up
    public class C11Lines //May not be present
    {
        public FSB FSB { get; }

        public NativeSpan<SE> StartEnd
        {
            get
            {
                var fsb = FSB;
                return chunk.PeekNativeSpan<SE>(fsb.StructSize, fsb.cSeg);
            }
        }

        public unsafe NativeSpan<int> SectionNumbers
        {
            get
            {
                var fsb = FSB;
                var offset = fsb.StructSize + (fsb.cSeg * SE.StructSize);
                return chunk.PeekNativeSpan<int>(offset, fsb.cSeg);
            }
        }

        //The data after the section numbers must be aligned to a 32-bit boundary

        private C11File[]? files;

        public C11File[] Files
        {
            get
            {
                if (files == null)
                {
                    var baseSrcFile = FSB.baseSrcFile;

                    var results = new C11File[baseSrcFile.Length];

                    for (var i = 0; i < results.Length; i++)
                        results[i] = new C11File(chunk.Slice(baseSrcFile[i]));

                    files = results;
                }

                return files;
            }
        }

        private readonly MemoryChunk chunk;

        internal C11Lines(in MemoryChunk chunk)
        {
            FSB = new FSB(chunk);

            this.chunk = chunk;
        }
    }
}
