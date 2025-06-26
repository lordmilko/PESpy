using PESpy.View;

namespace PESpy
{
    public readonly struct TypeDescriptor : IValue, IViewable
    {
#if PEFAST
        public ulong pVFTable => chunk.PeekUInt64(0);
#else
        public ulong pVFTable { get; }
#endif

#if PEFAST
        public ulong Spare => chunk.PeekUInt64(8);
#else
        public ulong Spare { get; }
#endif

#if PEFAST
        public AnsiString Name => chunk.PeekAnsiNullTerminatedString(16);
#else
        public string Name { get; }
#endif

#if PEFAST
        public int Offset => chunk.AbsoluteOffset;
#else
        public int Offset { get; }
#endif

        internal int StructSize =>
            sizeof(long) + //pVFTable
            sizeof(long) + //Spare
            Name.Length + 1; //Name

#if PEFAST
        private readonly MemoryChunk chunk;

        internal TypeDescriptor(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }
#else
        internal TypeDescriptor(IFileReader reader, PEFile peFile)
        {
            Offset = (int) reader.Position;

            pVFTable = peFile.OptionalHeader.Magic == PEMagic.PE32 ? reader.ReadUInt32() : reader.ReadUInt64();
            Spare = peFile.OptionalHeader.Magic == PEMagic.PE32 ? reader.ReadUInt32() : reader.ReadUInt64();
            Name = reader.ReadAnsiNullTerminatedString();
        }
#endif

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(nameof(PESpy.Native.TypeDescriptor), this, ViewKind.TypeDescriptor, StructSize);

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
