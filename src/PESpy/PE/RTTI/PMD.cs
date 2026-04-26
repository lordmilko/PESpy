using System;
using PESpy.View;

namespace PESpy
{
    /// <summary>
    /// PMD - Pointer to Member Data: generalized pointer-to-member descriptor
    /// </summary>
    public struct PMD : IViewable
    {
        private const int mdispOffset = 0;
        private const int pdispOffset = 4;
        private const int vdispOffset = 8;

        /// <summary>
        /// Offset of intended data within base
        /// </summary>
        public int mdisp;

        /// <summary>
        /// Displacement to virtual base pointer
        /// </summary>
        public int pdisp;

        /// <summary>
        /// Index within vbTable to offset of base
        /// </summary>
        public int vdisp;

        internal const int StructSize =
            sizeof(int) + //mdisp
            sizeof(int) + //pdisp
            sizeof(int); //vdisp

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(this, ViewKind.PMD, StructSize);

        int IViewable.NumChildren() => 3;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(mdisp), mdispOffset, mdisp);
                    break;

                case 1:
                    structWriter.WriteField(nameof(pdisp), pdispOffset, pdisp);
                    break;

                case 2:
                    structWriter.WriteField(nameof(vdisp), vdispOffset, vdisp);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
