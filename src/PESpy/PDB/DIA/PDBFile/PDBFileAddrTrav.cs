namespace PESpy.PDB.DIA
{
    internal struct PDBFileAddrTrav : IEnumProvider
    {
        private EnumPubsByAddr _enumByAddr;
        private EnumSC _enumSC;

        public SymCache SymCache { get; }

        internal static bool TryCreate(
            PDBFileSymCache symCache,
            PDBFile pdbFile,
            out CAllSymsByAddrTrav<PDBFileAddrTrav> trav)
        {
            trav = default;

            var dbi = pdbFile.DBI;

            if (dbi == null)
                return false;

            if (!dbi.TryEnumContribs(out var enumSC))
                return false;

            var publics = pdbFile.PSGSI;

            if (publics == null)
                return false;

            if (!publics.TryEnumByAddr(out var enumByAddr))
                return false;

            trav = new CAllSymsByAddrTrav<PDBFileAddrTrav>(new PDBFileAddrTrav(symCache, enumByAddr, enumSC));
            return true;
        }

        private PDBFileAddrTrav(
            PDBFileSymCache symCache,
            EnumPubsByAddr enumByAddr,
            EnumSC enumSC)
        {
            SymCache = symCache;
            _enumByAddr = enumByAddr;
            _enumSC = enumSC;
        }

        public bool EnumByAddrLocate(ISECT seg, int off) =>
            _enumByAddr.Locate(seg, off);

        public bool EnumByAddrNext(out SymType symType)
        {
            if (_enumByAddr.Next())
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
