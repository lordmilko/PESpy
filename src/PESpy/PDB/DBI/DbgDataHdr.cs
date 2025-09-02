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

        public SN FPO            => GetSN((ushort) DBGTYPE.dbgtypeFPO); //0
        public SN Exception      => GetSN((ushort) DBGTYPE.dbgtypeException); //1
        public SN Fixup          => GetSN((ushort) DBGTYPE.dbgtypeFixup); //2
        public SN OmapToSrc      => GetSN((ushort) DBGTYPE.dbgtypeOmapToSrc); //3
        public SN OmapFromSrc    => GetSN((ushort) DBGTYPE.dbgtypeOmapFromSrc); //4
        public SN SectionHdr     => GetSN((ushort) DBGTYPE.dbgtypeSectionHdr); //5
        public SN TokenRidMap    => GetSN((ushort) DBGTYPE.dbgtypeTokenRidMap); //6
        public SN XData          => GetSN((ushort) DBGTYPE.dbgtypeXdata); //7
        public SN PData          => GetSN((ushort) DBGTYPE.dbgtypePdata); //8
        public SN NewFPO         => GetSN((ushort) DBGTYPE.dbgtypeNewFPO); //9
        public SN SectionHdrOrig => GetSN((ushort) DBGTYPE.dbgtypeSectionHdrOrig); //10
        //There is a new debug type, "11" which appears to be XFG data, consisting of structs of type XFGTYPEHASHINFODATA
        public SN Max            => GetSN((ushort) DBGTYPE.dbgtypeMax); //11 - this is part of it, the total size is 24 bytes

        public int Offset => chunk.AbsoluteOffset;

        private readonly MemoryChunk chunk;
        private int maxIndex;

        internal DbgDataHdr(in MemoryChunk chunk, int length)
        {
            this.chunk = chunk;

            //Older PDB versions may not have all fields
            maxIndex = length / sizeof(short);
        }

        private SN GetSN(ushort type)
        {
            if (type < maxIndex)
                return chunk.PeekUInt16(type * 2);

            return SN.Nil;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.DbgDataHdr, this, ViewKind.DbgDataHdr, maxIndex * sizeof(short));

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            //This is supposed to be an array

            for (var i = 0; i < maxIndex; i++)
            {
                switch ((DBGTYPE) i)
                {
                    case DBGTYPE.dbgtypeFPO:
                        s.WriteField(nameof(FPO), FPO);
                        break;

                    case DBGTYPE.dbgtypeException:
                        s.WriteField(nameof(Exception), Exception);
                        break;

                    case DBGTYPE.dbgtypeFixup:
                        s.WriteField(nameof(Fixup), Fixup);
                        break;

                    case DBGTYPE.dbgtypeOmapToSrc:
                        s.WriteField(nameof(OmapToSrc), OmapToSrc);
                        break;

                    case DBGTYPE.dbgtypeOmapFromSrc:
                        s.WriteField(nameof(OmapFromSrc), OmapFromSrc);
                        break;

                    case DBGTYPE.dbgtypeSectionHdr:
                        s.WriteField(nameof(SectionHdr), SectionHdr);
                        break;

                    case DBGTYPE.dbgtypeTokenRidMap:
                        s.WriteField(nameof(TokenRidMap), TokenRidMap);
                        break;

                    case DBGTYPE.dbgtypeXdata:
                        s.WriteField(nameof(XData), XData);
                        break;

                    case DBGTYPE.dbgtypePdata:
                        s.WriteField(nameof(PData), PData);
                        break;

                    case DBGTYPE.dbgtypeNewFPO:
                        s.WriteField(nameof(NewFPO), NewFPO);
                        break;

                    case DBGTYPE.dbgtypeSectionHdrOrig:
                        s.WriteField(nameof(SectionHdrOrig), SectionHdrOrig);
                        break;

                    case DBGTYPE.dbgtypeMax:
                        s.WriteField(nameof(Max), Max); //This is part of it, the total size is 24 bytes
                        break;
                }
            }

            return s.ToArray();
        }
    }
}
