namespace PESpy
{
    [Source(SourceKind.ehdata4_export_h)]
    public readonly struct SepIPtoStateMapEntry4
    {
        /// <summary>
        /// Start address of the function contribution
        /// </summary>
        public int addrStartRVA { get; }

        /// <summary>
        /// RVA to IP map corresponding to this function contribution
        /// </summary>
        public RVA<IPtoStateMap4> dispOfIPMap { get; }

        internal unsafe SepIPtoStateMapEntry4(PEFile peFile, ref byte* pData, int functionAddress)
        {
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
    }
}
