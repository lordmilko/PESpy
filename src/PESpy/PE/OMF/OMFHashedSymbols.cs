using System;
using PESpy.PDB;
using PESpy.View;

namespace PESpy
{
    //Type is made up
    public class OMFHashedSymbols : IViewableValue
    {
        public OMFSymHash Hash { get; }

        public SymTypeList Symbols { get; }

        //See the comments in OMFSymHash about how to parse these

        public IValue? SymbolHashTable { get; }

        public IValue? AddressHashTable { get; }

        public long Offset { get; }

        public SymType GetSymbolFromOffset(int offset) => Symbols.GetSymbolFromOffset(offset);

        internal OMFHashedSymbols(
            in MemoryChunk valueChunk,
            OMFSymHash hash,
            SymTypeList symbols,
            IValue? symbolHashTable,
            IValue? addressHashTable,
            ICodeViewAccessor codeViewAccessor)
        {
            //We're not part of a module, so we don't implement ICodeViewModuleAccessor
            SymbolMemoryTracker.RegisterCVSymbolMemory(valueChunk, codeViewAccessor, null);

            Offset = valueChunk.AbsoluteOffset;
            Hash = hash;
            Symbols = symbols;
            SymbolHashTable = symbolHashTable;
            AddressHashTable = addressHashTable;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            writer.WriteGlobal(Hash);
            writer.WriteGlobal(Offset + OMFSymHash.StructSize, Symbols);
            writer.WriteGlobal((IViewable) SymbolHashTable);
            writer.WriteGlobal((IViewable) AddressHashTable);
        }

        IView? IViewable.WriteStruct(ViewWriter writer) => null;

        int IViewable.NumChildren() => throw new NotSupportedException();

        void IViewable.WriteChild(int index, ref StructWriter structWriter) => throw new NotSupportedException();
    }
}
