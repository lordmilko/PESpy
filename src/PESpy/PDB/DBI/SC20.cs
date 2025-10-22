using System;
using PESpy.View;

namespace PESpy.PDB
{
    //Seems to be used with NT 4 MODI
    public struct SC20 : IViewable
    {
        private const int isectOffset = 0;
        private const int padding1Offset = 2;
        private const int offOffset = 4;
        private const int cbOffset = 8;
        private const int imodOffset = 12;

        public ISECT isect;
        public ushort padding1;
        public int off;
        public int cb;
        public IMOD imod;

        internal const int StructSize =
            sizeof(ushort) + //isect
            sizeof(ushort) + //padding1
            sizeof(int) + //off
            sizeof(int) + //cb
            sizeof(ushort) + //imod
            sizeof(ushort); //Padding

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.SC20, this, ViewKind.SC20, StructSize);

        int IViewable.NumChildren() => 6;

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
                    structWriter.WriteField(nameof(imod), imodOffset, imod);
                    break;

                case 5:
                    structWriter.WriteByteBlob(imodOffset + 2, sizeof(short));
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
