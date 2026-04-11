using System;
using System.Diagnostics;
using ClrDebug.OMF;

namespace PESpy.PDB.DIA
{
    internal struct NB05AddrTrav : IEnumProvider
    {
        private NB05EnumPubsByAddr _enumByAddr;
        private EnumSC _enumSC;

        public SymCache SymCache { get; }

        internal NB05AddrTrav(
            NB05SymCache symCache,
            ISectionContribs sectionContribs)
        {
            SymCache = symCache;
            var entries = symCache._symbolAccessor._globalEntries;

            for (var i = 0; i < entries.Length; i++)
            {
                ref var entry = ref entries[i];

                switch (entry.SubSection)
                {
                    case SST.sstGlobalPub:
                        Debug.Assert(_enumByAddr == null);
                        var omfHashedSymbols = (OMFHashedSymbols) entry.Data;

                        if (omfHashedSymbols.AddressHashTable is IAddrHash32 a)
                        {
                            //We indirectly assert that all of our IAddrHash32 instances
                            //must also implement IAddrHashInternal32
                            _enumByAddr = new NB05EnumPubsByAddr(omfHashedSymbols, (IAddrHashInternal32) a);
                            break;
                        }
                        break;

                    case SST.sstPublic:
                    case SST.SSTPUBLIC:
                    case SST.sstPublicSym:
                        throw new NotImplementedException();
                }
            }

            _enumSC = new EnumSC(sectionContribs);
        }

        public bool EnumByAddrLocate(ISECT seg, int off) =>
            _enumByAddr?.Locate(seg, off) == true;

        public bool EnumByAddrNext(out SymType symType)
        {
            if (_enumByAddr?.Next() == true)
            {
                symType = _enumByAddr.Current;
                return true;
            }

            symType = default;
            return false;
        }

        public bool EnumContribLocate(ISECT seg, int off) =>
            _enumSC.Locate(seg, off);

        public bool EnumContribNext(out SC40 sc)
        {
            if (_enumSC.Next())
            {
                sc = _enumSC.Current;
                return true;
            }

            sc = default;
            return false;
        }

        public bool EnumContribPrev(out SC40 sc)
        {
            if (_enumSC.Previous())
            {
                sc = _enumSC.Current;
                return true;
            }

            sc = default;
            return false;
        }
    }
}
