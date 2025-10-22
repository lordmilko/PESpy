using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy.PDB
{
    [DebuggerDisplay("off = {off}, cb = {cb}")]
    public struct OffCb : IViewable
    {
        private const int offOffset = 0;
        private const int cbOffset = 4;

        public int off;
        public int cb;

        internal const int StructSize =
            sizeof(int) + //off
            sizeof(int);  //cb

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.OffCb, this, ViewKind.OffCb, StructSize);

        int IViewable.NumChildren() => 2;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(off), offOffset, off);
                    break;

                case 1:
                    structWriter.WriteField(nameof(cb), cbOffset, cb);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
