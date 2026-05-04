using System;
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
        private const int StartAddressOfRawDataOffset = 0;
        private int EndAddressOfRawDataOffset => chunk.PointerSize;
        private int AddressOfIndexOffset => 2 * chunk.PointerSize;
        private int AddressOfCallBacksOffset => 3 * chunk.PointerSize;
        private int SizeOfZeroFillOffset => 4 * chunk.PointerSize;
        private int CharacteristicsOffset => 4 + (4 * chunk.PointerSize);

        public ulong StartAddressOfRawData => chunk.PeekPointer(StartAddressOfRawDataOffset);

        public ulong EndAddressOfRawData => chunk.PeekPointer(EndAddressOfRawDataOffset);

        public ulong AddressOfIndex => chunk.PeekPointer(AddressOfIndexOffset);

        public ulong AddressOfCallBacks => chunk.PeekPointer(AddressOfCallBacksOffset);

        public int SizeOfZeroFill => chunk.PeekInt32(SizeOfZeroFillOffset);

        public IMAGE_SCN_ALIGN Characteristics => (IMAGE_SCN_ALIGN) chunk.PeekUInt32(CharacteristicsOffset);

        public long Offset => chunk.AbsoluteOffset;

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
            writer.NewStruct(this, ViewKind.ImageTlsDirectory, StructSize(writer.Is32Bit));

        int IViewable.NumChildren() => 6;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WritePointerField(nameof(StartAddressOfRawData), StartAddressOfRawDataOffset, StartAddressOfRawData);
                    break;

                case 1:
                    structWriter.WritePointerField(nameof(EndAddressOfRawData), EndAddressOfRawDataOffset, EndAddressOfRawData);
                    break;

                case 2:
                    structWriter.WritePointerField(nameof(AddressOfIndex), AddressOfIndexOffset, AddressOfIndex);
                    break;

                case 3:
                    structWriter.WritePointerField(nameof(AddressOfCallBacks), AddressOfCallBacksOffset, AddressOfCallBacks);
                    break;

                case 4:
                    structWriter.WriteField(nameof(SizeOfZeroFill), SizeOfZeroFillOffset, SizeOfZeroFill);
                    break;

                case 5:
                    structWriter.WriteField(nameof(Characteristics), CharacteristicsOffset, Characteristics, sizeof(int));
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
