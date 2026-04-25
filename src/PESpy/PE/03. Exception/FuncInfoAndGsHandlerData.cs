using PESpy.View;

namespace PESpy
{
    /// <summary>
    /// Encapsulates the data that is passed to the <see cref="WellKnownExceptionHandlerKind.__GSHandlerCheck_EH"/> exception handler.
    /// </summary>
    public readonly struct FuncInfoAndGsHandlerData : IValue //Not IViewable because this is just a transparent struct
    {
        public RVA<FuncInfo> FuncInfo { get; }

        public GsHandlerData GsHandlerData { get; }

        public int Offset { get; }

        internal unsafe FuncInfoAndGsHandlerData(in MemoryChunk chunk)
        {
            Offset = chunk.AbsoluteOffset;

            //Value is an RVA to the FuncInfo. FuncInfo struct may or may not be right after its RVA
            var rva = chunk.PeekInt32(0);

            var peFile = chunk.PEFile();

            if (peFile.TryGetValueChunkFromSection(rva, out var infoChunk))
                FuncInfo = new RVA<FuncInfo>(rva, infoChunk.AbsoluteOffset, new FuncInfo(infoChunk));
            else
                FuncInfo = new RVA<FuncInfo>(rva);

            GsHandlerData = new GsHandlerData(chunk.AbsoluteOffset + sizeof(int), chunk.Pointer + sizeof(int));
        }

        internal void WriteInline(ref EagerStructWriter s)
        {
            s.WriteInline(FuncInfo, ViewKind.FuncInfoRva);
            s.WriteInline(GsHandlerData);
        }
    }
}
