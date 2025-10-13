using System.Diagnostics;
using PESpy.View;

namespace PESpy
{
    public readonly unsafe struct loe : IValue, IViewable
    {
        public FixedAnsiString Name
        {
            get
            {
                var length = chunk.PeekByte(0);
                return chunk.PeekAnsiFixedLength(1, length);
            }
        }

        public ushort? Seg => hasSeg ? chunk.PeekUInt16(chunk.PeekByte(0) + 1) : null;

        public ushort cOff => chunk.PeekUInt16(chunk.PeekByte(0) + 1 + (hasSeg ? sizeof(ushort) : 0));

        public NativeSpan<LineNumberOffset> Items
        {
            get
            {
                var off = chunk.PeekByte(0) + 1 + (hasSeg ? sizeof(ushort) : 0);

                var cOff = chunk.PeekUInt16(off);

                return chunk.PeekNativeSpan<LineNumberOffset>(off + sizeof(ushort), cOff);
            }
        }

        public int Offset => chunk.AbsoluteOffset;

        public int StructSize
        {
            get
            {
                var lengthToCount =
                    chunk.PeekByte(0) + //Name
                    sizeof(byte) + //Length of name
                    (hasSeg ? sizeof(ushort) : 0); //Seg

                var cOff = chunk.PeekUInt16(lengthToCount);

                return lengthToCount +
                    sizeof(ushort) + //cOff
                    cOff * (sizeof(ushort) + sizeof(ushort));
            }
        }

        private readonly MemoryChunk chunk;
        private readonly bool hasSeg;

        internal loe(in MemoryChunk chunk, bool hasSeg)
        {
            this.chunk = chunk;
            this.hasSeg = hasSeg;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.loe, this, ViewKind.loe, StructSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteLengthPrefixedAnsiField(nameof(Name), Name);

            if (hasSeg)
                s.WriteField(nameof(Seg), Seg.Value);

            s.WriteField(nameof(cOff), cOff);

            s.WriteInline(Items);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }

        //Type is made up, fields are not

        [DebuggerDisplay("lineNbr = {lineNbr}, offset = {offset}")]
        public struct LineNumberOffset : IViewable
        {
            public ushort lineNbr;
            public ushort offset;

            internal const int StructSize =
                sizeof(ushort) + //lineNbr
                sizeof(ushort); //offset

            void IViewable.WriteGlobals(ViewWriter writer)
            {
                //No globals
            }

            IView? IViewable.WriteStruct(ViewWriter writer) =>
                writer.NewUnmanagedStruct(Strings.LineNumberOffset, this, ViewKind.LineNumberOffset, StructSize);

            IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
            {
                using var s = viewWriter.CreateStruct(parent);

                s.WriteField(nameof(lineNbr), lineNbr);
                s.WriteField(nameof(offset), offset);

                Debug.Assert(parent.Size == s.Size, "Size was not correct");
                return s.ToArray();
            }
        }

        public override string ToString()
        {
            return Name.ToString();
        }
    }
}
