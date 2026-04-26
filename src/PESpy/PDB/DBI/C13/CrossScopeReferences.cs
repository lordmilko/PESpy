using System;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    public readonly struct CrossScopeReferences : IViewableValue
    {
        private const int externalScopeOffset = 0;
        private const int countOfCrossReferencesOffset = 4;
        private const int referenceIdsOffset = 8;

        /// <summary>
        /// Module of definition Scope.
        /// </summary>
        public PdbIdScope externalScope => chunk.PeekUnmanaged<PdbIdScope>(externalScopeOffset);

        /// <summary>
        /// Count of following array.
        /// </summary>
        public int countOfCrossReferences => chunk.PeekInt32(countOfCrossReferencesOffset);

        /// <summary>
        /// CV_ItemId in another compilation unit.
        /// </summary>
        public TypOrEnumTypeList<CV_ItemId> referenceIds => new TypOrEnumTypeList<CV_ItemId>(chunk.PeekNativeSpan<CV_ItemId>(referenceIdsOffset, countOfCrossReferences));

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
            writer.NewStruct(this, ViewKind.CrossScopeReferences, FixedStructSize + (countOfCrossReferences * sizeof(int)));

        int IViewable.NumChildren() => 3;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteStructField(nameof(externalScope), externalScopeOffset, externalScope);
                    break;

                case 1:
                    structWriter.WriteField(nameof(countOfCrossReferences), countOfCrossReferencesOffset, countOfCrossReferences); ;
                    break;

                case 2:
                    var referenceIds = chunk.PeekNativeSpan<CV_ItemId>(referenceIdsOffset, countOfCrossReferences);
                    structWriter.WriteField(nameof(referenceIds), referenceIdsOffset, referenceIds);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
