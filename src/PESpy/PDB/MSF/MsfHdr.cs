using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the MSF_HDR structure used in PDB v2 files.
    /// </summary>
    [Source(SourceKind.msf_cpp)]
    public readonly partial struct MsfHdr : IValue, IViewable
    {
        internal const string HdrMagic = "Microsoft C/C++ program database 2.00\r\n\u001aJG\0\0";

        //This was present in NT 4 but I haven't found anywhere it's used yet
        internal const string HdrMagic2 = "Microsoft C/C++ program database 4.00\r\n\u001aJG\0\0";

        private const int MagicOffset = 0;
        private const int PageSizeOffset = 44;
        private const int FpmPageNoOffset = 48;
        private const int NumPagesOffset = 50;
        private const int StreamTableSizeInfoOffset = 52;
        private const int StreamTablePageListOffset = 60;

        //szMagic
        public FixedAnsiString Magic => chunk.PeekAnsiFixedLength(MagicOffset, 44);

        public int PageSize => chunk.PeekInt32(PageSizeOffset); //cbPg

        public PN FpmPageNo => chunk.PeekUInt16(FpmPageNoOffset); //V2 uses 16-bit

        public ushort NumPages => chunk.PeekUInt16(NumPagesOffset); //pnMac

        public SI_PERSIST StreamTableSizeInfo { get; } //siSt

        public long Offset => chunk.AbsoluteOffset;

        //Unlike BIGMSF_HDR where mpspnpnSt lists the location of the stream table pages, in MSF_HDR mpspnpnSt lists
        //the stream table pages immediately; there is no indirection
        public NativeSpan<ushort> StreamTablePageList => chunk.PeekNativeSpan<ushort>(StreamTablePageListOffset, SI.DivideUp(StreamTableSizeInfo.ByteCount, PageSize));

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
            StreamTableSizeInfo = new SI_PERSIST(chunk.Slice(StreamTableSizeInfoOffset));
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.MsfHdr, StructSize);

        int IViewable.NumChildren() => 6;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteNullPaddedUtf8Field("szMagic", MagicOffset, HdrMagic, 44); //It's not null padded, it's just exactly 44 bytes
                    break;

                case 1:
                    structWriter.WriteField("cbPg", PageSizeOffset, PageSize);
                    break;

                case 2:
                    structWriter.WriteField("pnFpm", FpmPageNoOffset, (ushort) (uint) FpmPageNo);
                    break;

                case 3:
                    structWriter.WriteField("pnMac", NumPagesOffset, NumPages);
                    break;

                case 4:
                    structWriter.WriteStructField("siSt", StreamTableSizeInfo);
                    break;

                case 5:
                    structWriter.WriteField("mpspnpnSt", StreamTablePageListOffset, StreamTablePageList);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
