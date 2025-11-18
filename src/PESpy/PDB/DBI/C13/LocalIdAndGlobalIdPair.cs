using System;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    public readonly unsafe struct LocalIdAndGlobalIdPair : IViewableValue
    {
        private const int localIdOffset = 0;
        private const int globalIdOffset = 4;

        /// <summary>
        /// local id inside the compile time PDB scope. 0 based
        /// </summary>
        public TypOrEnumType localId => new TypOrEnumType(chunk.Pointer, (CV_ItemId) chunk.PeekInt32(localIdOffset));

        /// <summary>
        /// global id inside the link time PDB scope, if scope are different.
        /// </summary>
        public TypOrEnumType globalId => new TypOrEnumType(chunk.Pointer, (CV_ItemId) chunk.PeekInt32(globalIdOffset));

        public int Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(int) + //localId
            sizeof(int); //globalId

        private readonly MemoryChunk chunk;

        internal LocalIdAndGlobalIdPair(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.LocalIdAndGlobalIdPair, this, ViewKind.LocalIdAndGlobalIdPair, StructSize);

        int IViewable.NumChildren() => 2;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(localId), localIdOffset, (CV_ItemId) localId);
                    break;

                case 1:
                    structWriter.WriteField(nameof(globalId), globalIdOffset, (CV_ItemId) globalId);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
