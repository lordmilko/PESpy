using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
#if NET5_0_OR_GREATER
using System.Runtime.InteropServices;
#endif
using System.Text;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the BIGMSF_HDR structure used in PDB v7 files.
    /// </summary>
    [Source(SourceKind.msf_cpp)]
    public readonly partial struct BigMsfHdr : IValue, IViewable
    {
        internal const string BigHdrMagic = "Microsoft C/C++ MSF 7.00\r\n\u001aDS\0\0\0";

        private const int MagicOffset = 0;
        private const int PageSizeOffset = 32;
        private const int FpmPageNoOffset = 36;
        private const int NumPagesOffset = 40;
        private const int StreamTableSizeInfoOffset = 44;
        private const int PagesOfStreamTablePageListOffset = 52;

        //szMagic
        public FixedAnsiString Magic
        {
            get => chunk.PeekAnsiFixedLength(MagicOffset, 32);
            set => chunk.PokeAnsiFixedLength(MagicOffset, 32, value);
        }

        public unsafe void SetMagic(string magic)
        {
            using var builder = new Utf8StringBuilder(magic);

            fixed (byte* p = builder.AsSpan())
            {
                var str = new FixedAnsiString(p, builder.Length);
                Magic = str;
            }
        }

        /// <summary>
        /// Gets or sets the size of each page in the PDB.<para/>
        /// PDBs can only be as big as there are bits in the free page map to record each page as being available or not.
        /// Thus, the larger the page size, the more data you can store in your PDB.<para/>
        /// cbPg
        /// </summary>
        public int PageSize
        {
            get => chunk.PeekInt32(PageSizeOffset);
            set => chunk.PokeInt32(PageSizeOffset, value);
        } //cbPg

        /// <summary>
        /// Gets or sets which of the two FPM pages is currently the active one.<para/>
        /// pnFpm
        /// </summary>
        public PN FpmPageNo
        {
            get => chunk.PeekUInt32(FpmPageNoOffset);
            set => chunk.PokeUInt32(FpmPageNoOffset, value);
        } //pnFpm

        /// <summary>
        /// Gets or sets the total number of pages contained in this PDB (including the master page (0).<para/>
        /// pnMac
        /// </summary>
        public int NumPages
        {
            get => chunk.PeekInt32(NumPagesOffset);
            set => chunk.PokeInt32(NumPagesOffset, value);
        } //pnMac

        /// <summary>
        /// Lists the size of the stream table that describes the streams that exist in the PDB<para/>
        /// siSt
        /// </summary>
        public SI_PERSIST StreamTableSizeInfo { get; } //siSt

        /// <summary>
        /// Lists the page numbers of a stream that contains an array of page numbers that the stream table
        /// is distributed across. BIGMSF_HDR does not list this member explicitly; instead, it lists an array
        /// mpspnpnSt that can hold 19 members, the first member of which is this value.<para/>
        /// mpspnpnSt
        /// </summary>
        /// <remarks>
        /// Suppose that the Stream Table is 524,288 bytes and we have 1024 byte pages. The Stream table itself spans 512 pages.
        /// A single PN is 4 bytes, so merely recording the existance of these 512 pages requires 2048 bytes. Which means that
        /// the list of PNs will itself span two pages
        /// </remarks>
        public NativeSpan<PN> PagesOfStreamTablePageList
        {
            get => chunk.PeekNativeSpan<PN>(PagesOfStreamTablePageListOffset, SI.DivideUp((SI.DivideUp(StreamTableSizeInfo.ByteCount, PageSize) * 4), PageSize));
            set => chunk.PokeNativeSpan<PN>(PagesOfStreamTablePageListOffset, SI.DivideUp((SI.DivideUp(StreamTableSizeInfo.ByteCount, PageSize) * 4), PageSize), value);
        } //Normally there will be a single page that lists the location of the stream table. However, once you have enough data, additional pages to represent the stream table may be required

        public void SetPagesOfStreamTablePageList(List<PN> newPages)
        {
            //This should be called after updating the new StreamTableSizeInfo.ByteCount, which will ensure that the native span that we peek
            //is the right size
            var dest = (Span<PN>) PagesOfStreamTablePageList;

            Span<PN> source;
#if NET5_0_OR_GREATER
            source = CollectionsMarshal.AsSpan<PN>(newPages);
            source.CopyTo(dest);
#else
            var arr = ArrayPool<PN>.Shared.Rent(newPages.Count);
            newPages.CopyTo(arr);

            try
            {
                source = new Span<PN>(arr, 0, newPages.Count);
                source.CopyTo(dest);
            }
            finally
            {
                ArrayPool<PN>.Shared.Return(arr);
            }
#endif
        }

        public long Offset => chunk.AbsoluteOffset;

        internal const int FixedStructSize =
            32 +          //32
            sizeof(int) + //PageSize
            sizeof(int) + //FpmPageNo
            sizeof(int) + //NumPages
            8;            //StreamTableSizeInfo

        private readonly MemoryChunk chunk;

        internal BigMsfHdr(in MemoryChunk chunk)
        {
            this.chunk = chunk;
            StreamTableSizeInfo = new SI_PERSIST(chunk.Slice(StreamTableSizeInfoOffset));
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.BigMsfHdr, FixedStructSize + (PagesOfStreamTablePageList.Length * sizeof(int)));

        int IViewable.NumChildren() => 6;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteNullPaddedUtf8Field("szMagic", MagicOffset, BigHdrMagic, 32); //It's not null padded, it's just exactly 32 bytes
                    break;

                case 1:
                    structWriter.WriteField("cbPg", PageSizeOffset, PageSize);
                    break;

                case 2:
                    structWriter.WriteField("pnFpm", FpmPageNoOffset, FpmPageNo);
                    break;

                case 3:
                    structWriter.WriteField("pnMac", NumPagesOffset, NumPages);
                    break;

                case 4:
                    structWriter.WriteStructField("siSt", StreamTableSizeInfo);
                    break;

                case 5:
                    structWriter.WriteField("mpspnpnSt", PagesOfStreamTablePageListOffset, PagesOfStreamTablePageList);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
