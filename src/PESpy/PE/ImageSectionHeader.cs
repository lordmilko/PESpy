using System;
using System.Diagnostics;
using System.IO;
using ClrDebug;
using PESpy.LIB;
using PESpy.View;

namespace PESpy
{
    /// <summary>
    /// Represents the <see cref="IMAGE_SECTION_HEADER"/> structure.
    /// </summary>
    public struct ImageSectionHeader : IValue, IViewable //Stored in an array, so can be a struct
    {
        private const int NameOffset = 0;
        private const int VirtualSizeOffset = 8;
        private const int VirtualAddressOffset = 12;
        private const int SizeOfRawDataOffset = 16;
        private const int PointerToRawDataOffset = 20;
        private const int PointerToRelocationsOffset = 24;
        private const int PointerToLineNumbersOffset = 28;
        private const int NumberOfRelocationsOffset = 32;
        private const int NumberOfLineNumbersOffset = 34;
        private const int CharacteristicsOffset = 36;

        /// <summary>
        /// The name of the section.
        /// </summary>
        public FixedUtf8String Name => chunk.PeekNullPaddedUtf8(NameOffset, 8);

        /// <summary>
        /// The total size of the section when loaded into memory.
        /// If this value is greater than <see cref="SizeOfRawData"/>, the section is zero-padded.
        /// This field is valid only for PE images and should be set to zero for object files.
        /// </summary>
        public int VirtualSize => chunk.PeekInt32(VirtualSizeOffset);

        /// <summary>
        /// For PE images, the address of the first byte of the section relative to the image base when the
        /// section is loaded into memory. For object files, this field is the address of the first byte before
        /// relocation is applied; for simplicity, compilers should set this to zero. Otherwise,
        /// it is an arbitrary value that is subtracted from offsets during relocation.
        /// </summary>
        public int VirtualAddress => chunk.PeekInt32(VirtualAddressOffset);

        /// <summary>
        /// The size of the section (for object files) or the size of the initialized data on disk (for image files).
        /// For PE images, this must be a multiple of <see cref="ImageOptionalHeader.FileAlignment"/>.
        /// If this is less than <see cref="VirtualSize"/>, the remainder of the section is zero-filled.
        /// Because the <see cref="SizeOfRawData"/> field is rounded but the <see cref="VirtualSize"/> field is not,
        /// it is possible for <see cref="SizeOfRawData"/> to be greater than <see cref="VirtualSize"/> as well.
        ///  When a section contains only uninitialized data, this field should be zero.
        /// </summary>
        public int SizeOfRawData => chunk.PeekInt32(SizeOfRawDataOffset);

        /// <summary>
        /// The file pointer to the first page of the section within the COFF file.
        /// For PE images, this must be a multiple of <see cref="ImageOptionalHeader.FileAlignment"/>.
        /// For object files, the value should be aligned on a 4 byte boundary for best performance.
        /// When a section contains only uninitialized data, this field should be zero.
        /// </summary>
        public int PointerToRawData => chunk.PeekInt32(PointerToRawDataOffset);

        /// <summary>
        /// The file pointer to the beginning of relocation entries for the section.
        /// This is set to zero for PE images or if there are no relocations.
        /// </summary>
        private VA<ImageRelocation[]> pointerToRelocations;

        public VA<ImageRelocation[]> PointerToRelocations
        {
            get
            {
                if (pointerToRelocations.ListedAddress == 0)
                {
                    var offset = chunk.PeekInt32(PointerToRelocationsOffset);

                    if (offset == 0)
                    {
                        pointerToRelocations = new VA<ImageRelocation[]>(offset);
                    }
                    else
                    {
                        MemoryChunk valueChunk;

                        if (TryGetHeaderChunk(chunk, offset, out valueChunk))
                        {
                            var relocations = new ImageRelocation[NumberOfRelocations];

                            for (var i = 0; i < NumberOfRelocations; i++)
                                relocations[i] = new ImageRelocation(valueChunk.Slice(i * ImageRelocation.StructSize));

                            pointerToRelocations = new VA<ImageRelocation[]>(offset, offset, relocations);
                        }
                        else
                        {
                            pointerToRelocations = new VA<ImageRelocation[]>(offset);
                        }
                    }
                }

                return pointerToRelocations;
            }
        }

        /// <summary>
        /// The file pointer to the beginning of line-number entries for the section.
        /// This is set to zero if there are no COFF line numbers.
        /// This value should be zero for an image because COFF debugging information is deprecated.
        /// </summary>
        private VA<ImageLineNumber[]> pointerToLineNumbers;

        public VA<ImageLineNumber[]> PointerToLineNumbers
        {
            get
            {
                if (pointerToLineNumbers.ListedAddress == 0)
                {
                    var offset = chunk.PeekInt32(PointerToLineNumbersOffset);

                    if (offset == 0)
                    {
                        pointerToLineNumbers = new VA<ImageLineNumber[]>(offset);
                    }
                    else
                    {
                        //This can point beyond the end of the file, or the section can claim to have more line numbers than would fit in the file
                        if (TryGetHeaderChunk(chunk, offset, out var valueChunk) && valueChunk.Remaining >= (NumberOfLineNumbers * ImageLineNumber.StructSize))
                        {
                            var lineNumbers = new ImageLineNumber[NumberOfLineNumbers];

                            for (var i = 0; i < NumberOfLineNumbers; i++)
                                lineNumbers[i] = new ImageLineNumber(valueChunk.Slice(i * ImageLineNumber.StructSize));

                            pointerToLineNumbers = new VA<ImageLineNumber[]>(offset, offset, lineNumbers);
                        }
                        else
                        {
                            pointerToLineNumbers = new VA<ImageLineNumber[]>(offset);
                        }
                    }
                }

                return pointerToLineNumbers;
            }
        }

