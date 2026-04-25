using System;
using PESpy.View;

namespace PESpy
{
    /// <summary>
    /// Encapsulates the data that is passed to the <see cref="WellKnownExceptionHandlerKind.__GSHandlerCheck_EH4"/> exception handler.
    /// </summary>
    public readonly struct FuncInfo4AndGsHandlerData : IValue
    {
        public RVA<FuncInfo4> FuncInfo { get; }

        public GsHandlerData GsHandlerData { get; }

        public int Offset { get; }

        internal unsafe FuncInfo4AndGsHandlerData(in MemoryChunk chunk, int functionAddress)
        {
            Offset = chunk.AbsoluteOffset;

            //Value is an RVA to the FuncInfo. FuncInfo struct may or may not be right after its RVA
            var rva = chunk.PeekInt32(0);

            var peFile = chunk.PEFile();

            if (peFile.TryGetValueChunkFromSection(rva, out var infoChunk))
                FuncInfo = new RVA<FuncInfo4>(rva, infoChunk.AbsoluteOffset, new FuncInfo4(infoChunk, functionAddress));
            else
                FuncInfo = new RVA<FuncInfo4>(rva);

            GsHandlerData = new GsHandlerData(chunk.AbsoluteOffset + sizeof(int), chunk.Pointer + sizeof(int));
        }

        internal void WriteInline(ref EagerStructWriter s)
        {
            s.WriteInline(FuncInfo, ViewKind.FuncInfo4Rva);
            s.WriteInline(GsHandlerData);
        }
    }
}
