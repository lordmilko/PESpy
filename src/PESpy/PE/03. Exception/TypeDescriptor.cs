using PESpy.View;

namespace PESpy
{
    public readonly struct TypeDescriptor : IValue, IViewable
    {
        public ulong pVFTable => chunk.PeekUInt64(0);

        public ulong Spare => chunk.PeekUInt64(8);

        public AnsiString Name => chunk.PeekAnsiNullTerminatedString(16);

        public int Offset => chunk.AbsoluteOffset;

        internal int StructSize =>
            sizeof(long) + //pVFTable
            sizeof(long) + //Spare
            Name.Length + 1; //Name

        private readonly MemoryChunk chunk;

        internal TypeDescriptor(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.TypeDescriptor, this, ViewKind.TypeDescriptor, StructSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WritePointerField("pVFTable", pVFTable);
            s.WritePointerField("spare", Spare);
            s.WriteAnsiNullTerminatedField("name", Name);

            return s.ToArray();
        }

        public override string ToString()
        {
            return Name.ToString();
        }
    }
}
