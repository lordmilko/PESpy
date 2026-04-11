namespace PESpy.PDB
{
    /// <summary>
    /// Models the essential functionality of the msdia140!CDataByAddrTrav class used
    /// for resolving an RVA to its "nearest" data symbol within a specific module.
    /// </summary>
    internal struct CDataByAddrTrav<TEnumProvider> : IModSymFinder where TEnumProvider : IEnumProvider
    {
        private CModSymsByAddrTrav<TEnumProvider> _modTrav;

        public CDataByAddrTrav(
            TEnumProvider enumProvider,
            OffSeg targetOffSeg,
            OffSegSym bestOffSeg)
        {
            _modTrav = new CModSymsByAddrTrav<TEnumProvider>(enumProvider, targetOffSeg, bestOffSeg);
        }

        public bool find(ISECT scSeg, int scOff, out OffSegSym candidateOffSeg)
        {
            var targetSeg = _modTrav._targetSeg;
            var targetOff = _modTrav._targetOff;

            if (!_modTrav._enumProvider.SymCache.TryGetDataSymbol(_modTrav._imod, targetSeg, targetOff, out candidateOffSeg))
                return false;

            //ModCache::dataByAddr may be handed a module where all of its symbols are way before the offset we're after. This is an issue,
            //because while trying to find the best module to use, we may need to rewind by 1 to get the best module. DIA seems to handle
            //this in CDataByAddrTrav::find by doing the following check.

            var candidateOff = candidateOffSeg.off;
            var candidateSeg = candidateOffSeg.seg;

            if (candidateSeg != targetSeg || candidateOff > targetOff ||
                scSeg >= candidateSeg && (scSeg != candidateSeg || scOff > candidateOff))
            {
                return false;
            }

            return true;
        }

        public bool next(out TraverserResult result)
        {
            //DIA just COMDAT folds into CFuncByAddrTrav::next which just forwards to CModSymsByAddrTrav::FInit

            if (_modTrav.FInit(ref this, out result))
            {
                result.hasName = true; //We only include symbols that have names in our cache, so implicitly we know we have a name
                return true;
            }

            result = default;
            return false;
        }
    }
}
