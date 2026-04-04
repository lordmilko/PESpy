using System;
using ClrDebug.OMF;

namespace PESpy.PDB.DIA
{
    internal class NB05ModCache : ModCache
    {
        protected override SymTypeList Symbols => OMFModuleSymbols?.List;

        public OMFModuleSymbols? OMFModuleSymbols { get; }

        internal NB05ModCache(OMFDirEntry[] entries)
        {
            //I'm not sure how many entries we might need to deal with;
            //if it's more than 1, we need to rework our API to allow enumerating over
            //the SymTypeList items we had to the base class can seed the full list
            //of cached symbols. For now, we'll assume you can only have a single
            //entry of interest

            OMFModuleSymbols symbols = null;

            for (var i = 0; i < entries.Length; i++)
            {
                ref var entry = ref entries[i];

                switch (entry.SubSection)
                {
                    case SST.sstModule:
                    case SST.sstSrcModule:
                        continue;

                    case SST.sstAlignSym:
                        if (symbols != null)
                            throw new NotImplementedException("Don't know how to handle having multiple symbol sections within a module."); //Maybe have some sort of allocation free iterator the base class can call upon to enumerate all SymTypeList the derived ModCache type has on offer

                        symbols = (OMFModuleSymbols) entry.Data;
                        break;

                    default:
                        throw new NotImplementedException($"Don't know how to handle {nameof(SST)} '{entry.SubSection}'");
                }
            }

            //Some modules might not have any symbols

            OMFModuleSymbols = symbols;
        }
    }
}
