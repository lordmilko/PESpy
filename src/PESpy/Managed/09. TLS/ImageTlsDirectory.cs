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
        public long StartAddressOfRawData { get; }

        public long EndAddressOfRawData { get; }

        public long AddressOfIndex { get; }

        public long AddressOfCallBacks { get; }

        public int SizeOfZeroFill { get; }

        public IMAGE_SCN_ALIGN Characteristics { get; }

        public RawOffset Offset { get; }

        public ImageTlsDirectory(ref FileReader reader, bool is32Bit)
        {
            Offset = (RawOffset) reader.Position;

            StartAddressOfRawData = ReadPointer(ref reader, is32Bit);
            EndAddressOfRawData = ReadPointer(ref reader, is32Bit);
            AddressOfIndex = ReadPointer(ref reader, is32Bit);
            AddressOfCallBacks = ReadPointer(ref reader, is32Bit);
            SizeOfZeroFill = reader.ReadInt32();
            Characteristics = (IMAGE_SCN_ALIGN) reader.ReadInt32();

            //I'm not sure if IMAGE_SCN_SCALE_INDEX is supposed to be included in the enum list
            Debug.Assert(((int) Characteristics & 1) == 0, "Should IMAGE_SCN_SCALE_INDEX be listed as an enum value?");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static long ReadPointer(ref FileReader reader, bool is32Bit)
        {
            if (is32Bit)
                return reader.ReadInt32();

            return reader.ReadInt64();
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct("IMAGE_TLS_DIRECTORY", this, ViewKind.ImageTlsDirectory);

            s.WritePointerField(nameof(StartAddressOfRawData), StartAddressOfRawData);
            s.WritePointerField(nameof(EndAddressOfRawData), EndAddressOfRawData);
            s.WritePointerField(nameof(AddressOfIndex), AddressOfIndex);
            s.WritePointerField(nameof(AddressOfCallBacks), AddressOfCallBacks);
            s.WriteField(nameof(SizeOfZeroFill), SizeOfZeroFill);
            s.WriteField(nameof(Characteristics), Characteristics, sizeof(int));
        }
    }
}
