using System;
using System.Diagnostics;
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
        /// Magic number<para/>
        /// e_magic
        /// </summary>
#if PEFAST
        public ushort Magic => chunk.PeekUInt16(0);
#else
        public ushort Magic { get; init; }
#endif

        /// <summary>
        /// Bytes on last page of file<para/>
        /// e_cblp
        /// </summary>
#if PEFAST
        public short BytesOnLastPageOfFile => chunk.PeekInt16(2);
#else
        public short BytesOnLastPageOfFile { get; init; }
#endif

        /// <summary>
        /// Pages in file<para/>
        /// e_cp
        /// </summary>
#if PEFAST
        public short PagesInFile => chunk.PeekInt16(4);
#else
        public short PagesInFile { get; init; }
#endif

        /// <summary>
        /// Relocations<para/>
        /// e_crlc
        /// </summary>
#if PEFAST
        public short Relocations => chunk.PeekInt16(6);
#else
        public short Relocations { get; init; }
#endif

        /// <summary>
        /// Size of header in paragraphs<para/>
        /// e_cparhdr
        /// </summary>
#if PEFAST
        public short SizeOfHeaderInParagraphs => chunk.PeekInt16(8);
#else
        public short SizeOfHeaderInParagraphs { get; init; }
#endif

        /// <summary>
        /// Minimum extra paragraphs needed<para/>
        /// e_minalloc
        /// </summary>
#if PEFAST
        public ushort MinimumExtraParagraphsNeeded => chunk.PeekUInt16(10);
#else
        public ushort MinimumExtraParagraphsNeeded { get; init; }
#endif

        /// <summary>
        /// Maximum extra paragraphs needed<para/>
        /// e_maxalloc
        /// </summary>
#if PEFAST
        public ushort MaximumExtraParagraphsNeeded => chunk.PeekUInt16(12);
#else
        public ushort MaximumExtraParagraphsNeeded { get; init; }
#endif

        /// <summary>
        /// Initial (relative) SS value<para/>
        /// e_ss
        /// </summary>
#if PEFAST
        public short InitialRelativeSSValue => chunk.PeekInt16(14);
#else
        public short InitialRelativeSSValue { get; init; }
#endif

        /// <summary>
        /// Initial SP value<para/>
        /// e_sp
        /// </summary>
#if PEFAST
        public short InitialSPValue => chunk.PeekInt16(16);
#else
        public short InitialSPValue { get; init; }
#endif

        /// <summary>
        /// Checksum<para/>
        /// e_csum
        /// </summary>
#if PEFAST
        public short Checksum => chunk.PeekInt16(18);
#else
        public short Checksum { get; init; }
#endif

        /// <summary>
        /// Initial IP value<para/>
        /// e_ip
        /// </summary>
#if PEFAST
        public short InitialIPValue => chunk.PeekInt16(20);
#else
        public short InitialIPValue { get; init; }
#endif

        /// <summary>
        /// Initial (relative) CS value<para/>
        /// e_cs
        /// </summary>
#if PEFAST
        public short InitialRelativeCSValue => chunk.PeekInt16(22);
#else
        public short InitialRelativeCSValue { get; init; }
#endif

        /// <summary>
        /// File address of relocation table<para/>
        /// e_lfarlc
        /// </summary>
#if PEFAST
        public short FileAddressOfRelocationTable => chunk.PeekInt16(24);
#else
        public short FileAddressOfRelocationTable { get; init; }
#endif

        /// <summary>
        /// Overlay number<para/>
        /// e_ovno
        /// </summary>
#if PEFAST
        public short OverlayNumber => chunk.PeekInt16(26);
#else
        public short OverlayNumber { get; init; }
#endif

        //Extended Header

        /// <summary>
        /// Reserved words<para/>
        /// e_res
        /// </summary>
#if PEFAST
        public NativeSpan<short> ReservedWords => chunk.PeekNativeSpan<short>(28, 4);
#else
        public short[] ReservedWords { get; init; }
#endif

        /// <summary>
        /// OEM identifier (for e_oeminfo)<para/>
        /// e_oemid
        /// </summary>
#if PEFAST
        public short OEMIdentifier => chunk.PeekInt16(36);
#else
        public short OEMIdentifier { get; init; }
#endif

        /// <summary>
        /// OEM information; e_oemid specific<para/>
        /// e_oeminfo
        /// </summary>
#if PEFAST
        public short OEMInformation => chunk.PeekInt16(38);
#else
        public short OEMInformation { get; init; }
#endif

        /// <summary>
        /// Reserved words<para/>
        /// e_res2
        /// </summary>
#if PEFAST
        public NativeSpan<short> ReservedWords2 => chunk.PeekNativeSpan<short>(40, 10);
#else
        public short[] ReservedWords2 { get; init; }
#endif

        /// <summary>
        /// File address of new exe header<para/>
        /// e_lfanew
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

            //Note that you can't rely on FileAddressOfRelocationTable alone; some tools may zero this out and just set FileAddressOfNewExeHeader
            Debug.Assert(FileAddressOfRelocationTable > 0x1C || FileAddressOfNewExeHeader != 0, "Encountered a file without an extended header. Consider making the getters for the extended headers return default values when the extended header is known to be not present");
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
