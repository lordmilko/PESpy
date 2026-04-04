using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.PDB;
using PESpy.View;

namespace PESpy
{
    //Type is made up
    [DebuggerDisplay("{Signature} Symbols")]
    public class OMFModuleSymbols : IValue, IViewable, ICodeViewModuleAccessor
    {
        public CV_SIGNATURE Signature { get; }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public SymTypeList List { get; }

        SymTypeList ICodeViewModuleAccessor.Symbols => List;

        public int Offset => chunk.AbsoluteOffset;

        private readonly MemoryChunk chunk;
        private readonly IMOD imod; //0 based

        internal OMFModuleSymbols(
            in MemoryChunk chunk,
            IMOD imod,
            CV_SIGNATURE signature,
            SymTypeList symbols,
            NB05SymbolAccessor codeViewAccessor)
        {
            SymbolMemoryTracker.RegisterCVSymbolMemory(chunk, codeViewAccessor, this);

            this.chunk = chunk;
            this.imod = imod;
            Signature = signature;
            List = symbols;
        }

        public unsafe SymType GetSymbolFromOffset(int offset) => (SYMTYPE*) (chunk.Pointer + offset);

        public bool TryGetFunctionSymbol(int off, ISECT seg, out SymType symType)
        {
            if (((NB05SymbolAccessor) List.codeViewAccessor)._symCache.TryGetFunctionSymbol(imod, seg, off, out var offSegSym))
            {
                symType = offSegSym.symType;
                return true;
            }

            symType = default;
            return false;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            writer.WriteGlobal(Offset, Signature, sizeof(int), ViewKind.CvSignature);

            var block = chunk.block;

            writer.WriteGlobal(chunk.RelativeOffset + 4, List);
        }

        IView? IViewable.WriteStruct(ViewWriter writer) => null;

        int IViewable.NumChildren() => throw new NotSupportedException();

        void IViewable.WriteChild(int index, ref StructWriter structWriter) => throw new NotSupportedException();
    }
}
