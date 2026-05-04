using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy
{
    /// <summary>
    /// Represents the data contained in the VCFeature debug directory.<para/>
    /// This type does not have a well-known native struct declaration.
    /// </summary>
    public class VCFeature : IValue, IViewable //This will always be boxed, so no point being a struct
    {
        private const int PreVC11Offset = 0;
        private const int C_CPPOffset = 4;
        private const int GSOffset = 8;
        private const int SDLOffset = 12;
        private const int GuardNOffset = 16;

        //Field names are based on the names listed with dumpbin

        public int PreVC11 => chunk.PeekInt32(PreVC11Offset);

        public int C_CPP => chunk.PeekInt32(C_CPPOffset);

        public int GS => chunk.PeekInt32(GSOffset);

        public int SDL => chunk.PeekInt32(SDLOffset);

        public int GuardN => chunk.PeekInt32(GuardNOffset);

        public long Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(int) + //PreVC11
            sizeof(int) + //C_CPP
            sizeof(int) + //GS
            sizeof(int) + //SDL
            sizeof(int); //GuardN

        private readonly MemoryChunk chunk;

        internal VCFeature(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.VCFeature, StructSize);

        int IViewable.NumChildren() => 5;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(PreVC11), PreVC11Offset, PreVC11);
                    break;

                case 1:
                    structWriter.WriteField(nameof(C_CPP), C_CPPOffset, C_CPP);
                    break;

                case 2:
                    structWriter.WriteField(nameof(GS), GSOffset, GS);
                    break;

                case 3:
                    structWriter.WriteField(nameof(SDL), SDLOffset, SDL);
                    break;

                case 4:
                    structWriter.WriteField(nameof(GuardN), GuardNOffset, GuardN);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
