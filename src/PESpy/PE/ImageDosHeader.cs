using System;
using System.Diagnostics;
using PESpy.Native;
using PESpy.View;

namespace PESpy
{
    /// <summary>
    /// Represents the <see cref="IMAGE_DOS_HEADER"/> structure.
    /// </summary>
    [Source(SourceKind.winnt_h | SourceKind.exehdr_h)] //Also known as exe_hdr in old versions of Windows
    public readonly struct ImageDosHeader : IValue, IViewable
    {
        public const ushort IMAGE_DOS_SIGNATURE = 0x5A4D;     //MZ

        private const int MagicOffset = 0;
        private const int BytesOnLastPageOfFileOffset = 2;
        private const int PagesInFileOffset = 4;
        private const int RelocationsOffset = 6;
        private const int SizeOfHeaderInParagraphsOffset = 8;
        private const int MinimumExtraParagraphsNeededOffset = 10;
        private const int MaximumExtraParagraphsNeededOffset = 12;
        private const int InitialRelativeSSValueOffset = 14;
        private const int InitialSPValueOffset = 16;
        private const int ChecksumOffset = 18;
        private const int InitialIPValueOffset = 20;
        private const int InitialRelativeCSValueOffset = 22;
        private const int FileAddressOfRelocationTableOffset = 24;
        private const int OverlayNumberOffset = 26;
        private const int ReservedWordsOffset = 28;
        private const int OEMIdentifierOffset = 36;
        private const int OEMInformationOffset = 38;
        private const int ReservedWords2Offset = 40;
        internal const int FileAddressOfNewExeHeaderOffset = 60;

        /// <summary>
        /// Magic number<para/>
        /// e_magic
        /// </summary>
        public ushort Magic => chunk.PeekUInt16(MagicOffset);

        /// <summary>
        /// Bytes on last page of file<para/>
        /// e_cblp
        /// </summary>
        public short BytesOnLastPageOfFile => chunk.PeekInt16(BytesOnLastPageOfFileOffset);

        /// <summary>
        /// Pages in file<para/>
        /// e_cp
        /// </summary>
        public short PagesInFile => chunk.PeekInt16(PagesInFileOffset);

        /// <summary>
        /// Relocations<para/>
        /// e_crlc
        /// </summary>
        public short Relocations => chunk.PeekInt16(RelocationsOffset);

        /// <summary>
        /// Size of header in paragraphs<para/>
        /// e_cparhdr
        /// </summary>
        public short SizeOfHeaderInParagraphs => chunk.PeekInt16(SizeOfHeaderInParagraphsOffset);

        /// <summary>
        /// Minimum extra paragraphs needed<para/>
        /// e_minalloc
        /// </summary>
        public ushort MinimumExtraParagraphsNeeded => chunk.PeekUInt16(MinimumExtraParagraphsNeededOffset);

        /// <summary>
        /// Maximum extra paragraphs needed<para/>
        /// e_maxalloc
        /// </summary>
        public ushort MaximumExtraParagraphsNeeded => chunk.PeekUInt16(MaximumExtraParagraphsNeededOffset);

        /// <summary>
        /// Initial (relative) SS value<para/>
        /// e_ss
        /// </summary>
        public short InitialRelativeSSValue => chunk.PeekInt16(InitialRelativeSSValueOffset);

        /// <summary>
        /// Initial SP value<para/>
        /// e_sp
        /// </summary>
        public short InitialSPValue => chunk.PeekInt16(InitialSPValueOffset);

        /// <summary>
        /// Checksum<para/>
        /// e_csum
        /// </summary>
        public short Checksum => chunk.PeekInt16(ChecksumOffset);

        /// <summary>
        /// Initial IP value<para/>
        /// e_ip
        /// </summary>
        public short InitialIPValue => chunk.PeekInt16(InitialIPValueOffset);

        /// <summary>
        /// Initial (relative) CS value<para/>
        /// e_cs
        /// </summary>
        public short InitialRelativeCSValue => chunk.PeekInt16(InitialRelativeCSValueOffset);

        /// <summary>
        /// File address of relocation table<para/>
        /// e_lfarlc
        /// </summary>
        public short FileAddressOfRelocationTable => chunk.PeekInt16(FileAddressOfRelocationTableOffset);

        /// <summary>
        /// Overlay number<para/>
        /// e_ovno
        /// </summary>
        public short OverlayNumber => chunk.PeekInt16(OverlayNumberOffset);

        //Extended Header

        /// <summary>
        /// Reserved words<para/>
        /// e_res
        /// </summary>
        public NativeSpan<short> ReservedWords => chunk.PeekNativeSpan<short>(ReservedWordsOffset, 4);

        /// <summary>
        /// OEM identifier (for e_oeminfo)<para/>
        /// e_oemid
        /// </summary>
        public short OEMIdentifier => chunk.PeekInt16(OEMIdentifierOffset);

        /// <summary>
        /// OEM information; e_oemid specific<para/>
        /// e_oeminfo
        /// </summary>
        public short OEMInformation => chunk.PeekInt16(OEMInformationOffset);

        /// <summary>
        /// Reserved words<para/>
        /// e_res2
        /// </summary>
        public NativeSpan<short> ReservedWords2 => chunk.PeekNativeSpan<short>(ReservedWords2Offset, 10);

        /// <summary>
        /// File address of new exe header<para/>
        /// e_lfanew
        /// </summary>
        public int FileAddressOfNewExeHeader => chunk.PeekInt32(FileAddressOfNewExeHeaderOffset);

        public int Offset => chunk.AbsoluteOffset;

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

        private readonly MemoryChunk chunk;

        internal ImageDosHeader(in MemoryChunk chunk)
        {
            this.chunk = chunk;

            if (Magic != IMAGE_DOS_SIGNATURE)
            {
                if (Magic != 0 || BytesOnLastPageOfFile != -1)
                    throw new BadImageFormatException($"The specified file is not a valid {chunk.File().Kind}.");

                throw new BadImageFormatException("Unknown file format.");
            }

            //Note that you can't rely on FileAddressOfRelocationTable alone; some tools may zero this out and just set FileAddressOfNewExeHeader
            Debug.Assert(FileAddressOfRelocationTable > 0x1C || FileAddressOfNewExeHeader != 0, "Encountered a file without an extended header. Consider making the getters for the extended headers return default values when the extended header is known to be not present");
        }

        internal static string GetDescription(string fieldName)
        {
            return fieldName switch
            {
                nameof(IMAGE_DOS_HEADER.e_magic) => "Magic number",
                nameof(IMAGE_DOS_HEADER.e_cblp) => "Bytes on last page of file",
                nameof(IMAGE_DOS_HEADER.e_cp) => "Pages in file",
                nameof(IMAGE_DOS_HEADER.e_crlc) => "Relocations",
                nameof(IMAGE_DOS_HEADER.e_cparhdr) => "Size of header in paragraphs",
                nameof(IMAGE_DOS_HEADER.e_minalloc) => "Minimum extra paragraphs needed",
                nameof(IMAGE_DOS_HEADER.e_maxalloc) => "Maximum extra paragraphs needed",
                nameof(IMAGE_DOS_HEADER.e_ss) => "Initial (relative) SS value",
                nameof(IMAGE_DOS_HEADER.e_sp) => "Initial SP value",
                nameof(IMAGE_DOS_HEADER.e_csum) => "Checksum",
                nameof(IMAGE_DOS_HEADER.e_ip) => "Initial IP value",
                nameof(IMAGE_DOS_HEADER.e_cs) => "Initial (relative) CS value",
                nameof(IMAGE_DOS_HEADER.e_lfarlc) => "File address of relocation table",
                nameof(IMAGE_DOS_HEADER.e_ovno) => "Overlay number",
                nameof(IMAGE_DOS_HEADER.e_res) => "Reserved words",
                nameof(IMAGE_DOS_HEADER.e_oemid) => "OEM identifier (for e_oeminfo)",
                nameof(IMAGE_DOS_HEADER.e_oeminfo) => "OEM information; e_oemid specific",
                nameof(IMAGE_DOS_HEADER.e_res2) => "Reserved words",
                nameof(IMAGE_DOS_HEADER.e_lfanew) => "File address of new exe header",
            };
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            var structOffset = Offset;

            switch (chunk.File().Kind)
            {
                case FileKind.PE:
                case FileKind.NE:
                case FileKind.LE:
                    writer.WriteOffsetXRef(structOffset, FileAddressOfNewExeHeaderOffset, FileAddressOfNewExeHeader);
                    break;
            }
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.ImageDosHeader, StructSize);

        int IViewable.NumChildren() => 19;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(IMAGE_DOS_HEADER.e_magic), MagicOffset, Magic, FieldViewFlags.HexString);
                    break;

