using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    [DebuggerDisplay("{offObjectFilePath}")]
    public struct PdbIdScope : IViewable
    {
        private const int offObjectFilePathOffset = 0;

        public CV_off32_t offObjectFilePath;

        internal const int StructSize = sizeof(int);

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(this, ViewKind.PdbIdScope, StructSize);

        int IViewable.NumChildren() => 1;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            if (index == 0)
                structWriter.WriteField(nameof(offObjectFilePath), offObjectFilePathOffset, offObjectFilePath);
            else
                throw new IndexOutOfRangeException();
        }
    }
}
