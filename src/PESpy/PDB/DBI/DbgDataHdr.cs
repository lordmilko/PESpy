using System;
using System.Diagnostics;
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

        int IViewable.NumChildren() => maxIndex;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    WriteIndex(nameof(FPO), index, FPO, ref structWriter);
                    break;

                case 1:
                    WriteIndex(nameof(Exception), index, Exception, ref structWriter);
                    break;

                case 2:
                    WriteIndex(nameof(Fixup), index, Fixup, ref structWriter);
                    break;

                case 3:
                    WriteIndex(nameof(OmapToSrc), index, OmapToSrc, ref structWriter);
                    break;

                case 4:
                    WriteIndex(nameof(OmapFromSrc), index, OmapFromSrc, ref structWriter);
                    break;

                case 5:
                    WriteIndex(nameof(SectionHdr), index, SectionHdr, ref structWriter);
                    break;

                case 6:
                    WriteIndex(nameof(TokenRidMap), index, TokenRidMap, ref structWriter);
                    break;

                case 7:
                    WriteIndex(nameof(XData), index, XData, ref structWriter);
                    break;

                case 8:
                    WriteIndex(nameof(PData), index, PData, ref structWriter);
                    break;

                case 9:
                    WriteIndex(nameof(NewFPO), index, NewFPO, ref structWriter);
                    break;

                case 10:
                    WriteIndex(nameof(SectionHdrOrig), index, SectionHdrOrig, ref structWriter);
                    break;

                case 11:
                    WriteIndex(nameof(Max), index, Max, ref structWriter); //This is part of it, the total size is 24 bytes
                    break;

                default:
                    if (index < maxIndex)
                        structWriter.WriteByteBlob(index * 2, (maxIndex - index) * 2); //Write the rest as bytes
                    else
                        throw new IndexOutOfRangeException();

                    break;
            }
        }

        private void WriteIndex(string name, int index, SN value, ref StructWriter structWriter)
        {
            if (index >= maxIndex)
                throw new IndexOutOfRangeException();

            structWriter.WriteField(name, index * 2, value);
        }
    }
}
