using System;
using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    [DebuggerDisplay("{inlinee.ToString(),nq} (Line {sourceLineNum})")]
    public readonly unsafe struct InlineeSourceLineEx
    {
        public TypOrEnumType inlinee => new TypOrEnumType(chunk.Pointer, (CV_ItemId) chunk.PeekInt32(0));       // function id
        public CV_off32_t fileId => chunk.PeekInt32(4);        // offset into file table DEBUG_S_FILECHKSMS
        public CV_off32_t sourceLineNum => chunk.PeekInt32(8); // definition start line number
        public int countOfExtraFiles => chunk.PeekInt32(12);

        public NativeSpan<CV_off32_t> extraFileId => chunk.PeekNativeSpan<CV_off32_t>(16, countOfExtraFiles);

        public int Offset => chunk.AbsoluteOffset;

        internal const int FixedStructSize =
            sizeof(int) + //inlinee
            sizeof(int) + //fileId
            sizeof(int) + //sourceLineNum
            sizeof(int); //countOfExtraFiles

        private readonly MemoryChunk chunk;

        internal InlineeSourceLineEx(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }
    }
}
