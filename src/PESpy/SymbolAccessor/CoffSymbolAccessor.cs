namespace PESpy
{
    internal class CoffSymbolAccessor : ISymbolAccessor
    {
        private CoffSymbolTable coffSymbolTable;
        private ImageSectionHeader[] sectionHeaders;
        private (ImageSymbol symbols, int length)[] externals;

        internal CoffSymbolAccessor(CoffSymbolTable coffSymbolTable, ImageSectionHeader[] sectionHeaders)
        {
            this.coffSymbolTable = coffSymbolTable;
            this.sectionHeaders = sectionHeaders;
        }

        public SymbolAccessorKind Kind => SymbolAccessorKind.Coff;

        public bool TryGetNameFromAddress(int targetAddress, out SymString name, out int displacement)
        {
            EnsureExternals();

            name = default;
            displacement = default;

            //Note that the symbol value stores the RVA, not the relative offset
            if (!ImageSectionHeader.TryGetSectionAndOffset(sectionHeaders, targetAddress, out var sectionNumber, out _))
                return false;

            var externals = this.externals;

            int low = 0;
            int high = externals.Length - 1;

            while (low <= high)
            {
                var mid = low + (high - low) / 2;

                var current = externals[mid];

                int comparison;

                if (current.symbols.SectionNumber == sectionNumber)
                {
                    //Note that the symbol value stores the RVA, not the relative offset
                    if (targetAddress < current.symbols.Value)
                        comparison = -1; //Before the start of the current entry
                    else if (targetAddress - current.symbols.Value < current.length)
                        comparison = 0; //Within the bounds of the current entry
                    else
                        comparison = 1; //After the bounds of the current entry
                }
                else
                    comparison = sectionNumber - current.symbols.SectionNumber;

                if (comparison == 0)
                {
                    name = current.symbols.Name.Name;
                    displacement = (int) (targetAddress - current.symbols.Value);
                    return true;
                }
                else if (comparison > 0)
                    low = mid + 1;
                else
                    high = mid - 1;
            }

            throw new System.NotImplementedException();
        }

        public bool TryGetAddressFromName(SymString name, out int targetAddress)
        {
            throw new System.NotImplementedException();
        }

        private void EnsureExternals()
        {
            if (externals != null)
                return;

            using var results = new PooledList<ImageSymbol>();

            var symbols = coffSymbolTable.Symbols;

            for (var i = 0; i < symbols.Length; i++)
            {
                ref var symbol = ref symbols[i];

                if (symbol.StorageClass == ImageSymClass.External)
                {
                    switch ((ImageSym) symbol.SectionNumber)
                    {
                        case ImageSym.IMAGE_SYM_UNDEFINED:
                        case ImageSym.IMAGE_SYM_ABSOLUTE:
                        case ImageSym.IMAGE_SYM_DEBUG:
                            continue;
                    }

                    results.Add(symbol);
                }
            }

            //The symbols do seem to be sorted already, but let's just ensure they're sorted for good measure
            results.Sort((a, b) =>
            {
                var diff = a.SectionNumber.CompareTo(b.SectionNumber);

                if (diff != 0)
                    return diff;

                return a.Value.CompareTo(b.Value);
            });

            //dbghelp!CompleteSymbolTable computes the size of each symbol as being the distance between that
            //symbol at the one after it. For the last symbol, the size is 0

            var resultsWithSize = new (ImageSymbol symbol, int length)[results.Count];

            for (var i = 0; i < resultsWithSize.Length - 1; i++)
                resultsWithSize[i] = (results[i], (int) (results[i + 1].Value - results[i].Value));

            resultsWithSize[resultsWithSize.Length - 1] = (results[resultsWithSize.Length - 1], 0);

            externals = resultsWithSize;
        }

        public void Dispose()
        {
        }
    }
}
