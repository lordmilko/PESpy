using System.Diagnostics;
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
    public struct ImageSectionHeader : IValue, IViewable //Stored in an array, so can be a struct
    {
        /// <summary>
        /// The name of the section.
        /// </summary>
#if PEFAST
        public Utf8String Name => chunk.PeekNullPaddedUTF8(0, 8);
#else
        public string Name { get; init; }
#endif

        /// <summary>
        /// The total size of the section when loaded into memory.
        /// If this value is greater than <see cref="SizeOfRawData"/>, the section is zero-padded.
        /// This field is valid only for PE images and should be set to zero for object files.
        /// </summary>
#if PEFAST
        public int VirtualSize => chunk.PeekInt32(8);
#else
        public int VirtualSize { get; init; }
#endif

        /// <summary>
        /// For PE images, the address of the first byte of the section relative to the image base when the
        /// section is loaded into memory. For object files, this field is the address of the first byte before
        /// relocation is applied; for simplicity, compilers should set this to zero. Otherwise,
        /// it is an arbitrary value that is subtracted from offsets during relocation.
        /// </summary>
#if PEFAST
        public int VirtualAddress => chunk.PeekInt32(12);
#else
        public RVA VirtualAddress { get; init; }
#endif

        /// <summary>
        /// The size of the section (for object files) or the size of the initialized data on disk (for image files).
        /// For PE images, this must be a multiple of <see cref="ImageOptionalHeader.FileAlignment"/>.
        /// If this is less than <see cref="VirtualSize"/>, the remainder of the section is zero-filled.
        /// Because the <see cref="SizeOfRawData"/> field is rounded but the <see cref="VirtualSize"/> field is not,
        /// it is possible for <see cref="SizeOfRawData"/> to be greater than <see cref="VirtualSize"/> as well.
        ///  When a section contains only uninitialized data, this field should be zero.
        /// </summary>
#if PEFAST
        public int SizeOfRawData => chunk.PeekInt32(16);
#else
        public int SizeOfRawData { get; init; }
#endif

        /// <summary>
        /// The file pointer to the first page of the section within the COFF file.
        /// For PE images, this must be a multiple of <see cref="ImageOptionalHeader.FileAlignment"/>.
        /// For object files, the value should be aligned on a 4 byte boundary for best performance.
        /// When a section contains only uninitialized data, this field should be zero.
        /// </summary>
#if PEFAST
        public int PointerToRawData => chunk.PeekInt32(20);
#else
        public RawOffset PointerToRawData { get; init; }
#endif

        /// <summary>
        /// The file pointer to the beginning of relocation entries for the section.
        /// This is set to zero for PE images or if there are no relocations.
        /// </summary>
#if PEFAST
        private VA<ImageRelocation[]>? pointerToRelocations;

        public VA<ImageRelocation[]> PointerToRelocations
        {
            get
            {
                if (pointerToRelocations == null)
                {
                    var offset = chunk.PeekInt32(24);

                    if (offset == 0)
                    {
                        pointerToRelocations = new VA<ImageRelocation[]>(offset);
                    }
                    else
                    {
                        MemoryChunk symbolTableChunk;

                        if (chunk.block is GlobalMemoryBlock b)
                        {
                            symbolTableChunk = new MemoryChunk(b, offset);
                        }
                        else
                        {
                            if (!chunk.PEFile().TryGetValueChunkFromSectionOrHeader(offset, out symbolTableChunk))
                            {
                                pointerToRelocations = new VA<ImageRelocation[]>(offset);
                                return pointerToRelocations.Value;
                            }
                        }

                        var relocations = new ImageRelocation[NumberOfRelocations];

                        for (var i = 0; i < NumberOfRelocations; i++)
                            relocations[i] = new ImageRelocation(symbolTableChunk.Slice(i * ImageRelocation.StructSize));

                        pointerToRelocations = new VA<ImageRelocation[]>(offset, offset, relocations);
                    }
                }

                return pointerToRelocations.Value;
            }
        }
#else
        public VA<ImageRelocation[]> PointerToRelocations { get; init; }
#endif

        /// <summary>
        /// The file pointer to the beginning of line-number entries for the section.
        /// This is set to zero if there are no COFF line numbers.
        /// This value should be zero for an image because COFF debugging information is deprecated.
        /// </summary>
#if PEFAST
        public int PointerToLineNumbers => chunk.PeekInt32(28);
#else
        public int PointerToLineNumbers { get; init; }
#endif

        /// <summary>
        /// The number of relocation entries for the section. This is set to zero for PE images.
        /// </summary>
#if PEFAST
        public short NumberOfRelocations => chunk.PeekInt16(32);
#else
        public short NumberOfRelocations { get; init; }
#endif

        /// <summary>
        /// The number of line-number entries for the section.
        ///  This value should be zero for an image because COFF debugging information is deprecated.
        /// </summary>
#if PEFAST
        public short NumberOfLineNumbers => chunk.PeekInt16(34);
#else
        public short NumberOfLineNumbers { get; init; }
#endif

        /// <summary>
        /// The flags that describe the characteristics of the section.
        /// </summary>
#if PEFAST
        public IMAGE_SCN Characteristics => (IMAGE_SCN) chunk.PeekUInt32(36);
#else
        public IMAGE_SCN Characteristics { get; init; }
#endif

#if PEFAST
        public RawOffset Offset => chunk.AbsoluteOffset;
#else
        public RawOffset Offset { get; }
#endif

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

#if PEFAST
        private readonly MemoryChunk chunk;

        internal ImageSectionHeader(in MemoryChunk chunk)
        {
            pointerToRelocations = default;
            this.chunk = chunk;
        }
#else
        internal ImageSectionHeader(IFileReader reader)
        {
            Offset = (RawOffset) reader.Position;

            reader.FillBuffer(StructSize);

            Name = reader.ReadNullPaddedUTF8(NameSize);
            VirtualSize = reader.ReadInt32();
            VirtualAddress = (RVA) reader.ReadInt32();
            SizeOfRawData = reader.ReadInt32();
            PointerToRawData = (RawOffset) reader.ReadInt32();
            var pointerToRelocations = reader.ReadInt32();
            PointerToLineNumbers = reader.ReadInt32();
            NumberOfRelocations = reader.ReadInt16();
            NumberOfLineNumbers = reader.ReadInt16();
            Characteristics = (IMAGE_SCN) reader.ReadUInt32();

            //@comp.id.Value apparently has the compiler type in the top 16 bits and the compiler id version in the bottom?
            if (pointerToRelocations == 0)
                PointerToRelocations = new VA<ImageRelocation[]>(pointerToRelocations);
            else
            {
                var oldOffset = reader.Position;

                reader.Seek(pointerToRelocations);

                var relocations = new ImageRelocation[NumberOfRelocations];

                for (var i = 0; i < NumberOfRelocations; i++)
                    relocations[i] = new ImageRelocation(reader);

                PointerToRelocations = new VA<ImageRelocation[]>(pointerToRelocations, pointerToRelocations, relocations);

                reader.Seek(oldOffset);
            }

            if (PointerToLineNumbers > 0)
                Debug.Assert(false, "Reading line numbers is not implemented");
        }
#endif

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct(nameof(IMAGE_SECTION_HEADER), this, ViewKind.ImageSectionHeader);

            //Name is exactly 8 bytes. If the name is only 4 bytes, the remaining 4 bytes are \0
            s.WriteNullPaddedUTF8Field(nameof(Name), Name, NameSize);
            s.WriteField(nameof(VirtualSize), VirtualSize);
            s.WriteField(nameof(VirtualAddress), (int) VirtualAddress);
            s.WriteField(nameof(SizeOfRawData), SizeOfRawData);
            s.WriteField(nameof(PointerToRawData), (int) PointerToRawData);
            s.WriteSmallVAPointerField(nameof(PointerToRelocations), PointerToRelocations);
            s.WriteField(nameof(PointerToLineNumbers), PointerToLineNumbers);
            s.WriteField(nameof(NumberOfRelocations), NumberOfRelocations);
            s.WriteField(nameof(NumberOfLineNumbers), NumberOfLineNumbers);
            s.WriteField(nameof(Characteristics), Characteristics, sizeof(int));
        }

        public override string ToString()
        {
            return Name.ToString();
        }
    }
}
