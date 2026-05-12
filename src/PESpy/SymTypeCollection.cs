using PESpy.PDB;

namespace PESpy
{
    internal readonly unsafe struct SymTypeCollection
    {
        private readonly SymType* _symbols;

        public int Count { get; }

        internal SymTypeCollection(SymType* symbols, int count)
        {
            _symbols = symbols;
            Count = count;
        }

        public SymType this[int index] => _symbols[index];

        public Enumerator GetEnumerator() => new Enumerator(_symbols, Count);

        public struct Enumerator
        {
            private readonly SymType* _symbols;
            private readonly int _count;
            private int _index;

            internal Enumerator(SymType* symbols, int count)
            {
                _symbols = symbols;
                _count = count;
                _index = -1;
            }

            public SymType Current => _symbols[_index];

            public bool MoveNext()
            {
                if (_index < _count - 1)
                {
                    _index++;
                    return true;
                }

                return false;
            }
        }
    }
}
