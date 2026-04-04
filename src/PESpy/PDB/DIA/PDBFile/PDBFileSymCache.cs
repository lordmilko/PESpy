namespace PESpy.PDB.DIA
{
    internal class PDBFileSymCache : SymCache
    {
        private int? _numModules;
        protected override int NumModules => _numModules ??= _pdbFile.DBI.Modules.Length;

        private bool? _isMinimal;
        public override bool IsMinimal => _isMinimal ??= _pdbFile.PDB?.Features.Contains(PdbFeature.featMinimalDbgInfo) == true;

        private PDBFile _pdbFile;

        public PDBFileSymCache(PDBFile pdbFile)
        {
            _pdbFile = pdbFile;
        }

        protected override ModCache CreateModCache(ushort imod)
        {
            return new PDBFileModCache(_pdbFile.DBI.Modules[imod]);
        }

        protected override OffSegSym[] InitializeGlobalDataSymbols()
        {
            var globalDataList = new PooledList<OffSegSym>();

            try
            {
                var globals = _pdbFile.GSI.Symbols;

                foreach (var symType in globals)
                    ProcessGlobalDataSymbol(symType, ref globalDataList);

                //When the off/seg is equivalent, we seem to get a different order to DIA due to the different sort
                //algorithm that is used
                globalDataList.Sort((a, b) =>
                {
                    var diff = a.seg.CompareTo(b.seg);

                    if (diff != 0)
                        return diff;

                    return a.off.CompareTo(b.off);
                });

                _globalDataSymbols = globalDataList.ToArray();
                return _globalDataSymbols;
            }
            finally
            {
                globalDataList.Dispose();
            }
        }

        public override void GetModuleSymbols(IMOD imod, out SymTypeList symbols, out ICodeViewModuleAccessor codeViewModuleAccessor)
        {
            var modi = _pdbFile.DBI.Modules[imod];
            symbols = modi.Symbols.List;
            codeViewModuleAccessor = modi.Symbols;
        }
    }
}
