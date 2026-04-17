using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace PESpy
{
    internal class HandlerMap4DebugView
    {
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public HandlerType4[] Items { get; }

        public HandlerMap4DebugView(HandlerMap4 map)
        {
            Items = map.ToArray();
        }
    }

    [Source(SourceKind.ehdata4_export_h)]
    [DebuggerDisplay("NumEntries = {NumEntries}")]
    [DebuggerTypeProxy(typeof(HandlerMap4DebugView))]
    public readonly unsafe struct HandlerMap4 : IEnumerable<HandlerType4>
    {
        public int NumEntries { get; }

        private readonly byte* _pEntries;
        private readonly int _functionAddress;

        public int Offset { get; }

        internal unsafe HandlerMap4(in MemoryChunk chunk, int functionAddress)
        {
            Offset = chunk.AbsoluteOffset;
            _functionAddress = functionAddress;

            var pData = chunk.Pointer;

            var numEntries = FuncInfo4.ReadUnsigned(ref pData);
            NumEntries = (int) numEntries;
            _pEntries = pData;
        }

        public Enumerator GetEnumerator() => new Enumerator(NumEntries, Offset, _pEntries, _functionAddress);

        IEnumerator<HandlerType4> IEnumerable<HandlerType4>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<HandlerType4>
        {
            private readonly int _numEntries;
            private readonly int _functionAddress;
            private readonly byte* _pStartEntries;
            private readonly int _startOffset;
            private byte* _pEntries;
            private int _index;

            internal Enumerator(int numEntries, int offset, byte* pEntries, int functionAddress)
            {
                _numEntries = numEntries;
                _pStartEntries = pEntries;
                _startOffset = offset;
                _pEntries = pEntries;
                _functionAddress = functionAddress;
            }

            public HandlerType4 Current { get; private set; }

            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                if (_index < _numEntries)
                {
                    Current = new HandlerType4(
                        _startOffset + (int) (_pEntries - _pStartEntries),
                        ref _pEntries,
                        _functionAddress
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
