using System;
using PESpy.View;

namespace PESpy
{
    /// <summary>
    /// Encapsulates the data that is passed to the <see cref="WellKnownExceptionHandlerKind.__GSHandlerCheck_SEH"/> exception handler.
    /// </summary>
    public readonly struct ScopeTableAndGsHandlerData : IValue //Not IViewable because this is just a transparent struct
    {
        public ScopeTable ScopeTable { get; }

        public GsHandlerData GsHandlerData { get; }

        public long Offset { get; }

        internal unsafe ScopeTableAndGsHandlerData(in MemoryChunk chunk)
        {
            Offset = chunk.AbsoluteOffset;

            ScopeTable = new ScopeTable(chunk);

            var scopeTableSize = ScopeTable.StructSize;
            GsHandlerData = new GsHandlerData(chunk.AbsoluteOffset + scopeTableSize, chunk.Pointer + scopeTableSize);
        }

        internal void WriteInline(ref EagerStructWriter s)
        {
            s.WriteInline(ScopeTable);
            s.WriteInline(GsHandlerData);
        }
    }
}
