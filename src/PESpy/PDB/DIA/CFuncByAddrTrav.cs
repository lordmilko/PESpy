namespace PESpy.PDB
{
    internal interface IModSymFinder
    {
        bool find(ISECT scSeg, int scOff, out OffSegSym candidateOffSeg);
    }

    internal struct CFuncByAddrTrav<TEnumProvider> : IModSymFinder where TEnumProvider : IEnumProvider
    {
        internal CModSymsByAddrTrav<TEnumProvider> _modTrav;

        public CFuncByAddrTrav(
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

            if (!_modTrav._enumProvider.SymCache.TryGetFunctionSymbol(_modTrav._imod, targetSeg, targetOff, out candidateOffSeg))
                return false;

            if (candidateOffSeg.seg >= targetSeg && (candidateOffSeg.seg != targetSeg || candidateOffSeg.off > targetOff))
                return false;

            if (scSeg < candidateOffSeg.seg || scSeg == candidateOffSeg.seg && scOff <= candidateOffSeg.off)
                return true;

            var displacement = targetSeg == candidateOffSeg.seg
                ? (uint) (targetOff - candidateOffSeg.off)
                : uint.MaxValue;

            candidateOffSeg.symType.TryGetLength(out var length);

            if (displacement >= length)
                return false;

            return true;
        }

        public bool next(out TraverserResult result)
        {
            if (_modTrav.FInit(ref this, out result))
            {
                result.hasName = true; //All functions should have names
                return true;
            }

            result = default;
            return false;
        }
    }
}
