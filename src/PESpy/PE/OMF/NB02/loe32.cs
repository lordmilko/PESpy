using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy
{
    //Name is guessed based on the fact the 16-bit version is called loe, and in cvexefmt.h
    //nsg and smd just had "32" added to the end
    public readonly unsafe struct loe32 : IValue, IViewable
    {
        private const int NameOffset = 0;
        private int SegOffset => chunk.PeekByte(0) + 1;
        private int cOffOffset => chunk.PeekByte(0) + 1 + (hasSeg ? sizeof(ushort) : 0);
        private int ItemsOffset => cOffOffset + sizeof(short);

        public SymString Name => chunk.PeekSymString(NameOffset, isLengthPrefixed: true);

        public ushort? Seg => hasSeg ? chunk.PeekUInt16(SegOffset) : null;

        public ushort cOff => chunk.PeekUInt16(cOffOffset);

        public NativeSpan<LineNumberOffset32> Items
        {
            get
            {
                var off = cOffOffset;

                var cOff = chunk.PeekUInt16(off);

                return chunk.PeekNativeSpan<LineNumberOffset32>(off + sizeof(ushort), cOff);
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
                    cOff * LineNumberOffset32.StructSize;
            }
        }

        private readonly MemoryChunk chunk;
        private readonly bool hasSeg;

        internal loe32(in MemoryChunk chunk, bool hasSeg)
        {
            this.chunk = chunk;
            this.hasSeg = hasSeg;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.loe32, this, ViewKind.loe, StructSize);

        int IViewable.NumChildren() => (hasSeg ? 3 : 2) + cOff;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteSymStringField(nameof(Name), NameOffset, Name);
                    break;

                case 1:
                    if (hasSeg)
                        structWriter.WriteField(nameof(Seg), SegOffset, Seg.Value);
                    else
                        structWriter.WriteField(nameof(cOff), cOffOffset, cOff);

                    break;

                case 2:
                    if (hasSeg)
                        structWriter.WriteField(nameof(cOff), cOffOffset, cOff);
                    else
                        structWriter.WriteInline(ItemsOffset, Items[0]);

                    break;

                default:
                    var i = index - (hasSeg ? 3 : 2);

                    structWriter.WriteInline(ItemsOffset + (i * LineNumberOffset32.StructSize), Items[i]);
                    break;
            }
        }

        //Type is made up, fields are based on the 16-bit version

        [DebuggerDisplay("lineNbr = {lineNbr}, offset = {offset}")]
        public struct LineNumberOffset32 : IViewable
        {
            private const int lineNbrOffset = 0;
            private const int offsetOffset = 2;

            public ushort lineNbr;
            public int offset;

            internal const int StructSize =
                sizeof(ushort) + //lineNbr
                sizeof(int); //offset

            void IViewable.WriteGlobals(ViewWriter writer)
            {
                //No globals
            }

            IView? IViewable.WriteStruct(ViewWriter writer) =>
                writer.NewUnmanagedStruct(Strings.LineNumberOffset, this, ViewKind.LineNumberOffset32, StructSize);

            int IViewable.NumChildren() => 2;

            void IViewable.WriteChild(int index, ref StructWriter structWriter)
            {
                switch (index)
                {
                    case 0:
                        structWriter.WriteField(nameof(lineNbr), lineNbrOffset, lineNbr);
                        break;

                    case 1:
                        structWriter.WriteField(nameof(offset), offsetOffset, offset);
                        break;

                    default:
                        throw new IndexOutOfRangeException();
                }
            }
        }

        public override string ToString()
        {
            return Name.ToString();
        }
    }
}
