using System;
using System.Diagnostics;
using ClrDebug;
using PESpy.View;

namespace PESpy.PDB
{
    //There's quite a lot of section contribs in each PDB, so this is a raw struct so that we can just peek it with NativeSpan
    [DebuggerDisplay("isect = {isect}, off = 0x{off.ToString(\"X\"),nq}, cb = {cb}, imod = {imod}")]
    public struct SC40 : ISC40, IViewable
    {
        private const int isectOffset = 0;
        private const int padding1Offset = 2;
        private const int offOffset = 4;
        private const int cbOffset = 8;
        private const int dwCharacteristicsOffset = 12;
        private const int imodOffset = 16;
        private const int padding2Offset = 18;

        //SC40
        public ISECT isect;
        public ushort padding1;
        public int off;
        public int cb;
        public IMAGE_SCN dwCharacteristics;
        public IMOD imod; //I believe this value is 0 based
        public ushort padding2;

        internal const int StructSize =
            sizeof(ushort) + //isect
            sizeof(ushort) + //padding1
            sizeof(int) + //off
            sizeof(int) + //cb
            sizeof(int) + //dwCharacteristics
            sizeof(ushort) + //imod
            sizeof(ushort); //padding2

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.SC40, this, ViewKind.SC40, StructSize);

        int IViewable.NumChildren() => 7;

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

                default:
                    throw new IndexOutOfRangeException();
            }
        }

        #region ISC40

        ISECT ISC40.isect => isect;

        int ISC40.off => off;

        int ISC40.cb => cb;

        IMAGE_SCN ISC40.dwCharacteristics => dwCharacteristics;

        IMOD ISC40.imod => imod;

        #endregion
    }
}
