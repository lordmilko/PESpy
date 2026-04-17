using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace PESpy
{
    //Represents a HashSet that uses open ended addressing to try and beat the performance of Roslyn's SegmentedHashSet<T>
    //and not incur allocations on the LOH. Coupled with the fact we don't need to copy items out of the SegmentedHashSet<T>
    //into a SegmentedArray<T>, and then sort that SegmentedArray (250ms+ on msedge.dll for all 3 of these steps, vs about 20ms total
    //using UnwindInfoHashSet) this results in 80% faster performance
    [DebuggerDisplay("Count = {Count}")]
    internal unsafe struct UnwindInfoHashSet : IDisposable
    {
        private const uint EmptyKey = 0; //We don't allow storing RVA 0

        //All slots
        internal Span<int> Entries => new Span<int>(_pEntries, Capacity);

        public int Capacity { get; }

        private int* _pEntries;

        private readonly uint _mask;

        public int Count { get; private set; }

        internal UnwindInfoHashSet(int capacity)
        {
            //Capacity must be power of 2. This is a bit twiddling hack to achieve this
            capacity--;
            capacity |= capacity >> 1;
            capacity |= capacity >> 2;
            capacity |= capacity >> 4;
            capacity |= capacity >> 8;
            capacity |= capacity >> 16;
            capacity++;

            var size = capacity * sizeof(int);
            _pEntries = (int*) Marshal.AllocHGlobal(size);
            Capacity = capacity;
            Unsafe.InitBlockUnaligned((void*) _pEntries, (byte) EmptyKey, (uint) size);

            _mask = (uint) capacity - 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Add(int key)
        {
            var i = key & _mask;

            var entries = Entries;

            while (true)
            {
                ref var entry = ref entries[(int) i];

                if (entry == EmptyKey)
                {
                    entry = key;
                    Count++;
                    return true;
                }

                if (entry == key)
                    return false;

                i = (i + 1) & _mask;
            }
        }

        public void Dispose()
        {
            Marshal.FreeHGlobal((IntPtr) _pEntries);
        }
    }
}
