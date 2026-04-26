using System;
using PESpy.View;

namespace PESpy
{
    [Source(SourceKind.ehdata4_export_h)]
    public readonly struct SepIPtoStateMapEntry4 : IViewableValue
    {
        private const int addrStartRVAOffset = 0;
        private const int dispOfIPMapOffset = 4;

        /// <summary>
        /// Start address of the function contribution
        /// </summary>
        public int addrStartRVA { get; }

        /// <summary>
        /// RVA to IP map corresponding to this function contribution
        /// </summary>
        public RVA<IPtoStateMap4> dispOfIPMap { get; }

        public int Offset { get; }

        //Even though the values are contained in a compressed stream, Int32's are not compressed
        internal const int StructSize =
            sizeof(int) +
            sizeof(int);

        internal unsafe SepIPtoStateMapEntry4(int offset, PEFile peFile, ref byte* pData, int functionAddress)
        {
            Offset = offset;
            addrStartRVA = FuncInfo4.ReadInt(ref pData);
            var dispOfIPMap = FuncInfo4.ReadInt(ref pData);

            if (peFile.TryGetValueChunkFromSection(dispOfIPMap, out var valueChunk))
            {
                var map = new IPtoStateMap4(valueChunk, functionAddress);

                this.dispOfIPMap = new RVA<IPtoStateMap4>(
                    dispOfIPMap,
                    map.Offset,
                    map
                );
            }
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            writer.WriteUniqueRVAXRef(Offset, addrStartRVAOffset, addrStartRVA);
            writer.WriteUniqueRVAField(dispOfIPMap, Offset, dispOfIPMapOffset);
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.SepIPtoStateMapEntry4, StructSize);

        int IViewable.NumChildren() => 2;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(addrStartRVA), addrStartRVAOffset, addrStartRVA);
                    break;

                case 1:
                    structWriter.WriteRVAField(nameof(dispOfIPMap), dispOfIPMapOffset, dispOfIPMap);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
