using System.Diagnostics;
using System.Runtime.CompilerServices;
using PESpy.Native;
using PESpy.View;

namespace PESpy
{
    /// <summary>
    /// Represents the <see cref="IMAGE_TLS_DIRECTORY32"/> / <see cref="IMAGE_TLS_DIRECTORY64"/> structure.
    /// </summary>
    public class ImageTlsDirectory : IValue, IViewable
    {
        public ulong StartAddressOfRawData => chunk.PeekPointer(0);

        public ulong EndAddressOfRawData => chunk.PeekPointer(chunk.PointerSize);

        public ulong AddressOfIndex => chunk.PeekPointer(2 * chunk.PointerSize);

        public ulong AddressOfCallBacks => chunk.PeekPointer(3 * chunk.PointerSize);

        public int SizeOfZeroFill => chunk.PeekInt32(4 * chunk.PointerSize);

        public IMAGE_SCN_ALIGN Characteristics => (IMAGE_SCN_ALIGN) chunk.PeekUInt32(4 + (4 * chunk.PointerSize));

        public int Offset => chunk.AbsoluteOffset;

        internal static int StructSize(bool is32Bit) =>
            (is32Bit
                ? (4 * 4)
                : (4 * 8)) + //StartAddressOfRawData, EndAddressOfRawData, AddressOfIndex, AddressOfCallBacks
            sizeof(int) + //SizeOfZeroFill
            sizeof(int); //Characteristics


        private readonly MemoryChunk chunk;

        internal ImageTlsDirectory(in MemoryChunk chunk)
        {
            this.chunk = chunk;

            //I'm not sure if IMAGE_SCN_SCALE_INDEX is supposed to be included in the enum list
            Debug.Assert(((int) Characteristics & 1) == 0, "Should IMAGE_SCN_SCALE_INDEX be listed as an enum value?");
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.IMAGE_TLS_DIRECTORY, this, ViewKind.ImageTlsDirectory, StructSize(((PEViewWriter) writer).Is32Bit));

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WritePointerField(nameof(StartAddressOfRawData), StartAddressOfRawData);
            s.WritePointerField(nameof(EndAddressOfRawData), EndAddressOfRawData);
            s.WritePointerField(nameof(AddressOfIndex), AddressOfIndex);
            s.WritePointerField(nameof(AddressOfCallBacks), AddressOfCallBacks);
            s.WriteField(nameof(SizeOfZeroFill), SizeOfZeroFill);
            s.WriteField(nameof(Characteristics), Characteristics, sizeof(int));

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }
    }
}
