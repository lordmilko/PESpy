using System.Diagnostics;
using PESpy.View;

namespace PESpy
{
    /// <summary>
    /// Represents a Profile Guided Optimization entry. This type does not have a well-known native struct declaration.
    /// </summary>
    public readonly struct PogoItem : IValue, IViewable
    {
        public int RVA => chunk.PeekInt32(0);

        public int Size => chunk.PeekInt32(4);

        public AnsiString Name => chunk.PeekAnsiNullTerminatedString(8);

        internal const int FixedStructSize =
            sizeof(int) + //RVA
            sizeof(int); //Size

        internal int StructSize =>
            FixedStructSize +
            Name.Length + 1; //Name

        public int Offset => chunk.AbsoluteOffset;

        private readonly MemoryChunk chunk;

        internal PogoItem(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.PogoItem, this, ViewKind.PogoItem, StructSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(RVA), RVA);
            s.WriteField(nameof(Size), Size);
            s.WriteAnsiNullTerminatedField(nameof(Name), Name);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }

        public override string ToString()
        {
            return Name.ToString();
        }
    }
}
