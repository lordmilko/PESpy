using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.PDB;
using PESpy.View;

namespace PESpy
{
    //Type is made up
    [DebuggerDisplay("{Signature} Symbols")]
    public class OMFModuleSymbols : IValue, IViewable
    {
        public CV_SIGNATURE Signature { get; }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public SymTypeList List { get; }

        public int Offset => chunk.AbsoluteOffset;

        private readonly MemoryChunk chunk;

        internal OMFModuleSymbols(in MemoryChunk chunk, CV_SIGNATURE signature, SymTypeList symbols)
        {
            this.chunk = chunk;
            Signature = signature;
            List = symbols;
        }

        public unsafe SymType GetSymbolFromOffset(int offset) => (SYMTYPE*) (chunk.Pointer + offset);

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            writer.WriteGlobal(Offset, Signature, sizeof(int), ViewKind.CvSignature);

            var block = chunk.block;

            writer.WriteGlobal(chunk.RelativeOffset + 4, List);
        }

        IView? IViewable.WriteStruct(ViewWriter writer) => null;

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter) => throw new NotSupportedException();
    }
}
