using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace PESpy.ISO
{
    internal class DirectoryEntryIteratorDebugView
    {
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public DirectoryRecord[] Items { get; }

        internal DirectoryEntryIteratorDebugView(DirectoryEntryIterator iterator)
        {
            Items = iterator.ToArray();
        }
    }

    //Provides facilities for enumerating the contents of a DirectoryRecord without allocating
    [DebuggerTypeProxy(typeof(DirectoryEntryIteratorDebugView))]
    public readonly unsafe struct DirectoryEntryIterator : IEnumerable<DirectoryRecord>
    {
        private readonly ISOContext _context;
        private readonly uint _locationOfExtent;
        private readonly uint _dataLength;

        internal DirectoryEntryIterator(ISOContext context, uint locationOfExtent, uint dataLength)
        {
            _context = context;
            _locationOfExtent = locationOfExtent;
            _dataLength = dataLength;
        }

        public Enumerator GetEnumerator() => new Enumerator(_context, _locationOfExtent, _dataLength);

        IEnumerator<DirectoryRecord> IEnumerable<DirectoryRecord>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public unsafe struct Enumerator : IEnumerator<DirectoryRecord>
        {
            private readonly ISOContext _context;
            private readonly uint _locationOfExtent;
            private int _read; //Total amount of complete blocks read
            private readonly uint _dataLength; //Total number of space across all blocks

            private byte* _pDataStart;

            private ISOByteReader reader;

            internal Enumerator(ISOContext context, uint locationOfExtent, uint dataLength)
            {
                _context = context;
                _locationOfExtent = locationOfExtent;
                _pDataStart = _pDataStart = _context.pISO + (_context.LogicalBlockSize * locationOfExtent); ;
                _dataLength = dataLength;
                reader = new ISOByteReader(_pDataStart, context.LogicalBlockSize);
            }

            public DirectoryRecord Current { get; private set; }

            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                while (_read < _dataLength)
                {
                    while (reader.Remaining > 0 && *reader._buffer != 0)
                    {
                        Current = new DirectoryRecord(ref reader, _context, _locationOfExtent);
                        return true;
                    }

                    _read += _context.LogicalBlockSize;
                    reader = new ISOByteReader(_pDataStart + _read, _context.LogicalBlockSize);
                }

                return false;
            }

            public void Reset()
            {
            }

            public void Dispose()
            {
            }
        }
    }
}
