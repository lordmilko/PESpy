using System.Collections.Generic;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    //CV_DebugSLinesHeader_t
    public class CvDebugSLinesHeader : IValue, IViewable
    {
        public CV_off32_t offCon => chunk.PeekInt32(0);
        public short segCon => chunk.PeekInt16(4);
        public CV_LINES flags => (CV_LINES) chunk.PeekUInt16(6);
        public int cbCon => chunk.PeekInt32(8);

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

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(offCon), offCon);
            s.WriteField(nameof(segCon), segCon);
            s.WriteField(nameof(flags), flags, sizeof(short));
            s.WriteField(nameof(cbCon), cbCon);

            s.WriteInline(FileBlocks);

            return s.ToArray();
        }
    }
}
