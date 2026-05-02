using System;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy
{
    public struct OMFTypeFlags : IViewable
    {
        public CV_SIGNATURE sig;
        public byte unused1;
        public byte unused2;
        public byte unused3;

        internal const int StructSize =
            sizeof(byte) +
            sizeof(byte) +
            sizeof(byte) +
            sizeof(byte);

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(this, ViewKind.OMFTypeFlags, StructSize);

        int IViewable.NumChildren() => 4;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(sig), 0, sig, sizeof(byte));
                    break;

                case 1:
                    structWriter.WriteField(nameof(unused1), 1, unused1);
                    break;

                case 2:
                    structWriter.WriteField(nameof(unused2), 2, unused2);
                    break;

                case 3:
                    structWriter.WriteField(nameof(unused3), 3, unused3);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
