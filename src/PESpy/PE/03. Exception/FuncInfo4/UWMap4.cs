using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using PESpy.View;

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
    public readonly unsafe struct UWMap4 : IEnumerable<UnwindMapEntry4>, IViewableValue
    {
        public int NumEntries { get; }

        private readonly byte* _pData; //Enumerator needs to know how many bytes it took to store NumEntries

        public int Offset { get; }

        internal int StructSize
        {
            get
            {
                var pData = _pData;

                //NumEntries
                FuncInfo4.ReadUnsigned(ref pData);

                //This ctor doesn't do anything complex so this is OK
                for (var i = 0; i < NumEntries; i++)
                    new UnwindMapEntry4(0, ref pData);

                return (int) (pData - _pData);
            }
        }

        //For use by ViewProvider only
        internal UWMap4(in MemoryChunk chunk) : this(chunk, 0)
        {
        }

        internal unsafe UWMap4(in MemoryChunk chunk, int functionAddress)
        {
            Offset = chunk.AbsoluteOffset;

            var pData = chunk.Pointer;
            _pData = pData;

            var numEntries = FuncInfo4.ReadUnsigned(ref pData);
            NumEntries = (int) numEntries;
        }

        public UnwindMapEntry4 this[int index]
        {
            get
            {
                var i = 0;

                var enumerator = GetEnumerator();

                while (enumerator.MoveNext())
                {
                    if (index == i)
                        return enumerator.Current;

                    i++;
                }

                throw new IndexOutOfRangeException();
            }
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            foreach (var entry in this)
                writer.RelayGlobals(entry);
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.UWMap4, this, ViewKind.UWMap4, StructSize);

        int IViewable.NumChildren() =>
            throw StructWriter.GetEagerLoadOnlyException();

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            if (index != -1)
                throw StructWriter.GetEagerLoadOnlyException();

            using var s = structWriter.CreateEagerWriter();

            var pData = _pData;
            FuncInfo4.ReadUnsigned(ref pData);

            s.WriteField(nameof(NumEntries), NumEntries, (int) (pData - _pData));

            foreach (var item in this)
                s.WriteInline(item);

            structWriter.EagerFields = s.ToArray();
        }

        public Enumerator GetEnumerator() => new Enumerator(NumEntries, Offset, _pData);

        IEnumerator<UnwindMapEntry4> IEnumerable<UnwindMapEntry4>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<UnwindMapEntry4>
        {
            private readonly int _numEntries;
            private readonly byte* _pStartData;
            private readonly int _parentOffset;
            private byte* _pEntries;
            private int _index;

            internal Enumerator(int numEntries, int offset, byte* pData)
            {
                _numEntries = numEntries;
                _pStartData = pData;
                _parentOffset = offset;

                //Skip over numEntries; this ensures _pStartData - pData will include the length
                //of numEntries
                FuncInfo4.ReadUnsigned(ref pData);

                _pEntries = pData;
            }

            public UnwindMapEntry4 Current { get; private set; }

            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                if (_index < _numEntries)
                {
                    Current = new UnwindMapEntry4(
                        _parentOffset + (int) (_pEntries - _pStartData),
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
