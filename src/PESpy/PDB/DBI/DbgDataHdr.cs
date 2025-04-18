using System;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    public class DbgDataHdr : IValue, IViewable //May not be present
    {
        /* An array of section numbers, that you should index into using the DBGTYPE enum to see whether
         * that particular type of debug information is present. e.g. rgSnDbg[dbgtypeFPO]. Ostensibly there
         * should be at most dbgtypeMax records, however if our dbgtypeMax is out of date, there may be newer
         * records we don't know about
         *
         * DbgDataHdr stores its elements as an array, however it's easier to use if we just store each element
         * as its own member. As such, we intentionally do not use Span<SN> rgSnDbg
         */

        public SN FPO            => chunk.PeekUInt16((ushort) DBGTYPE.dbgtypeFPO * 2); //0
        public SN Exception      => chunk.PeekUInt16((ushort) DBGTYPE.dbgtypeException * 2); //1
        public SN Fixup          => chunk.PeekUInt16((ushort) DBGTYPE.dbgtypeFixup * 2); //2
        public SN OmapToSrc      => chunk.PeekUInt16((ushort) DBGTYPE.dbgtypeOmapToSrc * 2); //3
        public SN OmapFromSrc    => chunk.PeekUInt16((ushort) DBGTYPE.dbgtypeOmapFromSrc * 2); //4
        public SN SectionHdr     => chunk.PeekUInt16((ushort) DBGTYPE.dbgtypeSectionHdr * 2); //5
        public SN TokenRidMap    => chunk.PeekUInt16((ushort) DBGTYPE.dbgtypeTokenRidMap * 2); //6
        public SN XData          => chunk.PeekUInt16((ushort) DBGTYPE.dbgtypeXdata * 2); //7
        public SN PData          => chunk.PeekUInt16((ushort) DBGTYPE.dbgtypePdata * 2); //8
        public SN NewFPO         => chunk.PeekUInt16((ushort) DBGTYPE.dbgtypeNewFPO * 2); //9
        public SN SectionHdrOrig => chunk.PeekUInt16((ushort) DBGTYPE.dbgtypeSectionHdrOrig * 2); //10
        public SN Max => chunk.PeekUInt16((ushort) DBGTYPE.dbgtypeMax * 2); //12 - this is part of it, the total size is 24 bytes

        public int Offset => chunk.AbsoluteOffset;

        private readonly MemoryChunk chunk;

        internal DbgDataHdr(in MemoryChunk chunk, int length)
        {
            this.chunk = chunk;

            var numItems = length / sizeof(short);

            if (numItems < (short) DBGTYPE.dbgtypeMax)
                throw new InvalidOperationException($"Expected {nameof(DbgDataHdr)} to have at least {(short) DBGTYPE.dbgtypeMax} items, however only {numItems} were present");
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct(nameof(DbgDataHdr), this, ViewKind.DbgDataHdr);

            //This is supposed to be an array
            s.WriteField(nameof(FPO), FPO);
            s.WriteField(nameof(Exception), Exception);
            s.WriteField(nameof(Fixup), Fixup);
            s.WriteField(nameof(OmapToSrc), OmapToSrc);
            s.WriteField(nameof(OmapFromSrc), OmapFromSrc);
            s.WriteField(nameof(SectionHdr), SectionHdr);
            s.WriteField(nameof(TokenRidMap), TokenRidMap);
            s.WriteField(nameof(XData), XData);
            s.WriteField(nameof(PData), PData);
            s.WriteField(nameof(NewFPO), NewFPO);
            s.WriteField(nameof(SectionHdrOrig), SectionHdrOrig);
            s.WriteField(nameof(Max), Max); //This is part of it, the total size is 24 bytes
        }
    }
}
