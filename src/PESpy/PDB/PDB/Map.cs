using System;
using System.Collections;
using PESpy.View;

namespace PESpy.PDB
{
    public readonly struct Map : IValue, IViewable
    {
        public int Size => chunk.PeekInt32(0);

        public int Capacity => chunk.PeekInt32(4);

        public int PresentWordCount => chunk.PeekInt32(8);

        public NativeSpan<int> PresentWords => chunk.PeekNativeSpan<int>(12, PresentWordCount);

        public int DeletedWordCount => chunk.PeekInt32(12 + (PresentWordCount * 4));

        public NativeSpan<int> DeletedWords => chunk.PeekNativeSpan<int>(16 + (PresentWordCount * 4), DeletedWordCount);

        public Entry[] Entries { get; }

        public int Offset => chunk.AbsoluteOffset;

        public int StructSize
        {
            get
            {
                var size = 16 + (PresentWordCount * 4) + (DeletedWordCount * 4);

                size += (Entries.Length * 8);

                return size;
            }
        }

        private readonly MemoryChunk chunk;

        internal Map(in MemoryChunk chunk)
        {
            this.chunk = chunk;

            var bucketIndex = 0;

            Entries = null!;

            var size = Size;
            var capacity = Capacity;
            var presentBits = new BitArray(PresentWords.ToArray());

            var entries = size == 0 ? Array.Empty<Entry>() : new Entry[size];

            var entryChunk = chunk.Slice(16 + (PresentWordCount * 4) + (DeletedWordCount * 4));

            for (var i = 0; i < capacity; i++)
            {
                //Suppose the capacity is 6 but the actual size is 4. The capacity being 6 means that at most the 6th bit (at index 5)
                //is set. In-between bits 0-5, there will be two false ones. We need to locate the 4 set bits within the range of possible
                //bits. All of the other bits in the BitArray after the capacity should be false and can be ignored
                if (i < presentBits.Length && presentBits[i]) //In Visual C++ 4, you can have an empty PresentWords
                {
                    entries[bucketIndex] = new Entry(entryChunk);
                    bucketIndex++;
                    entryChunk = entryChunk.Slice(Entry.StructSize);
                }
            }

            Entries = entries;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct("Map", this, default, StructSize);

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

        public readonly struct Entry : IValue, IViewable
        {
            public int Key => chunk.PeekInt32(0);

            public int Value => chunk.PeekInt32(4);

            public int Offset => chunk.AbsoluteOffset;

            internal const int StructSize =
                sizeof(int) + //Key
                sizeof(int);  //Value

            private readonly MemoryChunk chunk;

            internal Entry(in MemoryChunk chunk)
            {
                this.chunk = chunk;
            }

            void IViewable.WriteGlobals(ViewWriter writer)
            {
                //No globals
            }

            IView? IViewable.WriteStruct(ViewWriter writer) =>
                writer.NewStruct("Entry", this, default, StructSize);

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
