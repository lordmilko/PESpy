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
        public const ushort IMAGE_DOS_SIGNATURE = 0x5A4D;     //MZ

        /// <summary>
        /// Magic number
        /// </summary>
#if PEFAST
        public ushort Magic => chunk.PeekUInt16(0);
#else
        public ushort Magic { get; init; }
#endif

        /// <summary>
        /// Bytes on last page of file
        /// </summary>
#if PEFAST
        public short BytesOnLastPageOfFile => chunk.PeekInt16(2);
#else
        public short BytesOnLastPageOfFile { get; init; }
#endif

        /// <summary>
        /// Pages in file
        /// </summary>
#if PEFAST
        public short PagesInFile => chunk.PeekInt16(4);
#else
        public short PagesInFile { get; init; }
#endif

        /// <summary>
        /// Relocations
        /// </summary>
#if PEFAST
        public short Relocations => chunk.PeekInt16(6);
#else
        public short Relocations { get; init; }
#endif

        /// <summary>
        /// Size of header in paragraphs
        /// </summary>
#if PEFAST
        public short SizeOfHeaderInParagraphs => chunk.PeekInt16(8);
#else
        public short SizeOfHeaderInParagraphs { get; init; }
#endif

        /// <summary>
        /// Minimum extra paragraphs needed
        /// </summary>
#if PEFAST
        public ushort MinimumExtraParagraphsNeeded => chunk.PeekUInt16(10);
#else
        public ushort MinimumExtraParagraphsNeeded { get; init; }
#endif

        /// <summary>
        /// Maximum extra paragraphs needed
        /// </summary>
#if PEFAST
        public ushort MaximumExtraParagraphsNeeded => chunk.PeekUInt16(12);
#else
        public ushort MaximumExtraParagraphsNeeded { get; init; }
#endif

        /// <summary>
        /// Initial (relative) SS value
        /// </summary>
#if PEFAST
        public short InitialRelativeSSValue => chunk.PeekInt16(14);
#else
        public short InitialRelativeSSValue { get; init; }
#endif

        /// <summary>
        /// Initial SP value
        /// </summary>
#if PEFAST
        public short InitialSPValue => chunk.PeekInt16(16);
#else
        public short InitialSPValue { get; init; }
#endif

        /// <summary>
        /// Checksum
        /// </summary>
#if PEFAST
        public short Checksum => chunk.PeekInt16(18);
#else
        public short Checksum { get; init; }
#endif

        /// <summary>
        /// Initial IP value
        /// </summary>
#if PEFAST
        public short InitialIPValue => chunk.PeekInt16(20);
#else
        public short InitialIPValue { get; init; }
#endif

        /// <summary>
        /// Initial (relative) CS value
        /// </summary>
#if PEFAST
        public short InitialRelativeCSValue => chunk.PeekInt16(22);
#else
        public short InitialRelativeCSValue { get; init; }
#endif

        /// <summary>
        /// File address of relocation table
        /// </summary>
#if PEFAST
        public short FileAddressOfRelocationTable => chunk.PeekInt16(24);
#else
        public short FileAddressOfRelocationTable { get; init; }
#endif

        /// <summary>
        /// Overlay number
        /// </summary>
#if PEFAST
        public short OverlayNumber => chunk.PeekInt16(26);
#else
        public short OverlayNumber { get; init; }
#endif

        /// <summary>
        /// Reserved words
        /// </summary>
#if PEFAST
        public Span<short> ReservedWords => chunk.PeekSpan<short>(28, 4);
#else
        public short[] ReservedWords { get; init; }
#endif

        /// <summary>
        /// OEM identifier (for e_oeminfo)
        /// </summary>
#if PEFAST
        public short OEMIdentifier => chunk.PeekInt16(36);
#else
        public short OEMIdentifier { get; init; }
#endif

        /// <summary>
        /// OEM information; e_oemid specific
        /// </summary>
#if PEFAST
        public short OEMInformation => chunk.PeekInt16(38);
#else
        public short OEMInformation { get; init; }
#endif

        /// <summary>
        /// Reserved words
        /// </summary>
#if PEFAST
        public Span<short> ReservedWords2 => chunk.PeekSpan<short>(40, 10);
#else
        public short[] ReservedWords2 { get; init; }
#endif

        /// <summary>
        /// File address of new exe header
        /// </summary>
#if PEFAST
        public int FileAddressOfNewExeHeader => chunk.PeekInt32(60);
#else
        public RawOffset FileAddressOfNewExeHeader { get; init; }
#endif

#if PEFAST
        public RawOffset Offset => chunk.AbsoluteOffset;
#else
        public RawOffset Offset { get; }
#endif

        internal const int StructSize =
            sizeof(ushort) +     //Magic
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

#if PEFAST
        private readonly MemoryChunk chunk;

        internal ImageDosHeader(in MemoryChunk chunk)
        {
            this.chunk = chunk;

            if (Magic != IMAGE_DOS_SIGNATURE)
            {
                if (Magic != 0 || BytesOnLastPageOfFile != -1)
                    throw new BadImageFormatException("Don't know how to handle COFF file.");

                throw new BadImageFormatException("Unknown file format.");
            }
        }
#else
        internal ImageDosHeader(IFileReader reader)
        {
            Offset = (RawOffset) reader.Position;

            reader.FillBuffer(StructSize);

            Magic = reader.ReadUInt16();
            BytesOnLastPageOfFile = reader.ReadInt16();

            if (Magic != IMAGE_DOS_SIGNATURE)
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

            //We don't yet know whether we're actually a PE File or an MS-DOS executable. You can't look at e_lfanew because sometimes it contains garbage
            //(or perhaps more precisely: data that makes sense to MS-DOS). As such, the caller will need to load the NT Header to see if e_lfanew actually points
            //to the PE signature
        }
#endif

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
