using System;
using System.Diagnostics;
using ClrDebug;
using PESpy.View;

namespace PESpy.PDB
{
    //DBISCImpv2 seems to be used when we have a Mini PDB (/DEBUG:FASTLINK)
    [DebuggerDisplay("isect = {isect}, off = 0x{off.ToString(\"X\"),nq}, cb = {cb}, dwCharacteristics = {dwCharacteristics}, imod = {imod}")]
    public struct SC2 : ISC40, IViewable
    {
        private const int isectOffset = 0;
        private const int padding1Offset = 2;
        private const int offOffset = 4;
        private const int cbOffset = 8;
        private const int dwCharacteristicsOffset = 12;
        private const int imodOffset = 16;
        private const int padding2Offset = 18;
        private const int dwDataCrcOffset = 20;
        private const int dwRelocCrcOffset = 24;
        private const int isectCoffOffset = 28;

        //SC40
        public ISECT isect;
        public ushort padding1;
        public int off;
        public int cb;
        public IMAGE_SCN dwCharacteristics;
        public IMOD imod; //I believe this value is 0 based
        public ushort padding2;

        //SC
        public uint dwDataCrc;
        public uint dwRelocCrc;

        //SC2
        public int isectCoff;

        internal const int StructSize =
            sizeof(ushort) + //isect
            sizeof(ushort) + //padding1
            sizeof(int) + //off
            sizeof(int) + //cb
            sizeof(int) + //dwCharacteristics
            sizeof(ushort) + //imod
            sizeof(ushort) + //padding2
            sizeof(int) + //dwDataCrc
            sizeof(int) + //dwRelocCrc
            sizeof(int); //isectCoff

        public static unsafe implicit operator SC40(SC2 value) => *(SC40*) &value;
        public static unsafe implicit operator SC(SC2 value) => *(SC*) &value;

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(this, ViewKind.SC2, StructSize);

        int IViewable.NumChildren() => 10;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(isect), isectOffset, isect);
                    break;

                case 1:
                    structWriter.WriteField(nameof(padding1), padding1Offset, padding1);
                    break;

                case 2:
                    structWriter.WriteField(nameof(off), offOffset, off);
                    break;

                case 3:
                    structWriter.WriteField(nameof(cb), cbOffset, cb);
                    break;

                case 4:
                    structWriter.WriteField(nameof(dwCharacteristics), dwCharacteristicsOffset, dwCharacteristics, sizeof(int));
                    break;

                case 5:
                    structWriter.WriteField(nameof(imod), imodOffset, imod);
                    break;

                case 6:
                    structWriter.WriteField(nameof(padding2), padding2Offset, padding2);
                    break;

                case 7:
                    structWriter.WriteField(nameof(dwDataCrc), dwDataCrcOffset, dwDataCrc);
                    break;

                case 8:
                    structWriter.WriteField(nameof(dwRelocCrc), dwRelocCrcOffset, dwRelocCrc);
                    break;

                case 9:
                    structWriter.WriteField(nameof(isectCoff), isectCoffOffset, isectCoff);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }

        #region ISC20 / ISC40

        ISECT ISC20.isect => isect;

        int ISC20.off => off;

        int ISC20.cb => cb;

        IMAGE_SCN ISC40.dwCharacteristics => dwCharacteristics;

        IMOD ISC20.imod => imod;

        #endregion
    }
}
