using System;
using PESpy.Native;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy
{
    /// <summary>
    /// Represents the <see cref="IMAGE_DOS_HEADER"/> structure.
    /// </summary>
    public readonly struct ImageDosHeader : IValue, IViewable
    {
        public const ushort DosSignature = 0x5A4D;     //MZ

        /// <summary>
        /// Magic number
        /// </summary>
        public short Magic { get; init; }

        /// <summary>
        /// Bytes on last page of file
        /// </summary>
        public short BytesOnLastPageOfFile { get; init; }

        /// <summary>
        /// Pages in file
        /// </summary>
        public short PagesInFile { get; init; }

        /// <summary>
        /// Relocations
        /// </summary>
        public short Relocations { get; init; }

        /// <summary>
        /// Size of header in paragraphs
        /// </summary>
        public short SizeOfHeaderInParagraphs { get; init; }

        /// <summary>
        /// Minimum extra paragraphs needed
        /// </summary>
        public ushort MinimumExtraParagraphsNeeded { get; init; }

        /// <summary>
        /// Maximum extra paragraphs needed
        /// </summary>
        public ushort MaximumExtraParagraphsNeeded { get; init; }

        /// <summary>
        /// Initial (relative) SS value
        /// </summary>
        public short InitialRelativeSSValue { get; init; }

        /// <summary>
        /// Initial SP value
        /// </summary>
        public short InitialSPValue { get; init; }

        /// <summary>
        /// Checksum
        /// </summary>
        public short Checksum { get; init; }

        /// <summary>
        /// Initial IP value
        /// </summary>
        public short InitialIPValue { get; init; }

        /// <summary>
        /// Initial (relative) CS value
        /// </summary>
        public short InitialRelativeCSValue { get; init; }

        /// <summary>
        /// File address of relocation table
        /// </summary>
        public short FileAddressOfRelocationTable { get; init; }

        /// <summary>
        /// Overlay number
        /// </summary>
        public short OverlayNumber { get; init; }

        /// <summary>
        /// Reserved words
        /// </summary>
        public short[] ReservedWords { get; init; }

        /// <summary>
        /// OEM identifier (for e_oeminfo)
        /// </summary>
        public short OEMIdentifier { get; init; }

        /// <summary>
        /// OEM information; e_oemid specific
        /// </summary>
        public short OEMInformation { get; init; }

        /// <summary>
        /// Reserved words
        /// </summary>
        public short[] ReservedWords2 { get; init; }

        /// <summary>
        /// File address of new exe header
        /// </summary>
        public RawOffset FileAddressOfNewExeHeader { get; init; }

        public RawOffset Offset { get; }

        internal const int StructSize =
            sizeof(short) +      //Magic
            sizeof(short) +      //BytesOnLastPageOfFile
            sizeof(short) +      //PagesInFile
            sizeof(short) +      //Relocations
            sizeof(short) +      //SizeOfHeaderInParagraphs
            sizeof(short) +      //MinimumExtraParagraphsNeeded
            sizeof(short) +      //MaximumExtraParagraphsNeeded
            sizeof(short) +      //InitialRelativeSSValue
            sizeof(short) +      //InitialSPValue
            sizeof(short) +      //Checksum
            sizeof(short) +      //InitialIPValue
            sizeof(short) +      //InitialRelativeCSValue
            sizeof(short) +      //FileAddressOfRelocationTable
            sizeof(short) +      //OverlayNumber
            sizeof(short) * 4 +  //ReservedWords
            sizeof(short) +      //OEMIdentifier
            sizeof(short) +      //OEMInformation
            sizeof(short) * 10 + //ReservedWords2
            sizeof(int);

        internal ImageDosHeader(ref FileReader reader)
        {
            Offset = (RawOffset) reader.Position;

            reader.FillBuffer(StructSize);

            Magic = reader.ReadInt16();
            BytesOnLastPageOfFile = reader.ReadInt16();

            if (Magic != DosSignature)
            {
                if (Magic != 0 || BytesOnLastPageOfFile != -1)
                {
                    reader.Seek(0);
                    throw new BadImageFormatException("Don't know how to handle COFF file.");
                }

                throw new BadImageFormatException("Unknown file format.");
            }

            PagesInFile = reader.ReadInt16();
            Relocations = reader.ReadInt16();
            SizeOfHeaderInParagraphs = reader.ReadInt16();
            MinimumExtraParagraphsNeeded = reader.ReadUInt16();
            MaximumExtraParagraphsNeeded = reader.ReadUInt16();
            InitialRelativeSSValue = reader.ReadInt16();
            InitialSPValue = reader.ReadInt16();
            Checksum = reader.ReadInt16();
            InitialIPValue = reader.ReadInt16();
            InitialRelativeCSValue = reader.ReadInt16();
            FileAddressOfRelocationTable = reader.ReadInt16();
            OverlayNumber = reader.ReadInt16();
            ReservedWords = reader.ReadArray<short>(4);
            OEMIdentifier = reader.ReadInt16();
            OEMInformation = reader.ReadInt16();
            ReservedWords2 = reader.ReadArray<short>(10);
            FileAddressOfNewExeHeader = (RawOffset) reader.ReadInt32();
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct(nameof(IMAGE_DOS_HEADER), this, ViewKind.ImageDosHeader);

            s.WriteField(nameof(IMAGE_DOS_HEADER.e_magic), Magic);
            s.WriteField(nameof(IMAGE_DOS_HEADER.e_cblp), BytesOnLastPageOfFile);
            s.WriteField(nameof(IMAGE_DOS_HEADER.e_cp), PagesInFile);
            s.WriteField(nameof(IMAGE_DOS_HEADER.e_crlc), Relocations);
            s.WriteField(nameof(IMAGE_DOS_HEADER.e_cparhdr), SizeOfHeaderInParagraphs);
            s.WriteField(nameof(IMAGE_DOS_HEADER.e_minalloc), MinimumExtraParagraphsNeeded);
            s.WriteField(nameof(IMAGE_DOS_HEADER.e_maxalloc), MaximumExtraParagraphsNeeded);
            s.WriteField(nameof(IMAGE_DOS_HEADER.e_ss), InitialRelativeSSValue);
            s.WriteField(nameof(IMAGE_DOS_HEADER.e_sp), InitialSPValue);
            s.WriteField(nameof(IMAGE_DOS_HEADER.e_csum), Checksum);
            s.WriteField(nameof(IMAGE_DOS_HEADER.e_ip), InitialIPValue);
            s.WriteField(nameof(IMAGE_DOS_HEADER.e_cs), InitialRelativeCSValue);
            s.WriteField(nameof(IMAGE_DOS_HEADER.e_lfarlc), FileAddressOfRelocationTable);
            s.WriteField(nameof(IMAGE_DOS_HEADER.e_ovno), OverlayNumber);
            s.WriteField(nameof(IMAGE_DOS_HEADER.e_res), ReservedWords);
            s.WriteField(nameof(IMAGE_DOS_HEADER.e_oemid), OEMIdentifier);
            s.WriteField(nameof(IMAGE_DOS_HEADER.e_oeminfo), OEMInformation);
            s.WriteField(nameof(IMAGE_DOS_HEADER.e_res2), ReservedWords2);
            s.WriteField(nameof(IMAGE_DOS_HEADER.e_lfanew), (int) FileAddressOfNewExeHeader);
        }
    }
}
