using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy
{
    [Source(SourceKind.cvexefmt_h)]
    public struct OMFSourceFile : IValue, IViewable
    {
        private const int cSegOffset = 0;
        private const int reservedOffset = 2;
        private const int baseSrcLnOffset = 4;
        private int rangesOffset => baseSrcLnOffset + (cSeg * sizeof(int));
        private unsafe int cFNameOffset => baseSrcLnOffset + (cSeg * (sizeof(int) + sizeof(RANGE)));

        public ushort cSeg => chunk.PeekUInt16(cSegOffset);

        public ushort reserved => chunk.PeekUInt16(reservedOffset);

        private OMFSourceLine[]? rawBaseSrcLn;

        public OMFSourceLine[] baseSrcLn
        {
            get
            {
                if (rawBaseSrcLn == null)
                {
                    var offsets = chunk.PeekSpan<int>(baseSrcLnOffset, cSeg);

                    var results = new OMFSourceLine[offsets.Length];

                    for (var i = 0; i < results.Length; i++)
                        results[i] = new OMFSourceLine(new MemoryChunk(chunk.block, rootOffset + offsets[i]));

                    rawBaseSrcLn = results;
                }

                return rawBaseSrcLn;
            }
        }

        //Name is made up
        public NativeSpan<RANGE> ranges => chunk.PeekNativeSpan<RANGE>(rangesOffset, cSeg);

        //The spec says its 2 bytes, but microsoft-pdb treats it as 1 byte. Furthermore,
        //microsoft-pdb seems to think there's a possibility the string could be UTF8. I don't see how.
        //OMF means there's no PDB, and I don't think you would have CV 13 symbols when using OMF either
        public unsafe byte cFName => chunk.PeekByte(cFNameOffset);

        public unsafe FixedAnsiString Name
        {
            get
            {
                //We need the offset after cFName, so we compute both its location and value inline
                var lengthOffset = cFNameOffset;

                var length = chunk.PeekByte(lengthOffset);

                //While OMFSourceFile says that the Name is padded to maintain 32-bit alignment, I don't think we need to worry about this; there's nothing after the Name in this structure, and the way
                //OMFSourceFile works is that we're told the exact start of each item relative to OMFSourceModule. So any padding will naturally be ignored
                return chunk.PeekAnsiFixedLength(lengthOffset + 1, length);
            }
        }

        public long Offset => chunk.AbsoluteOffset;

        internal int StructSize
        {
            get
            {
                var lengthOffset = cFNameOffset;

                var length = chunk.PeekByte(lengthOffset);

                var padding = (4 - ((length + 1) & 3)) & 3;

                return lengthOffset + 1 + length + padding;
            }
        }

        private readonly MemoryChunk chunk;
        private readonly int rootOffset;

        internal OMFSourceFile(in MemoryChunk chunk, int rootOffset)
        {
            this.chunk = chunk;
            this.rootOffset = rootOffset;
            rawBaseSrcLn = default;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //baseSrcLn points to an array of offsets to OMFSourceLine items,
            //so technically speaking each baseSrcLn element is an xref

            var offsets = chunk.PeekSpan<int>(baseSrcLnOffset, cSeg);

            var structOffset = Offset;

            for (var i = 0; i < offsets.Length; i++)
            {
                var sourceLine = new OMFSourceLine(new MemoryChunk(chunk.block, rootOffset + offsets[i]));
                writer.WriteGlobal(sourceLine);

                //Offsets are relative to the start of the OMFSourceModule
                writer.WriteOffsetXRef(structOffset, baseSrcLnOffset + (i * sizeof(int)), sourceLine.Offset);
            }

            //Name is not an xref; it's part of the struct
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.OMFSourceFile, StructSize);

        int IViewable.NumChildren() =>((cFName + 1) & 3) != 0 ? 7 : 6;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(cSeg), cSegOffset, cSeg);
                    break;

                case 1:
                    structWriter.WriteField(nameof(reserved), reservedOffset, reserved);
                    break;

                case 2:
                    structWriter.WriteField(nameof(baseSrcLn), baseSrcLnOffset, chunk.PeekNativeSpan<int>(baseSrcLnOffset, cSeg));
                    break;

                case 3:
                    structWriter.WriteField(nameof(ranges), rangesOffset, ranges);
                    break;

                case 4:
                    structWriter.WriteField(nameof(cFName), cFNameOffset, cFName);
                    break;

                case 5:
                    structWriter.WriteAnsiFixedLengthField(nameof(Name), cFNameOffset + 1, Name);
                    break;

                case 6:
                    //Check if there's any padding
                    var padding = (4 - ((cFName + 1) & 3)) & 3;

                    if (padding == 0)
                        throw new IndexOutOfRangeException();

                    structWriter.WriteByteBlob(cFNameOffset + 1 + cFName, padding);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
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
