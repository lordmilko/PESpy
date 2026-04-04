using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    //Type is made up
    public class PDBModuleSymbols : IValue, IViewable, ICodeViewModuleAccessor
    {
        public CV_SIGNATURE Signature { get; }

        /// <summary>
        /// Provides access to all symbols contained within the module.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public SymTypeList List { get; }

        public int Offset => chunk.AbsoluteOffset;

        SymTypeList ICodeViewModuleAccessor.Symbols => List;

        private readonly MemoryChunk chunk;
        private IMOD imod;

        internal PDBModuleSymbols(
            in MemoryChunk chunk,
            IMOD imod,
            CV_SIGNATURE signature,
            SymTypeList symbols)
        {
            SymbolMemoryTracker.RegisterPDBSymbolMemory(chunk, this);

            this.chunk = chunk;
            this.imod = imod;
            Signature = signature;
            List = symbols;
        }

        public unsafe SymType GetSymbolFromOffset(int offset)
        {
            var ptr = (SYMTYPE*) (chunk.Pointer + offset);

            Debug.Assert(ptr < List.end);

            return ptr;
        }

        public bool TryGetFunctionSymbol(int off, ISECT seg, out SymType symType)
        {
            if (chunk.PDBFile()._symCache.TryGetFunctionSymbol(imod, seg, off, out var offSegSym))
            {
                symType = offSegSym.symType;
                return true;
            }

            symType = default;
            return false;
        }

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

        int IViewable.NumChildren() => throw new NotSupportedException();

        void IViewable.WriteChild(int index, ref StructWriter structWriter) => throw new NotSupportedException();
    }
}
