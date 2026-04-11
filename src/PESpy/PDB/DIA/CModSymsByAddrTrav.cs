namespace PESpy.PDB
{
    internal struct CModSymsByAddrTrav<TEnumProvider> where TEnumProvider : IEnumProvider
    {
        internal TEnumProvider _enumProvider;
        internal ushort _imod;

        internal ISECT _targetSeg;
        internal int _targetOff;

        private ISECT _bestSeg;
        private int _bestOff;

        public CModSymsByAddrTrav(
            TEnumProvider enumProvider,
            OffSeg targetOffSeg,
            OffSegSym bestOffSeg)
        {
            _enumProvider = enumProvider;

            _targetSeg = targetOffSeg.seg;
            _targetOff = targetOffSeg.off;
            _bestSeg = bestOffSeg.seg;
            _bestOff = bestOffSeg.off;
        }

        internal bool FInit<T>(ref T derived, out TraverserResult result) where T : IModSymFinder
        {
            //It doesn't matter if locate fails; in CAllSymsByAddrTrav a flag is set to say "try anyway".
            //The enumerator will have been updated even if locate returns false

            _enumProvider.EnumContribLocate(_targetSeg, _targetOff);

            if (_enumProvider.EnumContribNext(out var sc))
            {
                _imod = sc.imod;

                if (derived.find(sc.isect, sc.off, out var bestOffSeg))
                {
                    result = new TraverserResult
                    {
                        offSegSym = bestOffSeg,
                        imod = sc.imod
                    };
                    return true;
                }

                //We can't move this to a separate method to help with generics, as we need to call find()
                //again below
                if (!_enumProvider.SymCache.IsMinimal)
                {
                    while (_enumProvider.EnumContribPrev(out sc))
                    {
                        _imod = sc.imod;

                        var scOff = sc.off;
                        var scSeg = sc.isect;
                        var scEnd = scOff + sc.cb;

                        if (scSeg < _bestSeg || scSeg == _bestSeg && scEnd < _bestOff)
                            break;

                        if (derived.find(scSeg, scOff, out bestOffSeg))
                        {
                            result = new TraverserResult
                            {
                                offSegSym = bestOffSeg,
                                imod = sc.imod
                            };
                            return true;
                        }
                    }
                }
            }

            result = default;
            return false;
        }
    }
}
