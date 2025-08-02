using System.Diagnostics;
using PESpy.Native;
using PESpy.View;

namespace PESpy
{
    public readonly struct ImageBaseRelocation : IValue, IViewable
    {
        public int VirtualAddress => chunk.PeekInt32(0);

        public int SizeOfBlock => chunk.PeekInt32(4);

        public NativeSpan<Entry> Entries => chunk.PeekNativeSpan<Entry>(8, (SizeOfBlock - 8) / 2);

        public int Offset => chunk.AbsoluteOffset;

        private readonly MemoryChunk chunk;

        internal ImageBaseRelocation(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.IMAGE_BASE_RELOCATION, this, ViewKind.ImageBaseRelocation, SizeOfBlock);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(VirtualAddress), VirtualAddress);
            s.WriteField(nameof(SizeOfBlock), SizeOfBlock);

            //Roslyn compiles foreach loops on arrays down to for loops
            //https://github.com/dotnet/roslyn/blob/e2d4e372f19c16f9b3dea06f7ca857ed5d42bc09/src/Compilers/CSharp/Portable/Lowering/LocalRewriter/LocalRewriter_ForEachStatement.cs
            foreach (var entry in Entries)
            {
                using (var b = s.WriteStructBitField<ushort>(Strings.Entry, ViewKind.BaseRelocationEntry))
                {
                    b.WriteField("Type", entry.Type, 4);
                    b.WriteField("Offset", entry.Offset, 12);
                }
            }

            return s.ToArray();
        }

        /// <summary>
        /// Represents an entry in an <see cref="IMAGE_BASE_RELOCATION"/> record.<para/>
        /// This type encapsulates the bitfields of a <see cref="ushort"/> value and does not have a well-known native struct declaration.
        /// </summary>
        [DebuggerDisplay("Type = {Type}, Offset = {Offset}")]
        public readonly struct Entry
        {
            public ImageRelBased Type => (ImageRelBased) (Value >> 12);

            public short Offset => (short) (Value & 0x0FFF);

            public ushort Value { get; init; }

            //No need to pass a MemoryChunk; we read these directly through a span. The only physical
            //value is the Value field (2 bytes)
        }
    }
}
