using ClrDebug;
using PESpy.View;

namespace PESpy.PDB
{
    //DBISCImpv2 seems to be used when we have a Mini PDB (/DEBUG:FASTLINK)
    public struct SC2 : ISC40, IViewable
    {
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
            writer.NewUnmanagedStruct(Strings.SC2, this, ViewKind.SC2, StructSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            //SC40
            s.WriteField(nameof(isect), isect);
            s.WriteField(nameof(padding1), padding1);
            s.WriteField(nameof(off), off);
            s.WriteField(nameof(cb), cb);
            s.WriteField(nameof(dwCharacteristics), dwCharacteristics, sizeof(int));
            s.WriteField(nameof(imod), imod);
            s.WriteField(nameof(padding2), padding2);

            //SC
            s.WriteField(nameof(dwDataCrc), dwDataCrc);
            s.WriteField(nameof(dwRelocCrc), dwRelocCrc);

            //SC2
            s.WriteField(nameof(isectCoff), isectCoff);

            return s.ToArray();
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
