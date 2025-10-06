using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the MSF_HDR structure used in PDB v2 files.
    /// </summary>
    public readonly partial struct MsfHdr : IValue, IViewable
    {
        internal const string HdrMagic = "Microsoft C/C++ program database 2.00\r\n\u001aJG\0\0";

        //This was present in NT 4 but I haven't found anywhere it's used yet
        internal const string HdrMagic2 = "Microsoft C/C++ program database 4.00\r\n\u001aJG\0\0";

        //szMagic
        public FixedAnsiString Magic => chunk.PeekAnsiFixedLength(0, 44);

        public int PageSize => chunk.PeekInt32(44); //cbPg

        public PN FpmPageNo => chunk.PeekUInt16(48); //V2 uses 16-bit

        public ushort NumPages => chunk.PeekUInt16(50); //pnMac

        public SI_PERSIST StreamTableSizeInfo { get; } //siSt

        public int Offset => chunk.AbsoluteOffset;

        //Unlike BIGMSF_HDR where mpspnpnSt lists the location of the stream table pages, in MSF_HDR mpspnpnSt lists
        //the stream table pages immediately; there is no indirection
        public NativeSpan<ushort> StreamTablePageList => chunk.PeekNativeSpan<ushort>(60, SI.DivideUp(StreamTableSizeInfo.ByteCount, PageSize));

        internal const int FixedStructSize =
            44 + //Magic
            sizeof(int) + //pageSize
            sizeof(short) + //FpmPageNo
            sizeof(short) + //NumPages
            SI_PERSIST.StructSize;

        internal int StructSize =>
            FixedStructSize +
            (StreamTablePageList.Length * sizeof(short));

        private readonly MemoryChunk chunk;

        internal MsfHdr(in MemoryChunk chunk)
        {
            this.chunk = chunk;
            StreamTableSizeInfo = new SI_PERSIST(chunk.Slice(52));
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.MSF_HDR, this, ViewKind.MsfHdr, StructSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteNullPaddedUTF8Field("szMagic", HdrMagic, 44); //It's not null padded, it's just exactly 44 bytes
            s.WriteField("cbPg", PageSize);
            s.WriteField("pnFpm", (ushort) FpmPageNo);
            s.WriteField("pnMac", NumPages);
            s.WriteStructField("siSt", StreamTableSizeInfo);
            s.WriteField("mpspnpnSt", StreamTablePageList);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }
    }
}
