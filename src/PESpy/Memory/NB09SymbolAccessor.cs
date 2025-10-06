using System;
using ClrDebug.OMF;
using ClrDebug.PDB;
using PESpy.PDB;

namespace PESpy
{
    internal class NB09SymbolAccessor : NB05SymbolAccessor
    {
        //In versions subsequent to NB05, the CodeView data should begin with a list of sstModule
        //entries. If we are yet to count how many modules there are, we need to do that first
        //(as that then tells us how many modules to skip over to actually get to the per module data)
        private int _numModules;

        public NB09SymbolAccessor(IFile file) : base(file)
        {
        }

        public override SymType GetModuleSymbol(ushort imod, int ibSym)
        {
            /* The ordering of module sections should be
             * 1. sstAlignSym
             * 2. sstSrcModule
             */

            var index = GetModuleSubSectionIndex(imod, SST.sstAlignSym);

            if (index == -1)
                return default;

            ref var dirEntry = ref this.data.DirEntries[index];

            var data = (OMFModuleSymbols?) dirEntry.Data;

            if (data == null)
                return default;

            return data.GetSymbolFromOffset(ibSym);
        }

        public override TypType GetTypTypeFromIndex(CV_typ_t typeIndex)
        {
            throw new NotImplementedException();
        }

        protected int GetModuleSubSectionIndex(ushort imod, SST kind)
        {
            var dirEntries = data.DirEntries;

            var lo = GetNumModules(); //Skip over the sstModule entries at the beginning
            var hi = dirEntries.Length - 1;

            while (lo <= hi)
            {
                var mid = (lo + hi) / 2;

                ref var dirEntry = ref dirEntries[mid];

                if (dirEntry.iMod > imod)
                {
                    hi = mid - 1;
                }
                else if (dirEntry.iMod < imod)
                {
                    lo = mid + 1;
                }
                else
                {
                    do
                    {
                        if (dirEntry.SubSection == kind)
                            return mid;

                        //Figure out the direction to move mid based on the one we're after
                        switch (dirEntry.SubSection)
                        {
                            case SST.sstAlignSym:
                                throw new NotImplementedException();

                            case SST.sstSrcModule:
                                mid--;
                                break;

                            default:
                                throw new NotImplementedException();
                        }

                        dirEntry = ref dirEntries[mid];
                    } while (dirEntry.iMod == imod);

                    throw new NotImplementedException();
                }
            }

            return -1;
        }

        private int GetNumModules()
        {
            if (_numModules != 0)
                return _numModules;

            //Binary search to find the last sstModule entry

            var dirEntries = data.DirEntries;

            var lo = 0;
            var hi = dirEntries.Length - 1;

            while (lo <= hi)
            {
                var mid = (lo + hi) / 2;

                ref var item = ref dirEntries[mid];

                if (item.SubSection == ClrDebug.OMF.SST.sstModule)
                {
                    lo = mid + 1;
                }
                else
                {
                    hi = mid - 1;
                }
            }

            _numModules = lo;

            return lo;
        }
    }
}
