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
                    entries[bucketIndex] = new Entry(entryChunk, getValue, valueSize);
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

        int IViewable.NumChildren => 6 + Entries.Length;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField("Size", SizeOffset, Size);
                    break;

                case 1:
                    structWriter.WriteField("Capacity", CapacityOffset, Capacity);
                    break;

                case 2:
                    structWriter.WriteField("Present Word Count", PresentWordCountOffset, PresentWordCount);
                    break;

                case 3:
                    structWriter.WriteField("Present Words", PresentWordsOffset, PresentWords);
                    break;

                case 4:
                    structWriter.WriteField("Deleted Word Count", DeletedWordCountOffset, DeletedWordCount);
                    break;

                case 5:
                    structWriter.WriteField("Deleted Words", DeletedWordsOffset, DeletedWords);
                    break;

                default:
                    structWriter.WriteInline(Entries[index - 6]);
                    break;
            }
        }

        [DebuggerDisplay("{Key} -> {Value}")]
        public readonly unsafe struct Entry : IValue, IViewable
        {
            private const int KeyOffset = 0;

            public D Key => chunk.PeekUnmanaged<D>(KeyOffset);

            public unsafe R Value => getValue(chunk.Slice(sizeof(D)));

            public int Offset => chunk.AbsoluteOffset;

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
                writer.NewStruct(Strings.Entry, this, default, StructSize);

            int IViewable.NumChildren => 2;

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
                            structWriter.WriteStructField("Value", sizeof(D), Unsafe.As<R, SrcHeaderOut>(ref value));
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
