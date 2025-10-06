using System.Diagnostics;
using PESpy.View;

namespace PESpy
{
    //SSTPUBLICS
    public readonly struct pbi : IValue, IViewable
    {
        public ushort off => chunk.PeekUInt16(0);

        public ushort seg => chunk.PeekUInt16(2);

        public ushort type => chunk.PeekUInt16(4);

        public FixedAnsiString name
        {
            get
            {
                var length = chunk.PeekByte(6);
                return chunk.PeekAnsiFixedLength(7, length);
            }
        }

        public int Offset => chunk.AbsoluteOffset;

        internal const int FixedStructSize =
            sizeof(short) + //off
            sizeof(short) + //seg
            sizeof(short); //type

        internal int StructSize => FixedStructSize + name.Length + 1;

        private readonly MemoryChunk chunk;

        internal pbi(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.pbi, this, ViewKind.pbi, StructSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(off), off);
            s.WriteField(nameof(seg), seg);
            s.WriteField(nameof(type), type);
            s.WriteAnsiFixedLengthField(nameof(name), name);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}
