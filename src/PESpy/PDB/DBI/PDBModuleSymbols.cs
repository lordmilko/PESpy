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

        internal PDBModuleSymbols(in MemoryChunk chunk, CV_SIGNATURE signature, SymTypeList symbols)
        {
            this.chunk = chunk;
            Signature = signature;
            List = symbols;
        }

        public unsafe SymType GetSymbolFromOffset(int offset) => (SYMTYPE*) (chunk.Pointer + offset);

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //If the signature can be C6, we need to not write the signature and not do offset + 4 below
            Debug.Assert(Signature is CV_SIGNATURE.C7 or CV_SIGNATURE.C11 or CV_SIGNATURE.C13);

            writer.WriteGlobal(Offset, Signature, sizeof(int), ViewKind.CvSignature);

            var block = chunk.block;

            if (block is PagedMemoryBlock p)
                writer.WritePagedGlobal(chunk.RelativeOffset + 4, p, List);
            else
                writer.WriteGlobal(chunk.RelativeOffset + 4, List);
        }

        IView? IViewable.WriteStruct(ViewWriter writer) => null;

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter) => throw new NotSupportedException();
    }
}
