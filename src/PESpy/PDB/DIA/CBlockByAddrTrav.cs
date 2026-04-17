namespace PESpy.PDB
{
    /// <summary>
    /// Models the essential functionality of the msdia140!CBlockByAddrTrav class used
    /// for resolving an RVA to its "nearest" block symbol at or under a function symbol
    /// within a specific module.
    /// </summary>
    internal struct CBlockByAddrTrav<TEnumProvider> where TEnumProvider : IEnumProvider
    {
        private CFuncByAddrTrav<TEnumProvider> _funcTrav;

        public CBlockByAddrTrav(
            TEnumProvider enumProvider,
            OffSeg targetOffSeg,
            OffSegSym bestOffSeg)
        {
            _funcTrav = new CFuncByAddrTrav<TEnumProvider>(enumProvider, targetOffSeg, bestOffSeg);
        }

        public bool next(out TraverserResult result)
        {
            if (FInit(out result))
            {
                //Need to check length because block has a name but it's empty
                result.hasName = result.offSegSym.symType.TryGetName(out var name) && name.Length > 0; //todo: use codeviewaccessor?
                return true;
            }

            result = default;
            return false;
        }

        internal bool FInit(out TraverserResult result)
        {
            ref var modTrav = ref _funcTrav._modTrav;

            if (!modTrav.FInit(ref _funcTrav, out result))
                return false;

            modTrav._enumProvider.SymCache.GetModuleSymbols(modTrav._imod, out var symTypeList, out var codeViewModuleAccessor);

            var enumerator = ((BlockSym) result.offSegSym.symType).GetChildren(codeViewModuleAccessor).GetEnumerator();

            var targetSeg = modTrav._targetSeg;
            var targetOff = modTrav._targetOff;

            while (enumerator.MoveNext())
            {
                var childSymType = enumerator.Current;

                if (!childSymType.IsBlockSym())
                    continue;

                if (!childSymType.TryGetRawOffSeg(out var off, out var seg) || !childSymType.TryGetLength(out var length))
                    continue;

                if (seg > targetSeg || (seg == targetSeg && off > targetOff))
                    break;

                if (off <= targetOff)
                {
                    uint disp = targetSeg == seg
                        ? (uint) (targetOff - off)
                        : uint.MaxValue;

                    if (disp < length)
                    {
                        //Caller will check if we have a name (which we don't)
                        result.offSegSym = new OffSegSym
                        {
                            off = off,
                            seg = seg,
                            symType = childSymType
                        };

                        //Now dig into this symbol
                        enumerator = ((BlockSym) result.offSegSym.symType).GetChildren(codeViewModuleAccessor).GetEnumerator();
                    }
                }
            }

            return true;
        }
    }
}
