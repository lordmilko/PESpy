namespace PESpy.PDB
{
    internal class CGlobalDataByAddrTrav : Traverser
    {
        private CAllSymsByAddrTrav _parentTrav;

        public CGlobalDataByAddrTrav(
            CAllSymsByAddrTrav parentTrav,
            OffSeg targetOffSeg,
            OffSegSym bestOffSeg) : base(targetOffSeg, bestOffSeg)
        {
            _parentTrav = parentTrav;
        }

        public override bool next(out TraverserResult result)
        {
            if (_parentTrav._symCache.TryGetGlobalSymbol(_targetSeg, _targetOff, out var globalSym))
            {
                result = new TraverserResult
                {
                    offSegSym = globalSym,
                    hasName = true //todo: is this true?
                };
                return true;
            }

            result = default;
            return false;
        }
    }
}
