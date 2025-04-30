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

        public CV_Column_t[] columns => throw new NotImplementedException(); //todo

        public int Offset => chunk.AbsoluteOffset;

        private readonly MemoryChunk chunk;

        internal CvDebugSLinesFileBlockHeader(in MemoryChunk chunk, CV_LINES flags)
        {
            this.chunk = chunk;
            this.lines = default!;

            var lines = new CvLine[nLines];

            for (var i = 0; i < lines.Length; i++)
                lines[i] = new CvLine(chunk.Slice(12 + (i * CvLine.StructSize)));

            this.lines = lines;
            Debug.Assert(!flags.HasFlag(CV_LINES.HAVE_COLUMNS), "Need to add support for columns");
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct("CV_DebugSLinesFileBlockHeader_t", this, ViewKind.CvDebugSLinesFileBlockHeader);

            s.WriteField(nameof(nLines), nLines);
            s.WriteField(nameof(cbBlock), cbBlock);
            s.WriteInline(lines);

            //todo: columns
        }
    }
}
