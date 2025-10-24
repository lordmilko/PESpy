using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    public readonly struct CrossScopeReferences : IViewableValue
    {
        /// <summary>
        /// Module of definition Scope.
        /// </summary>
        public PdbIdScope externalScope => chunk.PeekUnmanaged<PdbIdScope>(0);

        /// <summary>
        /// Count of following array.
        /// </summary>
        public int countOfCrossReferences => chunk.PeekInt32(4);

        /// <summary>
        /// CV_ItemId in another compilation unit.
        /// </summary>
        public TypOrEnumTypeList<CV_ItemId> referenceIds => new TypOrEnumTypeList<CV_ItemId>(chunk.PeekNativeSpan<CV_ItemId>(8, countOfCrossReferences));

        public int Offset => chunk.AbsoluteOffset;

        internal const int FixedStructSize =
            sizeof(int) + //externalScope
            sizeof(int); //countOfCrossReferences

        private readonly MemoryChunk chunk;

        internal CrossScopeReferences(in MemoryChunk chunk)
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
