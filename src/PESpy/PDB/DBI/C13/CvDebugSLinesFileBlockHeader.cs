using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    //CV_DebugSLinesFileBlockHeader_t
    public readonly struct CvDebugSLinesFileBlockHeader : IValue, IViewable
    {
        private const int offFileOffset = 0;
        private const int nLinesOffset = 4;
        private const int cbBlockOffset = 8;

        /* This property returns the relative offset of the CV_FileCheckSum record of this file
         * from the beginning of DEBUG_S_FILECHKSMS. PDB1 creates a mapping from the index of
         * the file in the CV_FileCheckSum[] and converts to and from this when you call
         * EnumLines::GetLinesColumns/ Mod1::QueryFileNameInfo */
        public CV_off32_t offFile => chunk.PeekInt32(offFileOffset);

        public CV_off32_t nLines => chunk.PeekInt32(nLinesOffset);

        public CV_off32_t cbBlock => chunk.PeekInt32(cbBlockOffset);

        public CvLine[] lines { get; }

        public CV_Column_t[] columns { get; }

        public int Offset => chunk.AbsoluteOffset;

        internal static int FixedStructSize =
            sizeof(int) + //offFile
            sizeof(int) + //nLines
            sizeof(int); //cbBlock

        internal int StructSize
        {
            get
            {
                var size = FixedStructSize + (lines.Length * CvLine.StructSize);

                if (columns != null)
                    size += (columns.Length * 4);

                return size;
            }
        }

        private readonly MemoryChunk chunk;

        internal CvDebugSLinesFileBlockHeader(in MemoryChunk chunk, CV_LINES flags)
        {
            this.chunk = chunk;
            this.lines = default!;
            columns = default;

            var lines = new CvLine[nLines];

            for (var i = 0; i < lines.Length; i++)
                lines[i] = new CvLine(chunk.Slice(FixedStructSize + (i * CvLine.StructSize)));

            this.lines = lines;
            Debug.Assert((flags & CV_LINES.HAVE_COLUMNS) == 0, "Need to add support for columns");
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.CV_DebugSLinesFileBlockHeader_t, this, ViewKind.CvDebugSLinesFileBlockHeader, StructSize);

        int IViewable.NumChildren() => 2 + lines.Length;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(nLines), nLinesOffset, nLines);
                    break;

                case 1:
                    structWriter.WriteField(nameof(cbBlock), cbBlockOffset, cbBlock);
                    break;

                default:
                    structWriter.WriteInline(lines[index - 2]);
                    break;
            }
        }

        //We don't have a ToString or DebuggerDisplay that shows the filename, as that requires
        //that we ship the MODI all the way to this struct. I thought of maybe having a custom memory block
        //that we wrap the page block in (or maybe we derive from it so we can do checks if the block is a page block properly)
        //but ultimately I don't think the allocations are worth it. PDBFileModule60Symbol in SymHelp
        //demonstrates how the filename can be retrieved from the offFile
    }
}
