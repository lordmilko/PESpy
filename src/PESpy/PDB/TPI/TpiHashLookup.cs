using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    //This type implements an open addressing map using linear probing,
    //and also provides the storage for all type indices. Both the entries
    //of the map and the type indices are stored in a single memory mapped
    //region
    internal unsafe class TpiHashLookup : IDisposable
    {
        private const uint EmptyKey = uint.MaxValue;

        struct Entry
        {
            public uint Key;
            public SpanAllocatorHandle Value;
        }

        private Span<Entry> _entries => new Span<Entry>(_pEntries, _capacity);
        private Entry* _pEntries;
        private readonly int _capacity;

        private CV_typ_t* _pTypes;

        private MemoryMappedFileHolder _mmf;

        private readonly uint _mask;

        public int Count { get; private set; }

        public TpiHashLookup(Dictionary<uint, int> buckets, int numTypes, float loadFactory = 0.75f)
        {
            var capacity = 1;

            var targetCapacity = (int) (buckets.Count / loadFactory);

            while (capacity < targetCapacity)
                capacity <<= 1;

            var entriesSize = capacity * sizeof(Entry);

            var mmf = new MemoryMappedFileHolder(entriesSize + (numTypes * sizeof(CV_typ_t)));

            var entries = new Span<Entry>(mmf.Address, capacity);

            for (var i = 0; i < entries.Length; i++)
            {
                ref var e = ref entries[i];

                e.Key = EmptyKey;
            }

            _pTypes = (CV_typ_t*) (mmf.Address + entriesSize);

            _mask = (uint) capacity - 1;

            _pEntries = (Entry*) mmf.Address;
            _capacity = capacity;

            //Now add all the buckets

            var nextTypePos = 0;

            foreach (var kv in buckets)
            {
                var numItemsInBucket = kv.Value;

                Add(kv.Key, new SpanAllocatorHandle(nextTypePos, numItemsInBucket));

                nextTypePos += numItemsInBucket;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(uint key, SpanAllocatorHandle value)
        {
            var i = key & _mask;

            var entries = _entries;

            while (true)
            {
                if (entries[(int) i].Key == EmptyKey)
                {
                    entries[(int) i] = new Entry { Key = key, Value = value };
                    Count++;
                    return;
                }

                i = (i + 1) & _mask;
            }
        }

        public Span<CV_typ_t> GetSpan(SpanAllocatorHandle handle) => new Span<CV_typ_t>(_pTypes + handle.Index, handle.Length);

        public SpanAllocatorHandle this[uint key]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (!TryGetValue(key, out var value))
                    throw new NotImplementedException();

                return value;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetValue(uint key, out SpanAllocatorHandle value)
        {
            var i = key & _mask;

            var entries = _entries;

            while (true)
            {
                ref var e = ref entries[(int) i];

                if (e.Key == EmptyKey)
                    break;

                if (e.Key == key)
                {
                    value = e.Value;
                    return true;
                }

                i = (i + 1) & _mask;
            }

            value = default;
            return false;
        }

        public void Dispose()
        {
            _mmf.Dispose();
        }
    }
}
