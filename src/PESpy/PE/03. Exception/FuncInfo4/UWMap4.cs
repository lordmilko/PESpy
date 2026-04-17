using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace PESpy
{
    internal class UWMap4DebugView
    {
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public UnwindMapEntry4[] Items { get; }

        public UWMap4DebugView(UWMap4 map)
        {
            Items = map.ToArray();
        }
    }

    [Source(SourceKind.ehdata4_export_h)]
    [DebuggerDisplay("NumEntries = {NumEntries}")]
    [DebuggerTypeProxy(typeof(UWMap4DebugView))]
    public readonly unsafe struct UWMap4 : IEnumerable<UnwindMapEntry4>
    {
        public int NumEntries { get; }

        private readonly byte* _pEntries;

        public int Offset { get; }

        internal unsafe UWMap4(in MemoryChunk chunk, int functionAddress)
        {
            Offset = chunk.AbsoluteOffset;

            var pData = chunk.Pointer;

            var numEntries = FuncInfo4.ReadUnsigned(ref pData);
            NumEntries = (int) numEntries;
            _pEntries = pData;
        }

        public Enumerator GetEnumerator() => new Enumerator(NumEntries, Offset, _pEntries);

        IEnumerator<UnwindMapEntry4> IEnumerable<UnwindMapEntry4>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<UnwindMapEntry4>
        {
            private readonly int _numEntries;
            private readonly byte* _pStartEntries;
            private readonly int _startOffset;
            private byte* _pEntries;
            private int _index;

            internal Enumerator(int numEntries, int offset, byte* pEntries)
            {
                _numEntries = numEntries;
                _pStartEntries = pEntries;
                _startOffset = offset;
                _pEntries = pEntries;
            }

            public UnwindMapEntry4 Current { get; private set; }

            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                if (_index < _numEntries)
                {
                    Current = new UnwindMapEntry4(
                        _startOffset + (int) (_pEntries - _pStartEntries),
                        ref _pEntries
                    );
                    _index++;
                    return true;
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
