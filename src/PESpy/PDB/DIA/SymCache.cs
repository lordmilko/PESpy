using System.Runtime.CompilerServices;
using System.Threading;
using static ClrDebug.PDB.SYM_ENUM_e;

namespace PESpy.PDB
{
    internal abstract class SymCache
    {
        public abstract bool IsMinimal { get; }

        protected OffSegSym[] _globalDataSymbols;
        private ModCache[] _modCaches;

        protected abstract int NumModules { get; }

        private ModCache GetModCache(ushort imod)
        {
            if (_modCaches == null)
                Interlocked.CompareExchange(ref _modCaches, new ModCache[NumModules], null);

            var modCache = _modCaches[imod];

            if (modCache == null)
            {
                modCache = CreateModCache(imod);

                var original = Interlocked.CompareExchange(ref _modCaches[imod], modCache, null);

                modCache = original ?? modCache;
            }

            return modCache;
        }

        protected abstract ModCache CreateModCache(ushort imod);

        private bool findGlobalData(
            ISECT targetSeg,
            int targetOff,
            out OffSegSym offSegSym)
        {
            var globalDataSymbols = _globalDataSymbols;

            if (globalDataSymbols == null)
                globalDataSymbols = InitializeGlobalDataSymbols();

            var lo = 0;
            var hi = globalDataSymbols.Length; //Not -1

            while (lo < hi) //This should be < not <=
            {
                var mid = (lo + hi) / 2;

                var dataSym = globalDataSymbols[mid];

                if (targetSeg < dataSym.seg || (targetSeg == dataSym.seg && targetOff < dataSym.off))
                    hi = mid;
                else
                    lo = mid + 1;
            }

            if (lo > 0)
            {
                offSegSym = globalDataSymbols[lo - 1];
                return true;
            }

            offSegSym = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void ProcessGlobalDataSymbol(SymType symType, ref PooledList<OffSegSym> globalDataList)
        {
            ushort seg;
            int off;

            switch (symType.rectyp)
            {
                //DataSym16
                case S_LDATA16:
                case S_GDATA16:
                    var dataSym16 = (DataSym16) symType;
                    seg = dataSym16.seg;
                    off = dataSym16.off;
                    break;

                //DataSym3216t
                case S_LDATA32_16t:
                case S_GDATA32_16t:
                    var dataSym3216t = (DataSym3216t) symType;
                    seg = dataSym3216t.seg;
                    off = dataSym3216t.off;
                    break;

                //DataSym32
                case S_LDATA32:
                case S_GDATA32:
                    var dataSym32 = (DataSym32) symType;
                    seg = dataSym32.seg;
                    off = dataSym32.off;
                    break;

                default:
                    return;
            }

            globalDataList.Add(new OffSegSym
            {
                off = off,
                seg = seg,
                symType = symType
            });
        }

        protected abstract OffSegSym[] InitializeGlobalDataSymbols();

        public bool TryGetGlobalSymbol(ISECT seg, int off, out OffSegSym offSegSym) =>
            findGlobalData(seg, off, out offSegSym);

        public bool TryGetFunctionSymbol(IMOD imod, ISECT seg, int off, out OffSegSym offSegSym)
        {
            var modCache = GetModCache(imod);

            return modCache.blockByAddr(seg, off, out offSegSym);
        }

        public bool TryGetDataSymbol(IMOD imod, ISECT seg, int off, out OffSegSym offSegSym)
        {
            var modCache = GetModCache(imod);

            return modCache.dataByAddr(seg, off, out offSegSym);
        }

        public abstract void GetModuleSymbols(IMOD imod, out SymTypeList symbols, out ICodeViewModuleAccessor codeViewModuleAccessor);
    }
}
