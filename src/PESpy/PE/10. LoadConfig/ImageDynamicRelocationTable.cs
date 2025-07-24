using System.Collections.Generic;
using System.Diagnostics;
using PESpy.Native;
using PESpy.View;

namespace PESpy
{
    public readonly struct ImageDynamicRelocationTable : IValue, IViewable
    {
        public int Version { get; }

        public int Size { get; }

        public ImageDynamicRelocation[] DynamicRelocations { get; }

        public int Offset { get; }

        internal int StructSize =>
            sizeof(int) + //Version
            sizeof(int) + //Size
            Size;

#if PEFAST
        internal ImageDynamicRelocationTable(in MemoryChunk chunk)
        {
            Offset = chunk.AbsoluteOffset;

            //Eagerly load; I presume all the info you want is in the Dynamic Relocations

            //Is it a V1 or V2 structure?
            Version = chunk.PeekInt32(0);

            if (Version == 1)
            {
                Size = chunk.PeekInt32(4); //The size that all of the ImageDynamicRelocation entries occupy. Note that the "Symbol" member of each relocation is 8 bytes in x64

                //ImageDynamicRelocation contains heaps of dynamically sized structures, so we can't assume how many
                //entries we'll have

                using var dynamicRelocations = new PooledList<ImageDynamicRelocation>();

                var end = Size + 8;
                var read = 8;

                while (read < end)
                {
                    var item = new ImageDynamicRelocation(chunk.Slice(read));
                    Debug.Assert(item.BaseRelocSize != 0);
                    read += item.BaseRelocSize + 4 + chunk.PointerSize;
                    dynamicRelocations.Add(item);
                }

                Debug.Assert(read == end);

                DynamicRelocations = dynamicRelocations.ToArray();
            }
            else
            {
                //Haven't found an assembly that uses version 2 yet
                Debug.Assert(false, $"Don't know how to handle a {nameof(ImageDynamicRelocationTable)} with version {Version}");
                Size = default;
                DynamicRelocations = default!;
            }
        }
#else
        internal ImageDynamicRelocationTable(IFileReader reader, PEFile peFile)
        {
            Offset = (int) reader.Position;

            //Is it a V1 or V2 structure?
            Version = reader.ReadInt32();

            if (Version == 1)
            {
                Size = reader.ReadInt32(); //The size that all of the ImageDynamicRelocation entries occupy. Note that the "Symbol" member of each relocation is 8 bytes in x64

                //ImageDynamicRelocation contains heaps of dynamically sized structures, so we can't assume how many
                //entries we'll have

                using var dynamicRelocations = new PooledList<ImageDynamicRelocation>();

                var end = reader.Position + Size;

                reader.FillBuffer(Size - 8); //We've already read 8 bytes

                while (reader.Position < end)
                    dynamicRelocations.Add(new ImageDynamicRelocation(reader, peFile));

                Debug.Assert(reader.Position == end);

                DynamicRelocations = dynamicRelocations.ToArray();
            }
            else
            {
                //Haven't found an assembly that uses version 2 yet
                Debug.Assert(false, $"Don't know how to handle a {nameof(ImageDynamicRelocationTable)} with version {Version}");
                Size = default;
                DynamicRelocations = default;
            }
        }
#endif

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.IMAGE_DYNAMIC_RELOCATION_TABLE, this, ViewKind.ImageDynamicRelocationTable, StructSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(Version), Version);

            if (Version == 1)
            {
                s.WriteField(nameof(Size), Size);

                s.WriteInline(DynamicRelocations);
            }
            else
            {
                Debug.Assert(false, $"Don't know how to handle a {nameof(ImageDynamicRelocationTable)} with version {Version}");
            }

            return s.ToArray();
        }
    }
}
