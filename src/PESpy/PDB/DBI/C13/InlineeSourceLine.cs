using ClrDebug.PDB;

namespace PESpy.PDB
{
    //InlineeSourceLine
    public readonly struct InlineeSourceLine
    {
        /// <summary>
        /// function id.
        /// </summary>
        public CV_ItemId inlinee => chunk.PeekInt32(0);

        /// <summary>
        /// offset into file table DEBUG_S_FILECHKSMS
        /// </summary>
        public CV_off32_t fileId => chunk.PeekInt32(4);

        /// <summary>
        /// definition start line number.
        /// </summary>
        public CV_off32_t sourcLineNum => chunk.PeekInt32(8);

        private readonly MemoryChunk chunk;

        internal InlineeSourceLine(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }
    }
}
