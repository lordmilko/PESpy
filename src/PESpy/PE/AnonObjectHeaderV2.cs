using System;
using PESpy.View;

namespace PESpy
{
    //ANON_OBJECT_HEADER_V2
    public class AnonObjectHeaderV2 : AnonObjectHeader
    {
        /// <summary>
        /// 0x1 -> contains metadata
        /// </summary>
        public int Flags => chunk.PeekInt32(AnonObjectHeader.StructSize);

        /// <summary>
        /// Size of CLR metadata
        /// </summary>
        public int MetaDataSize => chunk.PeekInt32(AnonObjectHeader.StructSize + 4);

        /// <summary>
        /// Offset of CLR metadata
        /// </summary>
        public int MetaDataOffset => chunk.PeekInt32(AnonObjectHeader.StructSize + 8);

        internal new const int StructSize =
            AnonObjectHeader.StructSize +
            sizeof(int) + //Flags
            sizeof(int) + //MetaDataSize
            sizeof(int); //MetaDataOffset

        internal AnonObjectHeaderV2(in MemoryChunk chunk) : base(in chunk)
        {
        }

        protected override IView? WriteStruct(ViewWriter writer)
        {
            throw new System.NotImplementedException();
        }

        protected override IView[] GetChildren(IView parent, ViewWriter viewWriter)
        {
            throw new System.NotImplementedException();
        }
    }
}
