using ClrDebug.PDB;

namespace PESpy.PDB
{
    internal struct CPubByAddrTrav<TEnumProvider> where TEnumProvider : IEnumProvider
    {
        private TEnumProvider _enumProvider;

        private ISECT _targetSeg;
        private int _targetOff;

        public ISECT _bestSeg;
        public int _bestOff;

        public CPubByAddrTrav(
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

        public unsafe bool next(out TraverserResult result)
        {
            if (_enumProvider.EnumByAddrLocate(_targetSeg, _targetOff))
            {
                if (_enumProvider.EnumByAddrNext(out var pubSym))
                {
                    int off;
                    ISECT seg;

                    while (true)
                    {
                        pubSym.TryGetOffSeg(out off, out seg);

                        if (((ulong) seg << 32 | (uint) off) > ((ulong) _targetSeg << 32 | (uint) _targetOff))
                            break;

                        //CPubByAddrTrav::next returns S_OK or S_FALSE. Only S_OK is accepted as success

                        //There's some extra logic here but i dont know what virtual function is being called upon,
                        //so for now assume success
                        break;
                    }

                    result = new TraverserResult
                    {
                        offSegSym = new OffSegSym
                        {
                            off = off,
                            seg = seg,
                            symType = (SYMTYPE*) pubSym
                        },
                        hasName = true //Assume all publics have names?
                    };                    
                    return true;
                }
            }

            //CPubByAddrTrav::next has some ENC logic, but we don't have that
            result = default;
            return false;
        }
    }
}
