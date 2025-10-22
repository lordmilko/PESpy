using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy
{
    [DebuggerDisplay("Type = {Type}, Rva = {Rva}, RvaTarget = {RvaTarget}, Extra = {Extra}")]
    public readonly struct XFixupData : IValue, IViewable
    {
        //PEAnatomist thinks that these types correspond with the IMAGE_REL_* type used in ImageRelocation. So if the IMAGE_FILE_MACHINE
        //is I386, use ImageRelI386
        public short Type => chunk.PeekInt16(0);

        public short Extra => chunk.PeekInt16(2);

        public int Rva => chunk.PeekInt32(4);

        public int RvaTarget => chunk.PeekInt32(8);

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
            writer.NewStruct(Strings.XFIXUP_DATA, this, ViewKind.XFixupData, StructSize);

        int IViewable.NumChildren() => 4;

        void IViewable.WriteChild(int index, ViewWriter viewWriter)
        {
            switch (index)
            {
                case 0:
                    viewWriter.WriteField("wType", Type);
                    break;

                case 1:
                    viewWriter.WriteField("wExtra", Extra);
                    break;

                case 2:
                    viewWriter.WriteField("rva", Rva);
                    break;

                case 3:
                    viewWriter.WriteField("rvaTarget", RvaTarget);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
