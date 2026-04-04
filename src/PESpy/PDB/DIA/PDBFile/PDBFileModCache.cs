namespace PESpy.PDB.DIA
{
    internal class PDBFileModCache : ModCache
    {
        protected override SymTypeList Symbols => _modi.Symbols.List;

        private IModi _modi;

        internal PDBFileModCache(IModi modi)
        {
            _modi = modi;
        }
    }
}
