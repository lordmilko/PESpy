namespace PESpy.PDB
{
    internal abstract class CModSymsByAddrTrav : Traverser
    {
        protected CAllSymsByAddrTrav _parentTrav;
        protected ushort _imod;

        protected CModSymsByAddrTrav(
            CAllSymsByAddrTrav parentTrav,
            OffSeg targetOffSeg,
            OffSegSym bestOffSeg) : base(targetOffSeg, bestOffSeg)
        {
            _parentTrav = parentTrav;
        }

        internal virtual bool FInit(out TraverserResult result)
        {
            //It doesn't matter if locate fails; in CAllSymsByAddrTrav a flag is set to say "try anyway".
            //The enumerator will have been updated even if locate returns false

            _parentTrav.EnumContribLocate(_targetSeg, _targetOff);

            if (_parentTrav.EnumContribNext(out var sc))
            {
                _imod = sc.imod;

                if (find(sc.isect, sc.off, out var bestOffSeg))
                {
                    result = new TraverserResult
                    {
                        offSegSym = bestOffSeg,
                        imod = sc.imod
                    };
                    return true;
                }

                if (!_parentTrav._symCache.IsMinimal)
                {
                    while (_parentTrav.EnumContribPrev(out sc))
                    {
                        _imod = sc.imod;

                        var scOff = sc.off;
                        var scSeg = sc.isect;
                        var scEnd = scOff + sc.cb;

                        if (scSeg < _bestSeg || scSeg == _bestSeg && scEnd < _bestOff)
                            break;

                        if (find(scSeg, scOff, out bestOffSeg))
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

        protected abstract bool find(ISECT scSeg, int scOff, out OffSegSym bestOffSeg);
    }
}
