using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy.PDB
{
    //CV_Line_t
    [DebuggerDisplay("offset = {offset}, linenumStart = {linenumStart}, deltaLineEnd = {deltaLineEnd}, fStatement = {fStatement}")]
    public readonly struct CvLine : IValue, IViewable
    {
        private const int offsetOffset = 0;
        private const int flagsOffset = 4;

        /// <summary>
        /// Offset to start of code bytes for line number
        /// </summary>
        public int offset => chunk.PeekInt32(offsetOffset);

        /// <summary>
        /// line where statement/expression starts
        /// </summary>
        public int linenumStart => (int) (flags & 0x00FFFFFF);

        /// <summary>
        /// delta to line where statement ends (optional)
        /// </summary>
        public int deltaLineEnd => (int) (flags >> 24) & 0x7F;

        /// <summary>
        /// true if a statement linenumber, else an expression line num
        /// </summary>
        public bool fStatement => ((flags >> 31) & 0x1) != 0;

        private uint flags => chunk.PeekUInt32(flagsOffset);

        public int Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(int) + //offset
            sizeof(int); //flags

        private readonly MemoryChunk chunk;

        internal CvLine(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.CV_Line_t, this, ViewKind.CvLine, StructSize);

        int IViewable.NumChildren() => 4;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(offset), offsetOffset, offset);
                    break;

                #region BitField

                case 1:
                    structWriter.WriteBitField(nameof(linenumStart), flagsOffset, linenumStart, sizeof(int), 24);
                    break;

                case 2:
                    structWriter.WriteBitField(nameof(deltaLineEnd), flagsOffset, deltaLineEnd, sizeof(int), 7);
                    break;

                case 3:
                    structWriter.WriteBitField(nameof(fStatement), flagsOffset, fStatement, sizeof(int), 1);
                    break;

                #endregion

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
