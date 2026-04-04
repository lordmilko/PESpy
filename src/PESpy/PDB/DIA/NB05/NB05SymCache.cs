using System;
using ClrDebug.OMF;

namespace PESpy.PDB.DIA
{
    internal class NB05SymCache : SymCache
    {
        internal NB05SymbolAccessor _symbolAccessor;

        internal NB05SymCache(NB05SymbolAccessor symbolAccessor)
        {
            _symbolAccessor = symbolAccessor;
        }

        public override bool IsMinimal => false;

        protected override int NumModules => _symbolAccessor._moduleEntries.Length;

        protected override ModCache CreateModCache(ushort imod) => new NB05ModCache(_symbolAccessor._moduleEntries[imod]);

        protected override OffSegSym[] InitializeGlobalDataSymbols()
        {
            var globalDataList = new PooledList<OffSegSym>();

            try
            {
                var entries = _symbolAccessor._globalEntries;

                for (var i = 0; i < entries.Length; i++)
                {
                    ref var entry = ref entries[i];

                    if (entry.SubSection != SST.sstGlobalSym)
                        continue;

                    var symbols = (OMFHashedSymbols) entry.Data;

                    foreach (var symType in symbols.Symbols)
                        ProcessGlobalDataSymbol(symType, ref globalDataList);
                }

                //When the off/seg is equivalent, we seem to get a different order to DIA due to the different sort
                //algorithm that is used
                globalDataList.Sort((a, b) =>
                {
                    var diff = a.seg.CompareTo(b.seg);

                    if (diff != 0)
                        return diff;

                    return a.off.CompareTo(b.off);
                });

                _globalDataSymbols = globalDataList.ToArray();
                return _globalDataSymbols;
            }
            finally
            {
                globalDataList.Dispose();
            }
        }

        public override void GetModuleSymbols(IMOD imod, out SymTypeList symbols, out ICodeViewModuleAccessor codeViewModuleAccessor)
        {
            var modCache = (NB05ModCache) CreateModCache(imod);

            symbols = modCache.OMFModuleSymbols.List;
            codeViewModuleAccessor = modCache.OMFModuleSymbols;
        }
    }
}
