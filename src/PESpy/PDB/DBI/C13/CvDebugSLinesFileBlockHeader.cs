using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    //CV_DebugSLinesFileBlockHeader_t
    public readonly struct CvDebugSLinesFileBlockHeader : IValue, IViewable
    {
        public CV_off32_t offFile => chunk.PeekInt32(0);

        public CV_off32_t nLines => chunk.PeekInt32(4);

        public CV_off32_t cbBlock => chunk.PeekInt32(8);

        public CvLine[] lines { get; }

        public CV_Column_t[] columns { get; }

        public int Offset => chunk.AbsoluteOffset;

        internal static int FixedStructSize =
            sizeof(int) + //offFile
            sizeof(int) + //nLines
            sizeof(int); //cbBlock

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
            Debug.Assert(!flags.HasFlag(CV_LINES.HAVE_COLUMNS), "Need to add support for columns");
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct("CV_DebugSLinesFileBlockHeader_t", this, ViewKind.CvDebugSLinesFileBlockHeader, FixedStructSize + (lines.Length * CvLine.StructSize) + (columns.Length * 4));

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(nLines), nLines);
            s.WriteField(nameof(cbBlock), cbBlock);
            s.WriteInline(lines);

            Debug.Assert(columns.Length == 0); //todo

            return s.ToArray();
        }
    }
}
