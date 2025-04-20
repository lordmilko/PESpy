using System;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    public readonly struct InlineeSourceLineEx
    {
        public CV_ItemId inlinee => chunk.PeekInt32(0);       // function id
        public CV_off32_t fileId => chunk.PeekInt32(4);        // offset into file table DEBUG_S_FILECHKSMS
        public CV_off32_t sourceLineNum => chunk.PeekInt32(8); // definition start line number
        public int countOfExtraFiles => chunk.PeekInt32(12);

        public Span<CV_off32_t> extraFileId => chunk.PeekSpan<CV_off32_t>(16, countOfExtraFiles);

        private readonly MemoryChunk chunk;

        internal InlineeSourceLineEx(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }
    }
}
