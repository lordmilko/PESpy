using System;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the BIGMSF_HDR structure used in PDB v7 files.
    /// </summary>
    public readonly partial struct BigMsfHdr : IValue, IViewable
    {
        internal const string BigHdrMagic = "Microsoft C/C++ MSF 7.00\r\n\u001aDS\0\0\0";

        //szMagic
        public FixedAnsiString Magic => chunk.PeekAnsiFixedLength(0, 32);

        /// <summary>
        /// Gets the size of each page in the PDB.<para/>
        /// PDBs can only be as big as there are bits in the free page map to record each page as being available or not.
        /// Thus, the larger the page size, the more data you can store in your PDB.
        /// </summary>
        public int PageSize => chunk.PeekInt32(32); //cbPg

        /// <summary>
        /// Gets which of the two FPM pages is currently the active one.
        /// </summary>
        public PN FpmPageNo => chunk.PeekInt32(36); //pnFpm

        /// <summary>
        /// Gets the total number of pages contained in this PDB (including the master page (0).
        /// </summary>
        public int NumPages => chunk.PeekInt32(40); //pnMac

        /// <summary>
        /// Lists the size of the stream table that describes the streams that exist in the PDB
        /// </summary>
        public SI_PERSIST StreamTableSizeInfo { get; } //siSt

        /// <summary>
        /// Lists the page numbers of a stream that contains an array of page numbers that the stream table
        /// is distributed across. BIGMSF_HDR does not list this member explicitly; instead, it lists an array
        /// mpspnpnSt that can hold 19 members, the first member of which is this value.
        /// </summary>
        /// <remarks>
        /// Suppose that the Stream Table is 524,288 bytes and we have 1024 byte pages. The Stream table itself spans 512 pages.
        /// A single PN is 4 bytes, so merely recording the existance of these 512 pages requires 2048 bytes. Which means that
        /// the list of PNs will itself span two pages
        /// </remarks>
        public Span<PN> PagesOfStreamTablePageList => chunk.PeekSpan<PN>(52, SI.DivideUp((SI.DivideUp(StreamTableSizeInfo.ByteCount, PageSize) * 4), PageSize)); //Normally there will be a single page that lists the location of the stream table. However, suppose we have 1024 byte pages. We can store 256 32-bit page numbers in 1 page. , and the stream table is so large that

        public int Offset => chunk.AbsoluteOffset;

        private readonly MemoryChunk chunk;

        internal BigMsfHdr(in MemoryChunk chunk)
        {
            this.chunk = chunk;
            StreamTableSizeInfo = new SI_PERSIST(chunk.Slice(44));
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct("BIGMSF_HDR", this, ViewKind.BigMsfHdr);

            s.WriteNullPaddedUTF8Field("szMagic", BigHdrMagic, 32); //It's not null padded, it's just exactly 32 bytes
            s.WriteField("cbPg", PageSize);
            s.WriteField("pnFpm", FpmPageNo);
            s.WriteField("pnMac", NumPages);
            s.WriteStructField("siSt", StreamTableSizeInfo);
            s.WriteField("mpspnpnSt", PagesOfStreamTablePageList.ToArray()); 
        }
    }
}
