using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using PESpy.View;

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
    public readonly unsafe struct SepIPtoStateMap4 : IEnumerable<SepIPtoStateMapEntry4>, IViewableValue
    {
        public int NumEntries { get; }

        private readonly byte* _pData; //Enumerator needs to know how many bytes it took to store NumEntries
        private readonly PEFile _peFile;
        private readonly int _functionAddress;

        public int Offset { get; }

        internal int StructSize =>
            FuncInfo4.GetLength((uint) NumEntries) +
            (NumEntries * SepIPtoStateMapEntry4.StructSize);

        //For use by ViewProvider only
        internal SepIPtoStateMap4(in MemoryChunk chunk) : this(chunk, 0)
        {
        }

        internal unsafe SepIPtoStateMap4(in MemoryChunk chunk, int functionAddress)
        {
            Offset = chunk.AbsoluteOffset;
            _peFile = chunk.PEFile();
            _functionAddress = functionAddress;

            var pData = chunk.Pointer;
            _pData = pData;

            var numEntries = FuncInfo4.ReadUnsigned(ref pData);
            NumEntries = (int) numEntries;
        }

        public SepIPtoStateMapEntry4 this[int index]
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
            writer.NewStruct(this, ViewKind.SepIPtoStateMap4, StructSize);

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

        public Enumerator GetEnumerator() => new Enumerator(NumEntries, Offset, _pData, _functionAddress, _peFile);

        IEnumerator<SepIPtoStateMapEntry4> IEnumerable<SepIPtoStateMapEntry4>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<SepIPtoStateMapEntry4>
        {
            private readonly int _numEntries;
            private readonly byte* _pStartData;
            private readonly int _parentOffset;
            private readonly int _functionAddress;
            private readonly PEFile _peFile;
            private byte* _pEntries;
            private int _index;

            internal Enumerator(int numEntries, int parentOffset, byte* pData, int functionAddress, PEFile peFile)
            {
                _numEntries = numEntries;
                _pStartData = pData;
                _parentOffset = parentOffset;
                _functionAddress = functionAddress;
                _peFile = peFile;

                //Skip over numEntries; this ensures _pStartData - pData will include the length
                //of numEntries
                FuncInfo4.ReadUnsigned(ref pData);

                _pEntries = pData;
            }

            public SepIPtoStateMapEntry4 Current { get; private set; }

            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                if (_index < _numEntries)
                {
                    //Strictly speaking the size of each SepIPtoStateMapEntry4 is known, but we also need to account
                    //for the size of NumEntries
                    Current = new SepIPtoStateMapEntry4(
                        _parentOffset + (int) (_pEntries - _pStartData),
                        _peFile,
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
