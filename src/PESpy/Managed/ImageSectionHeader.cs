using ClrDebug;
using PESpy.View;
#if !DEBUG_POSITION
using RVA = System.Int32;
using RawOffset = System.Int32;
#endif

namespace PESpy
{
    /// <summary>
    /// Represents the <see cref="IMAGE_SECTION_HEADER"/> structure.
    /// </summary>
    public readonly struct ImageSectionHeader : IValue, IViewable //Stored in an array, so can be a struct
    {
        /// <summary>
        /// The name of the section.
        /// </summary>
        public string Name { get; init; }

        /// <summary>
        /// The total size of the section when loaded into memory.
        /// If this value is greater than <see cref="SizeOfRawData"/>, the section is zero-padded.
        /// This field is valid only for PE images and should be set to zero for object files.
        /// </summary>
        public int VirtualSize { get; init; }

        /// <summary>
        /// For PE images, the address of the first byte of the section relative to the image base when the
        /// section is loaded into memory. For object files, this field is the address of the first byte before
        /// relocation is applied; for simplicity, compilers should set this to zero. Otherwise,
        /// it is an arbitrary value that is subtracted from offsets during relocation.
        /// </summary>
        public RVA VirtualAddress { get; init; }

        /// <summary>
        /// The size of the section (for object files) or the size of the initialized data on disk (for image files).
        /// For PE images, this must be a multiple of <see cref="ImageOptionalHeader.FileAlignment"/>.
        /// If this is less than <see cref="VirtualSize"/>, the remainder of the section is zero-filled.
        /// Because the <see cref="SizeOfRawData"/> field is rounded but the <see cref="VirtualSize"/> field is not,
        /// it is possible for <see cref="SizeOfRawData"/> to be greater than <see cref="VirtualSize"/> as well.
        ///  When a section contains only uninitialized data, this field should be zero.
        /// </summary>
        public int SizeOfRawData { get; init; }

        /// <summary>
        /// The file pointer to the first page of the section within the COFF file.
        /// For PE images, this must be a multiple of <see cref="ImageOptionalHeader.FileAlignment"/>.
        /// For object files, the value should be aligned on a 4 byte boundary for best performance.
        /// When a section contains only uninitialized data, this field should be zero.
        /// </summary>
        public RawOffset PointerToRawData { get; init; }

        /// <summary>
        /// The file pointer to the beginning of relocation entries for the section.
        /// This is set to zero for PE images or if there are no relocations.
        /// </summary>
        public int PointerToRelocations { get; init; }

        /// <summary>
        /// The file pointer to the beginning of line-number entries for the section.
        /// This is set to zero if there are no COFF line numbers.
        /// This value should be zero for an image because COFF debugging information is deprecated.
        /// </summary>
        public int PointerToLineNumbers { get; init; }

        /// <summary>
        /// The number of relocation entries for the section. This is set to zero for PE images.
        /// </summary>
        public short NumberOfRelocations { get; init; }

        /// <summary>
        /// The number of line-number entries for the section.
        ///  This value should be zero for an image because COFF debugging information is deprecated.
        /// </summary>
        public short NumberOfLineNumbers { get; init; }

        /// <summary>
        /// The flags that describe the characteristics of the section.
        /// </summary>
        public IMAGE_SCN Characteristics { get; init; }

        public RawOffset Offset { get; }

        internal const int NameSize = 8;

        internal const int StructSize =
            NameSize +
            sizeof(int) +   // VirtualSize
            sizeof(int) +   // VirtualAddress
            sizeof(int) +   // SizeOfRawData
            sizeof(int) +   // PointerToRawData
            sizeof(int) +   // PointerToRelocations
            sizeof(int) +   // PointerToLineNumbers
            sizeof(short) + // NumberOfRelocations
            sizeof(short) + // NumberOfLineNumbers
            sizeof(int);    // SectionCharacteristics

        internal ImageSectionHeader(IFileReader reader)
        {
            Offset = (RawOffset) reader.Position;

            reader.FillBuffer(StructSize);

            Name = reader.ReadNullPaddedUTF8(NameSize);
            VirtualSize = reader.ReadInt32();
            VirtualAddress = (RVA) reader.ReadInt32();
            SizeOfRawData = reader.ReadInt32();
            PointerToRawData = (RawOffset) reader.ReadInt32();
            PointerToRelocations = reader.ReadInt32();
            PointerToLineNumbers = reader.ReadInt32();
            NumberOfRelocations = reader.ReadInt16();
            NumberOfLineNumbers = reader.ReadInt16();
            Characteristics = (IMAGE_SCN) reader.ReadUInt32();
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct(nameof(IMAGE_SECTION_HEADER), this, ViewKind.VCFeature);

            //Name is exactly 8 bytes. If the name is only 4 bytes, the remaining 4 bytes are \0
            s.WriteNullPaddedUTF8Field(nameof(Name), Name, NameSize);
            s.WriteField(nameof(VirtualSize), VirtualSize);
            s.WriteField(nameof(VirtualAddress), (int) VirtualAddress);
            s.WriteField(nameof(SizeOfRawData), SizeOfRawData);
            s.WriteField(nameof(PointerToRawData), (int) PointerToRawData);
            s.WriteField(nameof(PointerToRelocations), PointerToRelocations);
            s.WriteField(nameof(PointerToLineNumbers), PointerToLineNumbers);
            s.WriteField(nameof(NumberOfRelocations), NumberOfRelocations);
            s.WriteField(nameof(NumberOfLineNumbers), NumberOfLineNumbers);
            s.WriteField(nameof(Characteristics), Characteristics, sizeof(int));
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
