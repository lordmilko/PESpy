using PESpy.View;

namespace PESpy.PDB
{
    //CV_Line_t
    public readonly struct CvLine : IValue, IViewable
    {
        /// <summary>
        /// Offset to start of code bytes for line number
        /// </summary>
        public int offset => chunk.PeekInt32(0);

        /// <summary>
        /// line where statement/expression starts
        /// </summary>
        public int linenumStart => flags & 0x00FFFFFF;

        /// <summary>
        /// delta to line where statement ends (optional)
        /// </summary>
        public int deltaLineEnd => (flags >> 24) & 0x7F;

        /// <summary>
        /// true if a statement linenumber, else an expression line num
        /// </summary>
        public bool fStatement => ((flags >> 31) & 0x1) != 0;

        private int flags => chunk.PeekInt32(4);

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
            writer.NewStruct("CV_Line_t", this, ViewKind.CvLine, StructSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(offset), offset);

            using (var bitField = s.WriteBitFields<int>())
            {
                bitField.WriteField(nameof(linenumStart), linenumStart, 24);
                bitField.WriteField(nameof(deltaLineEnd), deltaLineEnd, 7);
                bitField.WriteField(nameof(fStatement), fStatement, 1);
            }

            return s.ToArray();
        }
    }
}
