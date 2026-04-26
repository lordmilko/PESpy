using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy
{
    [DebuggerDisplay("Type = {Type}, Rva = {Rva}, RvaTarget = {RvaTarget}, Extra = {Extra}")]
    public readonly struct XFixupData : IValue, IViewable
    {
        private const int TypeOffset = 0;
        private const int ExtraOffset = 2;
        private const int RvaOffset = 4;
        private const int RvaTargetOffset = 8;

        //PEAnatomist thinks that these types correspond with the IMAGE_REL_* type used in ImageRelocation. So if the IMAGE_FILE_MACHINE
        //is I386, use ImageRelI386
        public short Type => chunk.PeekInt16(TypeOffset);

        public short Extra => chunk.PeekInt16(ExtraOffset);

        public int Rva => chunk.PeekInt32(RvaOffset);

        public int RvaTarget => chunk.PeekInt32(RvaTargetOffset);

        public int Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(short) +
            sizeof(short) +
            sizeof(int) +
            sizeof(int);

        private readonly MemoryChunk chunk;

        internal XFixupData(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.XFixupData, StructSize);

        int IViewable.NumChildren() => 4;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField("wType", TypeOffset, Type);
                    break;

                case 1:
                    structWriter.WriteField("wExtra", ExtraOffset, Extra);
                    break;

                case 2:
                    structWriter.WriteField("rva", RvaOffset, Rva);
                    break;

                case 3:
                    structWriter.WriteField("rvaTarget", RvaTargetOffset, RvaTarget);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
