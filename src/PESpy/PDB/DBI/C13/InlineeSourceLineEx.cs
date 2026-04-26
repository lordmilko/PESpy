using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    [DebuggerDisplay("{inlinee.ToString(),nq} (Line {sourceLineNum})")]
    public readonly unsafe struct InlineeSourceLineEx : IViewableValue
    {
        private const int inlineeOffset = 0;
        private const int fileIdOffset = 4;
        private const int sourceLineNumOffset = 8;
        private const int countOfExtraFilesOffset = 12;
        private const int extraFileIdOffset = 16;

        public TypOrEnumType inlinee => new TypOrEnumType(chunk.Pointer, (CV_ItemId) chunk.PeekInt32(inlineeOffset));       // function id
        public CV_off32_t fileId => chunk.PeekInt32(fileIdOffset);        // offset into file table DEBUG_S_FILECHKSMS
        public CV_off32_t sourceLineNum => chunk.PeekInt32(sourceLineNumOffset); // definition start line number
        public int countOfExtraFiles => chunk.PeekInt32(countOfExtraFilesOffset);

        public NativeSpan<CV_off32_t> extraFileId => chunk.PeekNativeSpan<CV_off32_t>(extraFileIdOffset, countOfExtraFiles);

        public int Offset => chunk.AbsoluteOffset;

        internal const int FixedStructSize =
            sizeof(int) + //inlinee
            sizeof(int) + //fileId
            sizeof(int) + //sourceLineNum
            sizeof(int); //countOfExtraFiles

        internal int StructSize => FixedStructSize + (countOfExtraFiles * sizeof(int));

        private readonly MemoryChunk chunk;

        internal InlineeSourceLineEx(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        public static implicit operator InlineeSourceLine(InlineeSourceLineEx value) => new InlineeSourceLine(value.chunk);

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.InlineeSourceLineEx, StructSize);

        int IViewable.NumChildren() => 5;

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

                case 3:
                    structWriter.WriteField(nameof(countOfExtraFiles), countOfExtraFilesOffset, countOfExtraFiles);
                    break;

                case 4:
                    structWriter.WriteField(nameof(extraFileId), extraFileIdOffset, extraFileId);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
