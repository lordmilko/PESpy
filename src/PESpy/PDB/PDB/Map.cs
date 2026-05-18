using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    //D = domain (key)
    //R = range (value)
    //H = hasher
    public readonly struct Map<D, R, H> : IViewableValue
        where D : unmanaged, IEquatable<D>
        where H : HashClass<D>
    {
        private const int SizeOffset = 0;
        private const int CapacityOffset = 4;
        private const int PresentWordCountOffset = 8;
        private const int PresentWordsOffset = 12;
        private int DeletedWordCountOffset => 12 + (PresentWordCount * 4);
        private int DeletedWordsOffset => 16 + (PresentWordCount * 4);

        public int Size => chunk.PeekInt32(SizeOffset);

        public int Capacity => chunk.PeekInt32(CapacityOffset);

        public int PresentWordCount => chunk.PeekInt32(PresentWordCountOffset);

        public NativeSpan<int> PresentWords => chunk.PeekNativeSpan<int>(PresentWordsOffset, PresentWordCount);

        public int DeletedWordCount => chunk.PeekInt32(DeletedWordCountOffset);

        public NativeSpan<int> DeletedWords => chunk.PeekNativeSpan<int>(DeletedWordsOffset, DeletedWordCount);

        //The physical entries that exist on disk
        public Entry[] Entries { get; }

        //The virtual entries that exist in memory. Entries are spread out according to whether each slot has a value or not
        private readonly int[] virtualEntries;

        public long Offset => chunk.AbsoluteOffset;

        public unsafe int StructSize
        {
            get
            {
                var size = 16 + (PresentWordCount * sizeof(int)) + (DeletedWordCount * sizeof(int));

                size += (Entries.Length * (sizeof(D) + valueSize));

                return size;
            }
        }

        private readonly MemoryChunk chunk;
        private readonly H hasher;
        private readonly int valueSize;

        internal unsafe Map(in MemoryChunk chunk, Func<MemoryChunk, R> getValue, int valueSize, H hasher)
        {
            this.chunk = chunk;
            this.valueSize = valueSize;
            this.hasher = hasher;
            this.virtualEntries = default;

            var bucketIndex = 0;

            Entries = null!;

            var size = Size;
            var capacity = Capacity;

            var presentWords = PresentWords;

            Entry[] entries;
            int[] virtualEntries;

            if (size == 0)
            {
                entries = Array.Empty<Entry>();
                virtualEntries = Array.Empty<int>();
            }
            else
            {
                entries = new Entry[size];
                virtualEntries = new int[capacity];

                fixed (int* b = virtualEntries)
                    Unsafe.InitBlockUnaligned((byte*) b, byte.MaxValue, (uint) capacity * sizeof(int));
            }

            var entryChunk = chunk.Slice(16 + (PresentWordCount * 4) + (DeletedWordCount * 4));

            for (var i = 0; i < capacity; i++)
            {
                int wordIndex = i >> 5; // divide by 32
                int bitIndex = i & 31;
                var isPresent = wordIndex < presentWords.Length && (presentWords[wordIndex] & (1 << bitIndex)) != 0;

                //Suppose the capacity is 6 but the actual size is 4. The capacity being 6 means that at most the 6th bit (at index 5)
                //is set. In-between bits 0-5, there will be two false ones. We need to locate the 4 set bits within the range of possible
                //bits. All of the other bits in the present words after the capacity should be false and can be ignored
                if (isPresent) //In Visual C++ 4, you can have an empty PresentWords
                {
                    entries[bucketIndex] = new Entry(entryChunk, getValue, valueSize);
                    virtualEntries[i] = bucketIndex;
                    bucketIndex++;
                    entryChunk = entryChunk.Slice(sizeof(D) + valueSize);
                }
            }

            Entries = entries;
            this.virtualEntries = virtualEntries;
        }

        public bool TryFind(D key, out R value, out int physicalEntryIndex)
        {
            var n = Capacity;

            /* There are several different methods of hashing the key
             *
             * | Enum        | Typedef                                              | Description
             * |-------------|------------------------------------------------------|--------------|
             * | hcCast (0)  | HashClass<unsigned long, hcCast>              HcNi   | standard version of the HashClass merely casts the object to a HASH. by convention, this one is always HashClass<H,hcCast>
             * | hcSig (1)   |                                                      | SIG is an unsigned long (like NI!) and needs a different hash function
             * | hcKey (2)   |                                                      | KEY is an unsigned long (like NI!) and needs a different hash function
             * | hcMD5 (4)   |                                                      | simple hash class which hashes using MD5 hash
             * | hcCRC (5)   |                                                      | hash class with CRC hash
             * | hcLCast (6) | HashClass<unsigned long, hcLCast>             LHcNi  | casting long hash
             * |             | HashClass<UINT_PTR, hcLCast>                  HcPtr  |
             * | hcLPtr (7)  | HashClass2<void *, hcLPtr, cbitsTruncateHash> HcLPtr | casting long pointer hash
             *
             * Source uses the following map
             *
             * Map<unsigned long,SHO,pdb_internal::HashClass<unsigned long,0>,void,CriticalSectionNop>::find
             *
             * D = unsigned long ("domain")
             * R = SHO
             * H = pdb_internal::HashClass<unsigned long,0> = HcNi - casts the "domain" D to a HASH (short)
             * C = void
             * CS = CriticalSectionNop
             */

            var h = (int) hasher.GetHashableValue(key) % n;
            var i = h;

            var presentWords = PresentWords;
            var deletedWords = DeletedWords;

            do
            {
                var wordIndex = i >> 5;
                var bitIndex = i & 31;
                var bit = 1 << bitIndex;

                var isPresent = wordIndex < presentWords.Length && (presentWords[wordIndex] & bit) != 0;

                if (isPresent)
                {
                    var j = virtualEntries[i];
                    var entry = Entries[j];

                    if (hasher.Equals(entry.Key, key))
                    {
                        value = entry.Value;
                        physicalEntryIndex = j;
                        return true;
                    }
                }
                else
                {
                    var isDeleted = wordIndex < deletedWords.Length && (deletedWords[wordIndex] & bit) != 0;

                    if (!isDeleted)
                        break;
                }

                i = (i + 1 < n) ? i + 1 : 0;
            } while (i != h);

            value = default;
            physicalEntryIndex = default;
            return false;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.Map, StructSize);

        int IViewable.NumChildren() => throw StructWriter.GetEagerLoadOnlyException(); 

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            //Both PresentWords and DeletedWords can be missing, which makes this a bit too complicated
            //and we should just write it eagerly

            if (index != -1)
                throw StructWriter.GetEagerLoadOnlyException();

            using var s = structWriter.CreateEagerWriter();

            s.WriteField("Size", Size);
            s.WriteField("Capacity", Capacity);
            s.WriteField("Present Word Count", PresentWordCount);

            if (PresentWords.Length > 0)
                s.WriteField("Present Words", PresentWords);

            s.WriteField("Deleted Word Count", DeletedWordCount);

            if (DeletedWords.Length > 0)
                s.WriteField("Deleted Words", DeletedWords);

            foreach (var entry in Entries)
                s.WriteInline(entry);

            structWriter.EagerFields = s.ToArray();
        }

        [DebuggerDisplay("{Key} -> {Value}")]
        public readonly unsafe struct Entry : IViewableValue
        {
            private const int KeyOffset = 0;

            public D Key => chunk.PeekUnmanaged<D>(KeyOffset);

            public unsafe R Value => getValue(chunk.Slice(sizeof(D)));

            public long Offset => chunk.AbsoluteOffset;

            internal int StructSize =>
                sizeof(D) +
                valueSize;

            private readonly MemoryChunk chunk;
            private readonly Func<MemoryChunk, R> getValue;
            private readonly int valueSize;

            internal Entry(in MemoryChunk chunk, Func<MemoryChunk, R> getValue, int valueSize)
            {
                this.chunk = chunk;
                this.getValue = getValue;
                this.valueSize = valueSize;
            }

            void IViewable.WriteGlobals(ViewWriter writer)
            {
                //No globals
            }

            IView? IViewable.WriteStruct(ViewWriter writer) =>
                writer.NewStruct(this, ViewKind.Map_Entry, StructSize);

            int IViewable.NumChildren() => 2;

            void IViewable.WriteChild(int index, ref StructWriter structWriter)
            {
                switch (index)
                {
                    case 0:
                        var key = Key;

                        if (typeof(D) == typeof(int))
                            structWriter.WriteField("Key", 0, Unsafe.As<D, int>(ref key));
                        else if (typeof(D) == typeof(NI))
                            structWriter.WriteField("Key", 0, Unsafe.As<D, NI>(ref key));
                        else
                            Debug.Assert(false);
                        break;

                    case 1:
                        var value = Value;

                        if (typeof(R) == typeof(SN))
                            structWriter.WriteField("Value", sizeof(D), Unsafe.As<R, SN>(ref value), valueSize);
                        else if (typeof(R) == typeof(SrcHeaderOut))
                            structWriter.WriteStructField("Value", Unsafe.As<R, SrcHeaderOut>(ref value));
                        else if (typeof(R) == typeof(CV_typ_t))
                            structWriter.WriteField("Value", sizeof(D), Unsafe.As<R, CV_typ_t>(ref value));
                        else
                            Debug.Assert(false);
                        break;

                    default:
                        throw new IndexOutOfRangeException();
                }
            }
        }
    }
}
