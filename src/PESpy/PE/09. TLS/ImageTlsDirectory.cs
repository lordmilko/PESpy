using System.Diagnostics;
using System.Runtime.CompilerServices;
using PESpy.Native;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy
{
    /// <summary>
    /// Represents the <see cref="IMAGE_TLS_DIRECTORY32"/> / <see cref="IMAGE_TLS_DIRECTORY64"/> structure.
    /// </summary>
    public class ImageTlsDirectory : IValue, IViewable
    {
#if PEFAST
        public ulong StartAddressOfRawData => chunk.PeekPointer(0);
#else
        public ulong StartAddressOfRawData { get; }
#endif

#if PEFAST
        public ulong EndAddressOfRawData => chunk.PeekPointer(chunk.PointerSize);
#else
        public ulong EndAddressOfRawData { get; }
#endif

#if PEFAST
        public ulong AddressOfIndex => chunk.PeekPointer(2 * chunk.PointerSize);
#else
        public ulong AddressOfIndex { get; }
#endif

#if PEFAST
        public ulong AddressOfCallBacks => chunk.PeekPointer(3 * chunk.PointerSize);
#else
        public ulong AddressOfCallBacks { get; }
#endif

#if PEFAST
        public int SizeOfZeroFill => chunk.PeekInt32(4 * chunk.PointerSize);
#else
        public int SizeOfZeroFill { get; }
#endif

#if PEFAST
        public IMAGE_SCN_ALIGN Characteristics => (IMAGE_SCN_ALIGN) chunk.PeekUInt32(4 + (4 * chunk.PointerSize));
#else
        public IMAGE_SCN_ALIGN Characteristics { get; }
#endif

#if PEFAST
        public RawOffset Offset => chunk.AbsoluteOffset;
#else
        public RawOffset Offset { get; }
#endif

        internal static int StructSize(bool is32Bit) =>
            is32Bit
                ? (4 * 4)
                : (4 * 8) + //StartAddressOfRawData, EndAddressOfRawData, AddressOfIndex, AddressOfCallBacks
            sizeof(int) + //SizeOfZeroFill
            sizeof(int); //Characteristics


#if PEFAST
        private readonly MemoryChunk chunk;

        internal ImageTlsDirectory(in MemoryChunk chunk)
        {
            this.chunk = chunk;

            //I'm not sure if IMAGE_SCN_SCALE_INDEX is supposed to be included in the enum list
            Debug.Assert(((int) Characteristics & 1) == 0, "Should IMAGE_SCN_SCALE_INDEX be listed as an enum value?");
        }
#else
        internal ImageTlsDirectory(IFileReader reader, bool is32Bit)
        {
            Offset = (RawOffset) reader.Position;

            StartAddressOfRawData = ReadPointer(reader, is32Bit);
            EndAddressOfRawData = ReadPointer(reader, is32Bit);
            AddressOfIndex = ReadPointer(reader, is32Bit);
            AddressOfCallBacks = ReadPointer(reader, is32Bit);
            SizeOfZeroFill = reader.ReadInt32();
            Characteristics = (IMAGE_SCN_ALIGN) reader.ReadInt32();

            //I'm not sure if IMAGE_SCN_SCALE_INDEX is supposed to be included in the enum list
            Debug.Assert(((int) Characteristics & 1) == 0, "Should IMAGE_SCN_SCALE_INDEX be listed as an enum value?");
        }
#endif

#if !PEFAST
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong ReadPointer(IFileReader reader, bool is32Bit)
        {
            if (is32Bit)
                return reader.ReadUInt32();

            return reader.ReadUInt64();
        }
#endif

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct("IMAGE_TLS_DIRECTORY", this, ViewKind.ImageTlsDirectory, StructSize(((PEViewWriter) writer).Is32Bit));

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WritePointerField(nameof(StartAddressOfRawData), StartAddressOfRawData);
            s.WritePointerField(nameof(EndAddressOfRawData), EndAddressOfRawData);
            s.WritePointerField(nameof(AddressOfIndex), AddressOfIndex);
            s.WritePointerField(nameof(AddressOfCallBacks), AddressOfCallBacks);
            s.WriteField(nameof(SizeOfZeroFill), SizeOfZeroFill);
            s.WriteField(nameof(Characteristics), Characteristics, sizeof(int));

            return s.ToArray();
        }
    }
}
