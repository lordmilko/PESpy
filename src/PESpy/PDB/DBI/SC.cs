using ClrDebug;
using PESpy.View;

namespace PESpy.PDB
{
    public struct SC : ISC40, IViewable
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

        internal new const int StructSize =
            sizeof(ushort) + //isect
            sizeof(ushort) + //padding1
            sizeof(int) + //off
            sizeof(int) + //cb
            sizeof(int) + //dwCharacteristics
            sizeof(ushort) + //imod
            sizeof(ushort) + //padding2
            sizeof(int) + //dwDataCrc
            sizeof(int); //dwRelocCrc

        public static unsafe implicit operator SC40(SC value) => *(SC40*) &value;

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateUnmanagedStruct(nameof(SC), ViewKind.SC);

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
