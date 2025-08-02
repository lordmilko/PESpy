using System;
using System.Diagnostics;
using PESpy.Native;
using PESpy.View;

namespace PESpy
{
    /// <summary>
    /// Represents the <see cref="IMAGE_DOS_HEADER"/> structure.
    /// </summary>
    public readonly struct ImageDosHeader : IValue, IViewable
    {
        public const ushort IMAGE_DOS_SIGNATURE = 0x5A4D;     //MZ

        /// <summary>
        /// Magic number<para/>
        /// e_magic
        /// </summary>
        public ushort Magic => chunk.PeekUInt16(0);

        /// <summary>
        /// Bytes on last page of file<para/>
        /// e_cblp
        /// </summary>
        public short BytesOnLastPageOfFile => chunk.PeekInt16(2);

        /// <summary>
        /// Pages in file<para/>
        /// e_cp
        /// </summary>
        public short PagesInFile => chunk.PeekInt16(4);

        /// <summary>
        /// Relocations<para/>
        /// e_crlc
        /// </summary>
        public short Relocations => chunk.PeekInt16(6);

        /// <summary>
        /// Size of header in paragraphs<para/>
        /// e_cparhdr
        /// </summary>
        public short SizeOfHeaderInParagraphs => chunk.PeekInt16(8);

        /// <summary>
        /// Minimum extra paragraphs needed<para/>
        /// e_minalloc
        /// </summary>
        public ushort MinimumExtraParagraphsNeeded => chunk.PeekUInt16(10);

        /// <summary>
        /// Maximum extra paragraphs needed<para/>
        /// e_maxalloc
        /// </summary>
        public ushort MaximumExtraParagraphsNeeded => chunk.PeekUInt16(12);

        /// <summary>
        /// Initial (relative) SS value<para/>
        /// e_ss
        /// </summary>
        public short InitialRelativeSSValue => chunk.PeekInt16(14);

        /// <summary>
        /// Initial SP value<para/>
        /// e_sp
        /// </summary>
        public short InitialSPValue => chunk.PeekInt16(16);

        /// <summary>
        /// Checksum<para/>
        /// e_csum
        /// </summary>
        public short Checksum => chunk.PeekInt16(18);

        /// <summary>
        /// Initial IP value<para/>
        /// e_ip
        /// </summary>
        public short InitialIPValue => chunk.PeekInt16(20);

        /// <summary>
        /// Initial (relative) CS value<para/>
        /// e_cs
        /// </summary>
        public short InitialRelativeCSValue => chunk.PeekInt16(22);

        /// <summary>
        /// File address of relocation table<para/>
        /// e_lfarlc
        /// </summary>
        public short FileAddressOfRelocationTable => chunk.PeekInt16(24);

        /// <summary>
        /// Overlay number<para/>
        /// e_ovno
        /// </summary>
        public short OverlayNumber => chunk.PeekInt16(26);

        //Extended Header

        /// <summary>
        /// Reserved words<para/>
        /// e_res
        /// </summary>
        public NativeSpan<short> ReservedWords => chunk.PeekNativeSpan<short>(28, 4);

        /// <summary>
        /// OEM identifier (for e_oeminfo)<para/>
        /// e_oemid
        /// </summary>
        public short OEMIdentifier => chunk.PeekInt16(36);

        /// <summary>
        /// OEM information; e_oemid specific<para/>
        /// e_oeminfo
        /// </summary>
        public short OEMInformation => chunk.PeekInt16(38);

        /// <summary>
        /// Reserved words<para/>
        /// e_res2
        /// </summary>
        public NativeSpan<short> ReservedWords2 => chunk.PeekNativeSpan<short>(40, 10);

        /// <summary>
        /// File address of new exe header<para/>
        /// e_lfanew
        /// </summary>
        public int FileAddressOfNewExeHeader => chunk.PeekInt32(60);

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
                    throw new BadImageFormatException("Don't know how to handle COFF file.");

                throw new BadImageFormatException("Unknown file format.");
            }

            //Note that you can't rely on FileAddressOfRelocationTable alone; some tools may zero this out and just set FileAddressOfNewExeHeader
            Debug.Assert(FileAddressOfRelocationTable > 0x1C || FileAddressOfNewExeHeader != 0, "Encountered a file without an extended header. Consider making the getters for the extended headers return default values when the extended header is known to be not present");
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.IMAGE_DOS_HEADER, this, ViewKind.ImageDosHeader, StructSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            //Fields after e_ovno are considered to be part of the "extended header", and are only present
            //if e_lfarlc is greater than 0x1C
            http://justsolve.archiveteam.org/wiki/MS-DOS_EXE

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

            //Note that you can't rely on FileAddressOfRelocationTable alone; some tools may zero this out and just set FileAddressOfNewExeHeader
            Debug.Assert(FileAddressOfRelocationTable > 0x1C || FileAddressOfNewExeHeader != 0, "Encountered a file without an extended header. Consider making the getters for the extended headers return default values when the extended header is known to be not present");

            //Extended header
            s.WriteField(nameof(IMAGE_DOS_HEADER.e_res), ReservedWords);
            s.WriteField(nameof(IMAGE_DOS_HEADER.e_oemid), OEMIdentifier);
            s.WriteField(nameof(IMAGE_DOS_HEADER.e_oeminfo), OEMInformation);
            s.WriteField(nameof(IMAGE_DOS_HEADER.e_res2), ReservedWords2);
            s.WriteField(nameof(IMAGE_DOS_HEADER.e_lfanew), (int) FileAddressOfNewExeHeader);

            return s.ToArray();
        }
    }
}
