using System;
using PESpy.View;

namespace PESpy
{
    //ANON_OBJECT_HEADER_BIGOBJ
    public class AnonObjectHeaderBigObj : AnonObjectHeaderV2
    {
        public int NumberOfSections => chunk.PeekInt32(AnonObjectHeaderV2.StructSize);

        public int PointerToSymbolTable => chunk.PeekInt32(AnonObjectHeaderV2.StructSize + 4);

        public int NumberOfSymbols => chunk.PeekInt32(AnonObjectHeaderV2.StructSize + 8);

        internal new const int StructSize =
            AnonObjectHeaderV2.StructSize +
            sizeof(int) + //NumberOfSections
            sizeof(int) + //PointerToSymbolTable
            sizeof(int); //NumberOfSymbols

        internal AnonObjectHeaderBigObj(in MemoryChunk chunk) : base(chunk)
        {
        }

        protected override IView? WriteStruct(ViewWriter writer)
        {
            throw new System.NotImplementedException();
        }

        protected override int NumChildren => base.NumChildren;

        protected override void WriteChild(int index, ref StructWriter structWriter)
        {
            throw new NotImplementedException();
        }
    }
}
