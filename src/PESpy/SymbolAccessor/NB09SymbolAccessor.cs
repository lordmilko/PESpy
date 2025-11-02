using System;
using ClrDebug.OMF;
using ClrDebug.PDB;
using PESpy.PDB;

namespace PESpy
{
    internal class NB09SymbolAccessor : NB05SymbolAccessor
    {
        //Supposedly, iin versions subsequent to NB05, the CodeView data should begin with a list of sstModule
        //entries. We don't want to risk any issues however, so we'll treat the sequence and order of all
        //directory entries are untrusted regardless

        public NB09SymbolAccessor(IFile file) : base(file)
        {
        }

        public override SymType GetModuleSymbol(ushort imod, int ibSym)
        {
            /* The ordering of module sections should be
             * 1. sstAlignSym
             * 2. sstSrcModule
             */

            if (!TryGetModuleEntry(imod, SST.sstAlignSym, out var dirEntry))
                return default;

            var data = (OMFModuleSymbols?) dirEntry.Data;

            if (data == null)
                return default;

            return data.GetSymbolFromOffset(ibSym);
        }

        public override TypType GetTypTypeFromIndex(CV_typ_t typeIndex)
        {
            throw new NotImplementedException();
        }
    }
}