        /// <summary>
        /// The number of relocation entries for the section. This is set to zero for PE images.
        /// </summary>
        public short NumberOfRelocations => chunk.PeekInt16(NumberOfRelocationsOffset);

        /// <summary>
        /// The number of line-number entries for the section.
        ///  This value should be zero for an image because COFF debugging information is deprecated.
        /// </summary>
        public short NumberOfLineNumbers => chunk.PeekInt16(NumberOfLineNumbersOffset);

        /// <summary>
        /// The flags that describe the characteristics of the section.
        /// </summary>
        public IMAGE_SCN Characteristics => (IMAGE_SCN) chunk.PeekUInt32(CharacteristicsOffset);

        public int Offset => chunk.AbsoluteOffset;

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
            sizeof(int);    // Characteristics

        private readonly MemoryChunk chunk;

        internal ImageSectionHeader(in MemoryChunk chunk)
        {
            pointerToRelocations = default;
            pointerToLineNumbers = default;
            this.chunk = chunk;

            //Note: we can't do any section lookups in the ctor for stuff like NumberOfLineNumbers,
            //as these require that our sections have already been created!
        }

        internal static bool TryGetHeaderChunk(in MemoryChunk chunk, int offset, out MemoryChunk headerChunk)
        {
            if (chunk.block is GlobalMemoryBlock b)
            {
                //It should be an OBJ file. If we have an Anon Header, I think we need to adjust for it
                var objFile = (OBJFile) b.File;

                var effectiveOffset = objFile.FileHeader.Offset + offset;

                headerChunk = new MemoryChunk(b, effectiveOffset);
                return true;
            }
            else if (chunk.block is GlobalSubMemoryBlock s)
            {
                //It should be an OBJ file inside a LIB. There is no Anon Header., but there _is_ an archive header. The sub-block automatically handles
                //relative addresses for us, but we need to know our relative offset relative to the start of the ImageFileHeader
                Debug.Assert(s.Owner is LongImportLibraryMember);
                headerChunk = new MemoryChunk(s, offset + ImageArchiveMemberHeader.StructSize); //Offset for LongImportLibraryMember will be the ImageArchiveMemberHeader
                return true;
            }
            else if (chunk.block is PagedMemoryBlock p)
            {
                //I don't know if the original header gets literally written into the PDB file, or if references to things are removed. Regardless,
                //the safe thing to do is not allow resolving references to things
                headerChunk = default;
                return false;
            }
            else
            {
                //It should be a PE File
                if (chunk.PEFile().TryGetValueChunkFromPhysicalOffset(offset, out headerChunk))
                    return true;

                return false;
            }
        }

        //Section numbers are 1 based
        public static bool TryGetSectionAndOffset(ImageSectionHeader[] sectionHeaders, int rva, out PDB.ISECT sectionNumber, out int relativeOffset)
        {
            for (var i = 0; i < sectionHeaders.Length; i++)
            {
                ref var sectionHeader = ref sectionHeaders[i];

                if (rva >= sectionHeader.VirtualAddress && rva <= sectionHeader.VirtualAddress + sectionHeader.VirtualSize)
                {
                    relativeOffset = rva - sectionHeader.VirtualAddress;
                    sectionNumber = (ushort) (i + 1);
                    return true;
                }
            }

            sectionNumber = default;
            relativeOffset = default;
            return false;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            writer.WriteSmallVAPointerField(PointerToRelocations, fieldOffset: PointerToRelocationsOffset);
            writer.WriteSmallVAPointerField(PointerToLineNumbers, fieldOffset: PointerToLineNumbersOffset);
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.IMAGE_SECTION_HEADER, this, ViewKind.ImageSectionHeader, StructSize);

        int IViewable.NumChildren() => 10;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteNullPaddedUtf8Field(nameof(Name), NameOffset, Name, NameSize);
                    break;

                case 1:
                    structWriter.WriteField(nameof(VirtualSize), VirtualSizeOffset, VirtualSize);
                    break;

                case 2:
                    structWriter.WriteField(nameof(VirtualAddress), VirtualAddressOffset, (int) VirtualAddress);
                    break;

                case 3:
                    structWriter.WriteField(nameof(SizeOfRawData), SizeOfRawDataOffset, SizeOfRawData);
                    break;

                case 4:
                    structWriter.WriteField(nameof(PointerToRawData), PointerToRawDataOffset, (int) PointerToRawData);
                    break;

                case 5:
                    structWriter.WriteSmallVAPointerField(nameof(PointerToRelocations), PointerToRelocationsOffset, PointerToRelocations);
                    break;

                case 6:
                    structWriter.WriteSmallVAPointerField(nameof(PointerToLineNumbers), PointerToLineNumbersOffset, PointerToLineNumbers);
                    break;

                case 7:
                    structWriter.WriteField(nameof(NumberOfRelocations), NumberOfRelocationsOffset, NumberOfRelocations);
                    break;

                case 8:
                    structWriter.WriteField(nameof(NumberOfLineNumbers), NumberOfLineNumbersOffset, NumberOfLineNumbers);
                    break;

                case 9:
                    structWriter.WriteField(nameof(Characteristics), CharacteristicsOffset, Characteristics, sizeof(int));
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }

        public override string ToString()
        {
            return Name.ToString();
        }
    }
}
