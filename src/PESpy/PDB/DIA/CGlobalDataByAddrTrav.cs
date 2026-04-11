namespace PESpy.PDB
{
    internal struct CGlobalDataByAddrTrav<TEnumProvider> where TEnumProvider : IEnumProvider
    {
        private TEnumProvider _enumProvider;

        private ISECT _targetSeg;
        private int _targetOff;

        public CGlobalDataByAddrTrav(
            TEnumProvider enumProvider,
            OffSeg targetOffSeg)
        {
            _enumProvider = enumProvider;

            _targetSeg = targetOffSeg.seg;
            _targetOff = targetOffSeg.off;
        }

        public bool next(out TraverserResult result)
        {
            if (_enumProvider.SymCache.TryGetGlobalSymbol(_targetSeg, _targetOff, out var globalSym))
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
