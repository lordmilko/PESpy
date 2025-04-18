using PESpy.View;

namespace PESpy.PDB
{
    public readonly struct BigMsfHdr : IValue, IViewable
    {
        private const string BigHdrMagic = "Microsoft C/C++ MSF 7.00\r\n\u001aDS\0\0\0";

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
        /// Lists the page number of a page that contains an array of page numbers that the stream table
        /// is distributed across. BIGMSF_HDR does not list this member explicitly; instead, it lists an array
        /// mpspnpnSt that can hold 19 members, the first member of which is this value. It doesn't seem like
        /// the remaining 18 members are used (or their usage is unclear). It seems like the purpose is to actually
        /// store the stream table inline, but it doesn't actually get stored there, it gets stored somewhere else
        /// </summary>
        /// <remarks>
        /// This is the first element of mpspnpnSt. Gets copied into siPnList.mpspnpn which is then used
        /// to read the list of pages that describe the streams that exist in the PDB
        /// </remarks>
        public PN PageOfStreamTablePageList => chunk.PeekInt32(52);

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

            //Perhaps it's possible to have more than one value in mpspnpnSt? Not sure what other name to give this
            s.WriteField("mpspnpnSt", PageOfStreamTablePageList);
        }
    }
}