                case 1:
                    structWriter.WriteField(nameof(IMAGE_DOS_HEADER.e_cblp), BytesOnLastPageOfFileOffset, BytesOnLastPageOfFile);
                    break;

                case 2:
                    structWriter.WriteField(nameof(IMAGE_DOS_HEADER.e_cp), PagesInFileOffset, PagesInFile);
                    break;

                case 3:
                    structWriter.WriteField(nameof(IMAGE_DOS_HEADER.e_crlc), RelocationsOffset, Relocations);
                    break;

                case 4:
                    structWriter.WriteField(nameof(IMAGE_DOS_HEADER.e_cparhdr), SizeOfHeaderInParagraphsOffset, SizeOfHeaderInParagraphs);
                    break;

                case 5:
                    structWriter.WriteField(nameof(IMAGE_DOS_HEADER.e_minalloc), MinimumExtraParagraphsNeededOffset, MinimumExtraParagraphsNeeded);
                    break;

                case 6:
                    structWriter.WriteField(nameof(IMAGE_DOS_HEADER.e_maxalloc), MaximumExtraParagraphsNeededOffset, MaximumExtraParagraphsNeeded);
                    break;

                case 7:
                    structWriter.WriteField(nameof(IMAGE_DOS_HEADER.e_ss), InitialRelativeSSValueOffset, InitialRelativeSSValue);
                    break;

