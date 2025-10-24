using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy.PDB
{
    public readonly struct ECInfo : IViewableValue
    {
        private const int niSrcFileOffset = 0;
        private const int niPdbFileOffset = 4;

        //These name indices point into the Edit and Continue Name Table info included in the DBI

        public int niSrcFile => chunk.PeekInt32(niSrcFileOffset);

        public int niPdbFile => chunk.PeekInt32(niPdbFileOffset);

        public int Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(int) + //niSrcFile
            sizeof(int);  //niPdbFile

        private readonly MemoryChunk chunk;

        internal ECInfo(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.ECInfo, this, ViewKind.ECInfo, StructSize);

        int IViewable.NumChildren() => 2;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(niSrcFile), niSrcFileOffset, niSrcFile);
                    break;

                case 1:
                    structWriter.WriteField(nameof(niPdbFile), niPdbFileOffset, niPdbFile);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
