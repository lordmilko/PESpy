using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    //Type is made up
    public class PDBModuleSymbols : IValue, IViewable
    {
        public CV_SIGNATURE Signature { get; }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public SymTypeList List { get; }

        public int Offset => chunk.AbsoluteOffset;

        private readonly MemoryChunk chunk;

        public unsafe SymType GetSymbolFromOffset(int offset) => (SYMTYPE*)(chunk.Pointer + offset);

        internal PDBModuleSymbols(in MemoryChunk chunk, CV_SIGNATURE signature, SymTypeList symbols)
        {
            this.chunk = chunk;
            Signature = signature;
            List = symbols;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            writer.WriteGlobal(Offset, Signature, sizeof(int), ViewKind.Value);
            writer.WritePagedGlobal(chunk.RelativeOffset + 4, (PagedMemoryBlock) chunk.block, List);
        }

        IView? IViewable.WriteStruct(ViewWriter writer) => null;

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter) => throw new NotSupportedException();
    }
}
