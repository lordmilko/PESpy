namespace PESpy.PDB
{
    /// <summary>
    /// Models the essential functionality of the msdia140!CDataByAddrTrav class used
    /// for resolving an RVA to its "nearest" data symbol within a specific module.
    /// </summary>
    internal class CDataByAddrTrav : CModSymsByAddrTrav
    {
        public CDataByAddrTrav(
            CAllSymsByAddrTrav parentTrav,
            OffSeg targetOffSeg,
            OffSegSym bestOffSeg) : base(parentTrav, targetOffSeg, bestOffSeg)
        {
        }

        protected override bool find(ISECT scSeg, int scOff, out OffSegSym candidateOffSeg)
        {
            if (!_parentTrav._symCache.TryGetDataSymbol(_imod, _targetSeg, _targetOff, out candidateOffSeg))
                return false;

            //ModCache::dataByAddr may be handed a module where all of its symbols are way before the offset we're after. This is an issue,
            //because while trying to find the best module to use, we may need to rewind by 1 to get the best module. DIA seems to handle
            //this in CDataByAddrTrav::find by doing the following check.

            var candidateOff = candidateOffSeg.off;
            var candidateSeg = candidateOffSeg.seg;

            if (candidateSeg != _targetSeg || candidateOff > _targetOff ||
                scSeg >= candidateSeg && (scSeg != candidateSeg || scOff > candidateOff))
            {
                return false;
            }

            return true;
        }

        public override bool next(out TraverserResult result)
        {
            //DIA just COMDAT folds into CFuncByAddrTrav::next which just forwards to CModSymsByAddrTrav::FInit

            if (base.FInit(out result))
            {
                result.hasName = true; //We only include symbols that have names in our cache, so implicitly we know we have a name
                return true;
            }

            result = default;
            return false;
        }
    }
}
