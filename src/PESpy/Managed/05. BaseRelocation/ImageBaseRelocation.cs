using System.Diagnostics;
using PESpy.Native;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy
{
    public readonly struct ImageBaseRelocation : IValue, IViewable
    {
        public int VirtualAddress { get; }

        public int SizeOfBlock { get; }

        public Entry[] Entries { get; }

        public RawOffset Offset { get; }

        internal ImageBaseRelocation(IFileReader reader)
        {
            Offset = (RawOffset) reader.Position;

            VirtualAddress = reader.ReadInt32();
            SizeOfBlock = reader.ReadInt32();

            var numEntries = (SizeOfBlock - 8) / 2;

            var entries = new Entry[numEntries];

            for (var i = 0; i < numEntries; i++)
                entries[i] = new Entry(reader);

            Entries = entries;
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct(nameof(IMAGE_BASE_RELOCATION), this, ViewKind.ImageBaseRelocation);

            s.WriteField(nameof(VirtualAddress), VirtualAddress);
            s.WriteField(nameof(SizeOfBlock), SizeOfBlock);

            //Roslyn compiles foreach loops on arrays down to for loops
            //https://github.com/dotnet/roslyn/blob/e2d4e372f19c16f9b3dea06f7ca857ed5d42bc09/src/Compilers/CSharp/Portable/Lowering/LocalRewriter/LocalRewriter_ForEachStatement.cs
            foreach (var entry in Entries)
            {
                using (var b = s.WriteStructBitField<ushort>("Entry", ViewKind.BaseRelocationEntry))
                {
                    b.WriteField("Type", entry.Type, 4);
                    b.WriteField("Offset", entry.Offset, 12);
                }
            }
        }

        /// <summary>
        /// Represents an entry in an <see cref="IMAGE_BASE_RELOCATION"/> record.<para/>
        /// This type encapsulates the bitfields of a <see cref="ushort"/> value and does not have a well-known native struct declaration.
        /// </summary>
        [DebuggerDisplay("Type = {Type}, Offset = {Offset}, Value = {Value}")]
        public readonly struct Entry
        {
            public ImageRelBased Type => (ImageRelBased) (Value >> 12);

            public short Offset => (short) (Value & 0x0FFF);

            public ushort Value { get; init; }

            internal Entry(IFileReader reader)
            {
                Value = reader.ReadUInt16();
            }
        }
    }
}
