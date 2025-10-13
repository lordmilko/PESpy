using System;
using System.Diagnostics;

namespace PESpy
{
    [Source(SourceKind.cvexefmt)]
    public struct OMFSourceFile : IValue
    {
        public ushort cSeg => chunk.PeekUInt16(0);

        public ushort reserved => chunk.PeekUInt16(2);

        private OMFSourceLine[]? rawBaseSrcLn;

        public OMFSourceLine[] baseSrcLn
        {
            get
            {
                if (rawBaseSrcLn == null)
                {
                    var offsets = chunk.PeekSpan<int>(4, cSeg);

                    var results = new OMFSourceLine[offsets.Length];

                    for (var i = 0; i < results.Length; i++)
                        results[i] = new OMFSourceLine(new MemoryChunk(chunk.block, rootOffset + offsets[i]));

                    rawBaseSrcLn = results;
                }

                return rawBaseSrcLn;
            }
        }

        public NativeSpan<RANGE> ranges //Name is made up
        {
            get
            {
                var count = cSeg;
                return chunk.PeekNativeSpan<RANGE>(4 + (count * sizeof(int)), count);
            }
        }

        //The spec says its 2 bytes, but microsoft-pdb treats it as 1 byte. Furthermore,
        //microsoft-pdb seems to think there's a possibility the string could be UTF8. I don't see how.
        //OMF means there's no PDB, and I don't think you would have CV 13 symbols when using OMF either
        public byte cFName => chunk.PeekByte(4 + (cSeg * (sizeof(int) + 8)));

        public FixedAnsiString Name
        {
            get
            {
                //We need the offset after cFName, so we compute both its location and value inline
                var lengthOffset = 4 + (cSeg * (sizeof(int) + sizeof(ulong)));

                var length = chunk.PeekByte(lengthOffset);

                //While OMFSourceFile says that the Name is padded to maintain 32-bit alignment, I don't think we need to worry about this; there's nothing after the Name in this structure, and the way
                //OMFSourceFile works is that we're told the exact start of each item relative to OMFSourceModule. So any padding will naturally be ignored
                return chunk.PeekAnsiFixedLength(lengthOffset + 1, length);
            }
        }

        public int Offset => chunk.AbsoluteOffset;

        private readonly MemoryChunk chunk;
        private readonly int rootOffset;

        internal OMFSourceFile(in MemoryChunk chunk, int rootOffset)
        {
            this.chunk = chunk;
            this.rootOffset = rootOffset;
            rawBaseSrcLn = default;
        }

        //Struct is defined inline in cvdump.cpp!DumpLines
        [DebuggerDisplay("start = {start}, end = {end}")]
        public struct RANGE
        {
            public int start;
            public int end;
        }

        public override string ToString()
        {
            return Name.ToString();
        }
    }
}
