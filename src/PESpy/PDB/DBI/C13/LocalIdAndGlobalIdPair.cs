using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    public readonly unsafe struct LocalIdAndGlobalIdPair : IViewableValue
    {
        /// <summary>
        /// local id inside the compile time PDB scope. 0 based
        /// </summary>
        public TypOrEnumType localId => new TypOrEnumType(chunk.Pointer, (CV_ItemId) chunk.PeekInt32(0));

        /// <summary>
        /// global id inside the link time PDB scope, if scope are different.
        /// </summary>
        public TypOrEnumType globalId => new TypOrEnumType(chunk.Pointer, (CV_ItemId) chunk.PeekInt32(4));

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
            throw new System.NotImplementedException();

        int IViewable.NumChildren() => throw new System.NotImplementedException();

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            throw new System.NotImplementedException();
        }
    }
}
