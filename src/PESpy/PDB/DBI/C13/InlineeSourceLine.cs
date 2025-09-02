using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    //InlineeSourceLine
    [DebuggerDisplay("{inlinee.ToString(),nq} (Line {sourceLineNum})")]
    public unsafe readonly struct InlineeSourceLine
    {
        /// <summary>
        /// function id.
        /// </summary>
        public TypOrEnumType inlinee => new TypOrEnumType(chunk.Pointer, (CV_ItemId) chunk.PeekInt32(0));

        /// <summary>
        /// offset into file table DEBUG_S_FILECHKSMS
        /// </summary>
        public CV_off32_t fileId => chunk.PeekInt32(4);

        /// <summary>
        /// definition start line number.
        /// </summary>
        public CV_off32_t sourceLineNum => chunk.PeekInt32(8);

        internal const int StructSize =
            sizeof(int) + //inlinee
            sizeof(int) + //fileId
            sizeof(int);  //sourceLineNum

        private readonly MemoryChunk chunk;

        internal InlineeSourceLine(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }
    }
}
