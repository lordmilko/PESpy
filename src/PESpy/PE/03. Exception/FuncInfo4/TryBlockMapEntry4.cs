namespace PESpy
{
    [Source(SourceKind.ehdata4_export_h)]
    public readonly struct TryBlockMapEntry4
    {
        /// <summary>
        /// Lowest state index of try
        /// </summary>
        public int tryLow { get; }

        /// <summary>
        /// Highest state index of try
        /// </summary>
        public int tryHigh { get; }

        /// <summary>
        /// Highest state index of any associated catch
        /// </summary>
        public int catchHigh { get; }

        /// <summary>
        /// Image relative offset of list of handlers for this try
        /// </summary>
        public RVA<HandlerMap4> dispHandlerArray { get; }

        internal unsafe TryBlockMapEntry4(PEFile peFile, ref byte* pData, int functionAddress)
        {
            tryLow = (int) FuncInfo4.ReadUnsigned(ref pData);
            tryHigh = (int) FuncInfo4.ReadUnsigned(ref pData);
            catchHigh = (int) FuncInfo4.ReadUnsigned(ref pData);

            var dispHandlerArray = FuncInfo4.ReadInt(ref pData);

            if (peFile.TryGetValueChunkFromSection(dispHandlerArray, out var valueChunk))
            {
                var map = new HandlerMap4(valueChunk, functionAddress);

                this.dispHandlerArray = new RVA<HandlerMap4>(
                    dispHandlerArray,
                    map.Offset,
                    map
                );
            }
            else
                this.dispHandlerArray = new RVA<HandlerMap4>(dispHandlerArray);
        }
    }
}
