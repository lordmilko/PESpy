using System;
using PESpy.View;

namespace PESpy
{
    internal class CoffSymbolAccessor : ISymbolAccessor
    {
        private readonly CoffSymbolTable _coffSymbolTable;
        private readonly ImageSectionHeader[] _sectionHeaders;
        private (ImageSymbol symbol, int length)[] _externals;

        internal (ImageSymbol symbol, int length)[] Externals
        {
            get
            {
                if (_externals == null)
                    EnsureExternals();

                return _externals;
            }
        }

        internal CoffSymbolAccessor(CoffSymbolTable coffSymbolTable, ImageSectionHeader[] sectionHeaders)
        {
            _coffSymbolTable = coffSymbolTable;
            _sectionHeaders = sectionHeaders;
        }

        public SymbolAccessorKind Kind => SymbolAccessorKind.Coff;

        public bool TryGetNameFromAddress(int targetAddress, out SymString name, out int displacement)
        {
            var index = BinarySearchSymbols(targetAddress);

            if (index == -1)
            {
                name = default;
                displacement = default;
                return false;
            }

            var current = _externals[index];

            name = current.symbol.Name.Name;
            displacement = (int) (targetAddress - current.symbol.Value);
            return true;
        }

        public bool TryGetAddressFromName(FixedUtf8String name, out int targetAddress)
        {
            throw new NotImplementedException();
        }

        public bool TryGetLengthFromAddress(int targetAddress, ISectionDataAccessor sectionDataAccessor, out int length)
        {
            var externals = Externals;

            var index = BinarySearchSymbols(targetAddress);

            if (index == -1)
            {
                length = default;
                return false;
            }

            var current = _externals[index];
            var displacement = targetAddress - (int) current.symbol.Value;
            length = current.length - displacement;
            return true;
        }

        private int BinarySearchSymbols(int targetAddress)
        {
            //Note that the symbol value stores the RVA, not the relative offset
            if (!ImageSectionHeader.TryGetSectionAndOffset(_sectionHeaders, targetAddress, out var sectionNumber, out _))
                return -1;

            var externals = Externals;

            int low = 0;
            int high = externals.Length - 1;

            while (low <= high)
            {
                var mid = low + (high - low) / 2;

                var current = externals[mid];

                int comparison;

                if (current.symbol.SectionNumber == sectionNumber)
                {
                    //Note that the symbol value stores the RVA, not the relative offset
                    if (targetAddress < current.symbol.Value)
                        comparison = -1; //Before the start of the current entry
                    else if (targetAddress - current.symbol.Value < current.length)
                        comparison = 0; //Within the bounds of the current entry
                    else
                        comparison = 1; //After the bounds of the current entry
                }
                else
                    comparison = sectionNumber - current.symbol.SectionNumber;

                if (comparison == 0)
                {
                    return mid;
                }
                else if (comparison > 0)
                    low = mid + 1;
                else
                    high = mid - 1;
            }

            return -1;
        }

        private void EnsureExternals()
        {
            if (_externals != null)
                return;

            using var results = new PooledList<ImageSymbol>();

            var symbols = _coffSymbolTable.Symbols;

            for (var i = 0; i < symbols.Length; i++)
            {
                ref var symbol = ref symbols[i];

                if (symbol.StorageClass == IMAGE_SYM_CLASS.IMAGE_SYM_CLASS_EXTERNAL)
                {
                    switch ((IMAGE_SYM) symbol.SectionNumber)
                    {
                        case IMAGE_SYM.IMAGE_SYM_UNDEFINED:
                        case IMAGE_SYM.IMAGE_SYM_ABSOLUTE:
                        case IMAGE_SYM.IMAGE_SYM_DEBUG:
                            continue;
                    }

                    switch (symbol.BasicType)
                    {
                        case IMAGE_SYM_TYPE.IMAGE_SYM_TYPE_NULL:
                            break;

                        case IMAGE_SYM_TYPE.IMAGE_SYM_TYPE_STRUCT:
                            continue;

                        default:
                            throw new NotImplementedException();
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

            if (resultsWithSize.Length > 0)
                resultsWithSize[resultsWithSize.Length - 1] = (results[resultsWithSize.Length - 1], 0);

            _externals = resultsWithSize;
        }

        public void Dispose()
        {
        }
    }
}
