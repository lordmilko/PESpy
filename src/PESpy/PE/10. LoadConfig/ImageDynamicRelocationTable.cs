using System;
using System.Collections.Generic;
using System.Diagnostics;
using PESpy.Native;
using PESpy.View;

namespace PESpy
{
    public readonly struct ImageDynamicRelocationTable : IValue, IViewable
    {
        private const int VersionOffset = 0;
        private const int SizeOffset = 4;

        public int Version { get; }

        public int Size { get; }

        public ImageDynamicRelocation[] DynamicRelocations { get; }

        public long Offset { get; }

        internal int StructSize =>
            sizeof(int) + //Version
            sizeof(int) + //Size
            Size;

        internal ImageDynamicRelocationTable(in MemoryChunk chunk)
        {
            Offset = chunk.AbsoluteOffset;

            //Eagerly load; I presume all the info you want is in the Dynamic Relocations

            //Is it a V1 or V2 structure?
            Version = chunk.PeekInt32(VersionOffset);

            if (Version == 1)
            {
                Size = chunk.PeekInt32(SizeOffset); //The size that all of the ImageDynamicRelocation entries occupy. Note that the "Symbol" member of each relocation is 8 bytes in x64

                //ImageDynamicRelocation contains heaps of dynamically sized structures, so we can't assume how many
                //entries we'll have

                using var dynamicRelocations = new ValueList<ImageDynamicRelocation>();

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

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.ImageDynamicRelocationTable, StructSize);

        int IViewable.NumChildren()
        {
            if (Version == 1)
                return 2 + DynamicRelocations.Length;

            throw GetUnsupportedVersionException();
        }

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(Version), VersionOffset, Version);
                    break;

                case 1:
                    if (Version == 1)
                        structWriter.WriteField(nameof(Size), SizeOffset, Size);
                    else
                        throw GetUnsupportedVersionException();

                    break;

                default:
                    if (Version == 1)
                        structWriter.WriteInline(DynamicRelocations[index - 2]);
                    else
                        throw GetUnsupportedVersionException();

                    break;
            }
        }

        private Exception GetUnsupportedVersionException() =>
            new NotImplementedException($"Don't know how to handle a {nameof(ImageDynamicRelocationTable)} with version {Version}");
    }
}
