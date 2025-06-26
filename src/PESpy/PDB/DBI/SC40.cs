using System.Diagnostics;
using ClrDebug;
using PESpy.View;

namespace PESpy.PDB
{
    //There's quite a lot of section contribs in each PDB, so this is a raw struct so that we can just peek it with NativeSpan
    [DebuggerDisplay("isect = {isect}, off = 0x{off.ToString(\"X\"),nq}, cb = {cb}, imod = {imod}")]
    public struct SC40 : ISC40, IViewable
    {
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
            writer.NewUnmanagedStruct(nameof(SC40), this, ViewKind.SC40, StructSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(isect), isect);
            s.WriteField(nameof(padding1), padding1);
            s.WriteField(nameof(off), off);
            s.WriteField(nameof(cb), cb);
            s.WriteField(nameof(dwCharacteristics), dwCharacteristics, sizeof(int));
            s.WriteField(nameof(imod), imod);
            s.WriteField(nameof(padding2), padding2);

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
