namespace PESpy.PDB
{
    internal class CFuncByAddrTrav : CModSymsByAddrTrav
    {
        public CFuncByAddrTrav(
            CAllSymsByAddrTrav parentTrav,
            OffSeg targetOffSeg,
            OffSegSym bestOffSeg) : base(parentTrav, targetOffSeg, bestOffSeg)
        {
        }

        protected override bool find(ISECT scSeg, int scOff, out OffSegSym candidateOffSeg)
        {
            if (!_parentTrav._symCache.TryGetFunctionSymbol(_imod, _targetSeg, _targetOff, out candidateOffSeg))
                return false;

            if (candidateOffSeg.seg >= _targetSeg && (candidateOffSeg.seg != _targetSeg || candidateOffSeg.off > _targetOff))
                return false;

            if (scSeg < candidateOffSeg.seg || scSeg == candidateOffSeg.seg && scOff <= candidateOffSeg.off)
                return true;

            var displacement = _targetSeg == candidateOffSeg.seg
                ? (uint) (_targetOff - candidateOffSeg.off)
                : uint.MaxValue;

            candidateOffSeg.symType.TryGetLength(out var length);

            if (displacement >= length)
                return false;

            return true;
        }

        public override bool next(out TraverserResult result)
        {
            if (FInit(out result))
            {
                result.hasName = true; //All functions should have names
                return true;
            }

            result = default;
            return false;
        }
    }
}
