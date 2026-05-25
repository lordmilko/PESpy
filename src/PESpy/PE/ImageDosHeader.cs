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
        /// Magic number
        /// </summary>
        public ushort e_magic => chunk.PeekUInt16(MagicOffset);

        /// <summary>
        /// Bytes on last page of file
        /// </summary>
        public short e_cblp => chunk.PeekInt16(BytesOnLastPageOfFileOffset);

        /// <summary>
        /// Pages in file
        /// </summary>
        public short e_cp => chunk.PeekInt16(PagesInFileOffset);

        /// <summary>
        /// Relocations
        /// </summary>
        public short e_crlc => chunk.PeekInt16(RelocationsOffset);

        /// <summary>
        /// Size of header in paragraphs
        /// </summary>
        public short e_cparhdr => chunk.PeekInt16(SizeOfHeaderInParagraphsOffset);

        /// <summary>
        /// Minimum extra paragraphs needed
        /// </summary>
        public ushort e_minalloc => chunk.PeekUInt16(MinimumExtraParagraphsNeededOffset);

        /// <summary>
        /// Maximum extra paragraphs needed
        /// </summary>
        public ushort e_maxalloc => chunk.PeekUInt16(MaximumExtraParagraphsNeededOffset);

        /// <summary>
        /// Initial (relative) SS value
        /// </summary>
        public short e_ss => chunk.PeekInt16(InitialRelativeSSValueOffset);

        /// <summary>
        /// Initial SP value
        /// </summary>
        public short e_sp => chunk.PeekInt16(InitialSPValueOffset);

        /// <summary>
        /// Checksum
        /// </summary>
        public short e_csum => chunk.PeekInt16(ChecksumOffset);

        /// <summary>
        /// Initial IP value
        /// </summary>
        public short e_ip => chunk.PeekInt16(InitialIPValueOffset);

        /// <summary>
        /// Initial (relative) CS value
        /// </summary>
        public short e_cs => chunk.PeekInt16(InitialRelativeCSValueOffset);

        /// <summary>
        /// File address of relocation table
        /// </summary>
        public short e_lfarlc => chunk.PeekInt16(FileAddressOfRelocationTableOffset);

        /// <summary>
        /// Overlay number
        /// </summary>
        public short e_ovno => chunk.PeekInt16(OverlayNumberOffset);

        //Extended Header

        /// <summary>
        /// Reserved words
        /// </summary>
        public NativeSpan<short> e_res => chunk.PeekNativeSpan<short>(ReservedWordsOffset, 4);

        /// <summary>
        /// OEM identifier (for e_oeminfo)
        /// </summary>
        public short e_oemid => chunk.PeekInt16(OEMIdentifierOffset);

        /// <summary>
        /// OEM information; e_oemid specific
        /// </summary>
        public short e_oeminfo => chunk.PeekInt16(OEMInformationOffset);

        /// <summary>
        /// Reserved words
        /// </summary>
        public NativeSpan<short> e_res2 => chunk.PeekNativeSpan<short>(ReservedWords2Offset, 10);

        /// <summary>
        /// File address of new exe header
        /// </summary>
        public int e_lfanew => chunk.PeekInt32(FileAddressOfNewExeHeaderOffset);

        public long Offset => chunk.AbsoluteOffset;

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

            if (e_magic != IMAGE_DOS_SIGNATURE)
            {
                if (e_magic != 0 || e_cblp != -1)
                    throw new BadImageFormatException($"The specified file is not a valid {chunk.File().Kind}.");

                throw new BadImageFormatException("Unknown file format.");
            }

            //Note that you can't rely on FileAddressOfRelocationTable alone; some tools may zero this out and just set FileAddressOfNewExeHeader
            Debug.Assert(e_lfarlc > 0x1C || e_lfanew != 0, "Encountered a file without an extended header. Consider making the getters for the extended headers return default values when the extended header is known to be not present");
        }

        internal static string GetDescription(string fieldName)
        {
            return fieldName switch
            {
                nameof(e_magic) => "Magic number",
                nameof(e_cblp) => "Bytes on last page of file",
                nameof(e_cp) => "Pages in file",
                nameof(e_crlc) => "Relocations",
                nameof(e_cparhdr) => "Size of header in paragraphs",
                nameof(e_minalloc) => "Minimum extra paragraphs needed",
                nameof(e_maxalloc) => "Maximum extra paragraphs needed",
                nameof(e_ss) => "Initial (relative) SS value",
                nameof(e_sp) => "Initial SP value",
                nameof(e_csum) => "Checksum",
                nameof(e_ip) => "Initial IP value",
                nameof(e_cs) => "Initial (relative) CS value",
                nameof(e_lfarlc) => "File address of relocation table",
                nameof(e_ovno) => "Overlay number",
                nameof(e_res) => "Reserved words",
                nameof(e_oemid) => "OEM identifier (for e_oeminfo)",
                nameof(e_oeminfo) => "OEM information; e_oemid specific",
                nameof(e_res2) => "Reserved words",
                nameof(e_lfanew) => "File address of new exe header",
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
                    writer.WriteOffsetXRef(structOffset, FileAddressOfNewExeHeaderOffset, e_lfanew);
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
                    structWriter.WriteField(nameof(e_magic), MagicOffset, e_magic, FieldViewFlags.HexString);
                    break;

                case 1:
                    structWriter.WriteField(nameof(e_cblp), BytesOnLastPageOfFileOffset, e_cblp);
                    break;

                case 2:
                    structWriter.WriteField(nameof(e_cp), PagesInFileOffset, e_cp);
                    break;

                case 3:
                    structWriter.WriteField(nameof(e_crlc), RelocationsOffset, e_crlc);
                    break;

                case 4:
                    structWriter.WriteField(nameof(e_cparhdr), SizeOfHeaderInParagraphsOffset, e_cparhdr);
                    break;

                case 5:
                    structWriter.WriteField(nameof(e_minalloc), MinimumExtraParagraphsNeededOffset, e_minalloc);
                    break;

                case 6:
                    structWriter.WriteField(nameof(e_maxalloc), MaximumExtraParagraphsNeededOffset, e_maxalloc);
                    break;

                case 7:
                    structWriter.WriteField(nameof(e_ss), InitialRelativeSSValueOffset, e_ss);
                    break;

                case 8:
                    structWriter.WriteField(nameof(e_sp), InitialSPValueOffset, e_sp);
                    break;

                case 9:
                    structWriter.WriteField(nameof(e_csum), ChecksumOffset, e_csum);
                    break;

                case 10:
                    structWriter.WriteField(nameof(e_ip), InitialIPValueOffset, e_ip);
                    break;

                case 11:
                    structWriter.WriteField(nameof(e_cs), InitialRelativeCSValueOffset, e_cs);
                    break;

                case 12:
                    structWriter.WriteField(nameof(e_lfarlc), FileAddressOfRelocationTableOffset, e_lfarlc, FieldViewFlags.Address);
                    break;

                case 13:
                    structWriter.WriteField(nameof(e_ovno), OverlayNumberOffset, e_ovno);
                    break;

                case 14:
                    structWriter.WriteField(nameof(e_res), ReservedWordsOffset, e_res);
                    break;

                case 15:
                    structWriter.WriteField(nameof(e_oemid), OEMIdentifierOffset, e_oemid);
                    break;

                case 16:
                    structWriter.WriteField(nameof(e_oeminfo), OEMInformationOffset, e_oeminfo);
                    break;

                case 17:
                    structWriter.WriteField(nameof(e_res2), ReservedWords2Offset, e_res2);
                    break;

                case 18:
                    structWriter.WriteField(nameof(e_lfanew), FileAddressOfNewExeHeaderOffset, (int) e_lfanew, FieldViewFlags.Address);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
