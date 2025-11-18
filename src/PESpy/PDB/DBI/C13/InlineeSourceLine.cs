using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    //InlineeSourceLine
    [DebuggerDisplay("{inlinee.ToString(),nq} (Line {sourceLineNum})")]
    public unsafe readonly struct InlineeSourceLine : IViewableValue
    {
        private const int inlineeOffset = 0;
        private const int fileIdOffset = 4;
        private const int sourceLineNumOffset = 8;

        /// <summary>
        /// function id.
        /// </summary>
        public TypOrEnumType inlinee => new TypOrEnumType(chunk.Pointer, (CV_ItemId) chunk.PeekInt32(inlineeOffset));

        /// <summary>
        /// offset into file table DEBUG_S_FILECHKSMS
        /// </summary>
        public CV_off32_t fileId => chunk.PeekInt32(fileIdOffset);

        /// <summary>
        /// definition start line number.
        /// </summary>
        public CV_off32_t sourceLineNum => chunk.PeekInt32(sourceLineNumOffset);

        public int Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(int) + //inlinee
            sizeof(int) + //fileId
            sizeof(int);  //sourceLineNum

        private readonly MemoryChunk chunk;

        internal InlineeSourceLine(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.InlineeSourceLine, this, ViewKind.InlineeSourceLine, StructSize);

        int IViewable.NumChildren() => 3;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(inlinee), inlineeOffset, (CV_ItemId) inlinee);
                    break;

                case 1:
                    structWriter.WriteField(nameof(fileId), fileIdOffset, fileId);
                    break;

                case 2:
                    structWriter.WriteField(nameof(sourceLineNum), sourceLineNumOffset, sourceLineNum);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
