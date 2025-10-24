using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    //CV_DebugSLinesHeader_t
    public class CvDebugSLinesHeader : IViewableValue //It's going to be boxed, and also it stores a big array which we don't want to lose
    {
        private const int offConOffset = 0;
        private const int segConOffset = 4;
        private const int flagsOffset = 6;
        private const int cbConOffset = 8;

        public CV_off32_t offCon => chunk.PeekInt32(offConOffset); //Relative offset within segment
        public short segCon => chunk.PeekInt16(segConOffset); //1-based segment number
        public CV_LINES flags => (CV_LINES) chunk.PeekUInt16(flagsOffset);
        public int cbCon => chunk.PeekInt32(cbConOffset); //Total number of bytes represented by this area. The difference between the last line and this gives you the length of the last line

        private CvDebugSLinesFileBlockHeader[]? fileBlocks;

        public CvDebugSLinesFileBlockHeader[] FileBlocks
        {
            get
            {
                if (fileBlocks == null)
                {
                    var read = 12;

                    using var results = new PooledList<CvDebugSLinesFileBlockHeader>();

                    while (read < length)
                    {
                        var file = new CvDebugSLinesFileBlockHeader(chunk.Slice(read), flags);
                        results.Add(file);
                        read += file.cbBlock;
                    }

                    fileBlocks = results.ToArray();
                }

                return fileBlocks;
            }
        }

        public int Offset => chunk.AbsoluteOffset;

        private readonly MemoryChunk chunk;
        private int length;

        internal CvDebugSLinesHeader(in MemoryChunk chunk, int length)
        {
            this.chunk = chunk;
            this.length = length;

            Debug.Assert((flags & CV_LINES.HAVE_COLUMNS) == 0, "Need to add support for columns");

#if STRESS_TEST
            _ = FileBlocks;
#endif
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.CV_DebugSLinesHeader_t, this, ViewKind.CvDebugSLinesHeader, length);

        int IViewable.NumChildren() => 4 + FileBlocks.Length;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(offCon), offConOffset, offCon);
                    break;

                case 1:
                    structWriter.WriteField(nameof(segCon), segConOffset, segCon);
                    break;

                case 2:
                    structWriter.WriteField(nameof(flags), flagsOffset, flags, sizeof(short));
                    break;

                case 3:
                    structWriter.WriteField(nameof(cbCon), cbConOffset, cbCon);
                    break;

                default:
                    structWriter.WriteInline(FileBlocks[index - 4]);
                    break;
            }
        }
    }
}
