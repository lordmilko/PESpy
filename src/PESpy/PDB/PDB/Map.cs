using System;
using System.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using PESpy.View;

namespace PESpy.PDB
{
    //D = domain (key)
    //R = range (value)
    //H = hasher
    public readonly struct Map<D, R, H> : IValue, IViewable
        where D : unmanaged, IEquatable<D>
        where H : HashClass<D>
    {
        public int Size => chunk.PeekInt32(0);

        public int Capacity => chunk.PeekInt32(4);

        public int PresentWordCount => chunk.PeekInt32(8);

        public NativeSpan<int> PresentWords => chunk.PeekNativeSpan<int>(12, PresentWordCount);

        public int DeletedWordCount => chunk.PeekInt32(12 + (PresentWordCount * 4));

        public NativeSpan<int> DeletedWords => chunk.PeekNativeSpan<int>(16 + (PresentWordCount * 4), DeletedWordCount);

        //The physical entries that exist on disk
        public Entry[] Entries { get; }

        //The virtual entries that exist in memory. Entries are spread out according to whether each slot has a value or not
        private readonly int[] virtualEntries;

        public int Offset => chunk.AbsoluteOffset;

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
            var presentBits = new BitArray(PresentWords.ToArray());

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
                //Suppose the capacity is 6 but the actual size is 4. The capacity being 6 means that at most the 6th bit (at index 5)
                //is set. In-between bits 0-5, there will be two false ones. We need to locate the 4 set bits within the range of possible
                //bits. All of the other bits in the BitArray after the capacity should be false and can be ignored
                if (i < presentBits.Length && presentBits[i]) //In Visual C++ 4, you can have an empty PresentWords
                {
                    entries[bucketIndex] = new Entry(entryChunk, getValue);
                    virtualEntries[i] = bucketIndex;
                    bucketIndex++;
                    entryChunk = entryChunk.Slice(sizeof(D) + valueSize);
                }
            }

            Entries = entries;
            this.virtualEntries = virtualEntries;
        }

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
                    if (i >= deletedBits.Length || !deletedBits[i])
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
            writer.NewStruct(Strings.Map, this, ViewKind.Map, StructSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField("Size", Size);
            s.WriteField("Capacity", Capacity);
            s.WriteField("Present Word Count", PresentWordCount);
            s.WriteField("Present Words", PresentWords);
            s.WriteField("Deleted Word Count", DeletedWordCount);
            s.WriteField("Deleted Words", DeletedWords);

            s.WriteInline(Entries);

            return s.ToArray();
        }

        [DebuggerDisplay("{Key} -> {Value}")]
        public readonly struct Entry : IValue, IViewable
        {
            public D Key => chunk.PeekUnmanaged<D>(0);

            public unsafe R Value => getValue(chunk.Slice(sizeof(D)));

            public int Offset => chunk.AbsoluteOffset;

            internal const int StructSize =
                sizeof(int) + //Key
                sizeof(int);  //Value

            private readonly MemoryChunk chunk;
            private readonly Func<MemoryChunk, R> getValue;

            internal Entry(in MemoryChunk chunk, Func<MemoryChunk, R> getValue)
            {
                this.chunk = chunk;
                this.getValue = getValue;
            }

            void IViewable.WriteGlobals(ViewWriter writer)
            {
                //No globals
            }

            IView? IViewable.WriteStruct(ViewWriter writer) =>
                writer.NewStruct(Strings.Entry, this, default, StructSize);

            IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
            {
                using var s = viewWriter.CreateStruct(parent);

                s.WriteField("Key", Key);
                s.WriteField("Value", Value);

                return s.ToArray();
            }
        }
    }
}
