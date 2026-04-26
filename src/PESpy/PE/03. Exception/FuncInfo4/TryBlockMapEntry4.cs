using PESpy.View;

namespace PESpy
{
    [Source(SourceKind.ehdata4_export_h)]
    public readonly struct TryBlockMapEntry4 : IViewableValue
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

        public int Offset { get; }

        internal int StructSize =>
            FuncInfo4.GetLength((uint) tryLow) +
            FuncInfo4.GetLength((uint) tryHigh) +
            FuncInfo4.GetLength((uint) catchHigh) +
            sizeof(int); //dispHandlerArray

        internal unsafe TryBlockMapEntry4(int offset, PEFile peFile, ref byte* pData, int functionAddress)
        {
            Offset = offset;
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

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            var fieldOffset =
                FuncInfo4.GetLength((uint) tryLow) +
                FuncInfo4.GetLength((uint) tryHigh) +
                FuncInfo4.GetLength((uint) catchHigh);

            writer.WriteUniqueRVAField(dispHandlerArray, Offset, fieldOffset);
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.TryBlockMapEntry4, StructSize);

        int IViewable.NumChildren() =>
            throw StructWriter.GetEagerLoadOnlyException();

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            if (index != -1)
                throw StructWriter.GetEagerLoadOnlyException();

            using var s = structWriter.CreateEagerWriter();

            s.WriteField(nameof(tryLow), tryLow, FuncInfo4.GetLength((uint) tryLow));
            s.WriteField(nameof(tryHigh), tryHigh, FuncInfo4.GetLength((uint) tryHigh));
            s.WriteField(nameof(catchHigh), catchHigh, FuncInfo4.GetLength((uint) catchHigh));
            s.WriteRVAField(nameof(dispHandlerArray), dispHandlerArray);

            structWriter.EagerFields = s.ToArray();
        }
    }
}
