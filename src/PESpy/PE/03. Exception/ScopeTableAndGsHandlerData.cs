using System;
using PESpy.View;

namespace PESpy
{
    public readonly struct ScopeTableAndGsHandlerData : IViewableValue
    {
        public ScopeTable ScopeTable { get; }

        public GsHandlerData GsHandlerData { get; }

        public int Offset { get; }

        internal unsafe ScopeTableAndGsHandlerData(in MemoryChunk chunk)
        {
            Offset = chunk.AbsoluteOffset;

            ScopeTable = new ScopeTable(chunk);

            var scopeTableSize = ScopeTable.StructSize;
            GsHandlerData = new GsHandlerData(chunk.AbsoluteOffset + scopeTableSize, chunk.Pointer + scopeTableSize);
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //Both values are globals
            writer.WriteGlobal(ScopeTable);
            writer.WriteGlobal(GsHandlerData);
        }

        IView? IViewable.WriteStruct(ViewWriter writer) => null;

        int IViewable.NumChildren() => throw new NotSupportedException();

        void IViewable.WriteChild(int index, ref StructWriter structWriter) => throw new NotSupportedException();
    }
}
