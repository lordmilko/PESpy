namespace PESpy.PDB.DIA
{
    internal sealed class PDBFileAddrTrav : CAllSymsByAddrTrav
    {
        private EnumPubsByAddr _enumByAddr;
        private EnumSC _enumSC;

        internal static bool TryCreate(
            PDBFileSymCache symCache,
            PDBFile pdbFile,
            out PDBFileAddrTrav trav)
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

            trav = new PDBFileAddrTrav(symCache, enumByAddr, enumSC);
            return true;
        }

        private PDBFileAddrTrav(
            PDBFileSymCache symCache,
            EnumPubsByAddr enumByAddr,
            EnumSC enumSC) : base(symCache)
        {
            _enumByAddr = enumByAddr;
            _enumSC = enumSC;
        }

        public override bool EnumByAddrLocate(ISECT seg, int off) =>
            _enumByAddr.Locate(seg, off);

        public override bool EnumByAddrNext(out SymType symType)
        {
            if (_enumByAddr.Next())
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