                case 8:
                    structWriter.WriteField(nameof(IMAGE_DOS_HEADER.e_sp), InitialSPValueOffset, InitialSPValue);
                    break;

                case 9:
                    structWriter.WriteField(nameof(IMAGE_DOS_HEADER.e_csum), ChecksumOffset, Checksum);
                    break;

                case 10:
                    structWriter.WriteField(nameof(IMAGE_DOS_HEADER.e_ip), InitialIPValueOffset, InitialIPValue);
                    break;

                case 11:
                    structWriter.WriteField(nameof(IMAGE_DOS_HEADER.e_cs), InitialRelativeCSValueOffset, InitialRelativeCSValue);
                    break;

                case 12:
                    structWriter.WriteField(nameof(IMAGE_DOS_HEADER.e_lfarlc), FileAddressOfRelocationTableOffset, FileAddressOfRelocationTable, FieldViewFlags.Address);
                    break;

                case 13:
                    structWriter.WriteField(nameof(IMAGE_DOS_HEADER.e_ovno), OverlayNumberOffset, OverlayNumber);
                    break;

                case 14:
                    structWriter.WriteField(nameof(IMAGE_DOS_HEADER.e_res), ReservedWordsOffset, ReservedWords);
                    break;

                case 15:
                    structWriter.WriteField(nameof(IMAGE_DOS_HEADER.e_oemid), OEMIdentifierOffset, OEMIdentifier);
                    break;

                case 16:
                    structWriter.WriteField(nameof(IMAGE_DOS_HEADER.e_oeminfo), OEMInformationOffset, OEMInformation);
                    break;

                case 17:
                    structWriter.WriteField(nameof(IMAGE_DOS_HEADER.e_res2), ReservedWords2Offset, ReservedWords2);
                    break;

                case 18:
                    structWriter.WriteField(nameof(IMAGE_DOS_HEADER.e_lfanew), FileAddressOfNewExeHeaderOffset, (int) FileAddressOfNewExeHeader, FieldViewFlags.Address);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
