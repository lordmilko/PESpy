using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace PESpy
{
    internal class SepIPtoStateMap4DebugView
    {
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public SepIPtoStateMapEntry4[] Items { get; }

        public SepIPtoStateMap4DebugView(SepIPtoStateMap4 map)
        {
            Items = map.ToArray();
        }
    }

    [Source(SourceKind.ehdata4_export_h)]
    [DebuggerDisplay("NumEntries = {NumEntries}")]
    [DebuggerTypeProxy(typeof(SepIPtoStateMap4DebugView))]
    public readonly unsafe struct SepIPtoStateMap4 : IEnumerable<SepIPtoStateMapEntry4>
    {
        public int NumEntries { get; }

        private readonly byte* _pEntries;
        private readonly PEFile _peFile;
        private readonly int _functionAddress;

        public int Offset { get; }

        internal unsafe SepIPtoStateMap4(in MemoryChunk chunk, int functionAddress)
        {
            Offset = chunk.AbsoluteOffset;
            _peFile = chunk.PEFile();
            _functionAddress = functionAddress;

            var pData = chunk.Pointer;

            var numEntries = FuncInfo4.ReadUnsigned(ref pData);
            NumEntries = (int) numEntries;
            _pEntries = pData;
        }

        public Enumerator GetEnumerator() => new Enumerator(NumEntries, _pEntries, _functionAddress, _peFile);

        IEnumerator<SepIPtoStateMapEntry4> IEnumerable<SepIPtoStateMapEntry4>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<SepIPtoStateMapEntry4>
        {
            private readonly int _numEntries;
            private readonly int _functionAddress;
            private readonly PEFile _peFile;
            private byte* _pEntries;
            private int _index;

            internal Enumerator(int numEntries, byte* pEntries, int functionAddress, PEFile peFile)
            {
                _numEntries = numEntries;
                _pEntries = pEntries;
                _functionAddress = functionAddress;
                _peFile = peFile;
            }

            public SepIPtoStateMapEntry4 Current { get; private set; }

            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                if (_index < _numEntries)
                {
                    Current = new SepIPtoStateMapEntry4(_peFile, ref _pEntries, _functionAddress);
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
