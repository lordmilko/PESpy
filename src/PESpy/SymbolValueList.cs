using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using PESpy.PDB;

namespace PESpy
{
    internal class SymbolValueListDebugView<T>
    {
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public SymbolValue<T>[] Items { get; }

        internal SymbolValueListDebugView(SymbolValueList<T> list)
        {
            Items = list.ToArray();
        }
    }

    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(SymbolValueListDebugView<>))]
    public class SymbolValueList<T> : IEnumerable<SymbolValue<T>>
    {
        private readonly Func<IFile, ICodeViewAccessor, SymType, SymbolValue<T>> _factory;
        private readonly SymTypeCollection _symbols;
        private readonly IFile _file;
        private readonly ICodeViewAccessor _codeViewAccessor;

        public int Count => _symbols.Count;

        internal SymbolValueList(
            Func<IFile, ICodeViewAccessor, SymType, SymbolValue<T>> factory,
            in SymTypeCollection symbols,
            IFile file,
            ICodeViewAccessor codeViewAccessor)
        {
            _factory = factory;
            _symbols = symbols;
            _file = file;
            _codeViewAccessor = codeViewAccessor;
        }

        public SymbolValue<T> this[int index] => _factory(_file, _codeViewAccessor, _symbols[index]);

        public Enumerator GetEnumerator() => new Enumerator(_factory, _symbols, _file, _codeViewAccessor);

        IEnumerator<SymbolValue<T>> IEnumerable<SymbolValue<T>>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<SymbolValue<T>>
        {
            private readonly Func<IFile, ICodeViewAccessor, SymType, SymbolValue<T>> _factory;
            private readonly SymTypeCollection _symbols;
            private readonly IFile _file;
            private readonly ICodeViewAccessor _codeViewAccessor;
            private readonly int _count;

            private int _index;

            internal Enumerator(
                Func<IFile, ICodeViewAccessor, SymType, SymbolValue<T>> factory,
                in SymTypeCollection symbols,
                IFile file,
                ICodeViewAccessor codeViewAccessor)
            {
                _factory = factory;
                _symbols = symbols;
                _file = file;
                _codeViewAccessor = codeViewAccessor;
                _count = symbols.Count;
                _index = -1;
            }

            public SymbolValue<T> Current => _factory(_file, _codeViewAccessor, _symbols[_index]);

            public bool MoveNext()
            {
                if (_index < _count - 1)
                {
                    _index++;
                    return true;
                }

                return false;
            }

            object IEnumerator.Current => Current;

            public void Reset()
            {
            }

            public void Dispose()
            {
            }
        }
    }
}
