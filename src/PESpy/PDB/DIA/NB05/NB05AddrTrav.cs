using System;
using System.Diagnostics;
using ClrDebug.OMF;

namespace PESpy.PDB.DIA
{
    internal sealed class NB05AddrTrav : CAllSymsByAddrTrav
    {
        private NB05EnumPubsByAddr _enumByAddr;
        private EnumSC _enumSC;

        internal NB05AddrTrav(
            NB05SymCache symCache,
            ISectionContribs sectionContribs) : base(symCache)
        {
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

        public override bool EnumByAddrLocate(ISECT seg, int off) =>
            _enumByAddr?.Locate(seg, off) == true;

        public override bool EnumByAddrNext(out SymType symType)
        {
            if (_enumByAddr?.Next() == true)
            {
                symType = _enumByAddr.Current;
                return true;
            }

            symType = default;
            return false;
        }

        public override bool EnumContribLocate(ISECT seg, int off) =>
            _enumSC.Locate(seg, off);

        public override bool EnumContribNext(out SC40 sc)
        {
            if (_enumSC.Next())
            {
                sc = _enumSC.Current;
                return true;
            }

            sc = default;
            return false;
        }

        public override bool EnumContribPrev(out SC40 sc)
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
