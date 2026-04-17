using System;
using PESpy.View;

namespace PESpy
{
    public readonly struct FuncInfoAndGsHandlerData : IViewableValue
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

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //Both values are globals
            writer.WriteRVAField(FuncInfo, Offset, 0);
            writer.WriteGlobal(GsHandlerData);
        }

        IView? IViewable.WriteStruct(ViewWriter writer) => null;

        int IViewable.NumChildren() => throw new NotSupportedException();

        void IViewable.WriteChild(int index, ref StructWriter structWriter) => throw new NotSupportedException();
    }
}
