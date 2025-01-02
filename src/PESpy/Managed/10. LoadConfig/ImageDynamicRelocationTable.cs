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

        internal ImageDynamicRelocationTable(IFileReader reader, PEFile peFile)
        {
            //We already read Version
            Offset = (int) reader.Position - 4;

            //Is it a V1 or V2 structure?
            Version = reader.ReadInt32();

            if (Version == 1)
            {
                Size = reader.ReadInt32(); //The size that all of the ImageDynamicRelocation entries occupy. Note that the "Symbol" member of each relocation is 8 bytes in x64

                //ImageDynamicRelocation contains heaps of dynamically sized structures, so we can't assume how many
                //entries we'll have

                var dynamicRelocations = new List<ImageDynamicRelocation>();

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

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct(nameof(IMAGE_DYNAMIC_RELOCATION_TABLE), this, ViewKind.ImageDynamicRelocationTable);

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
        }
    }
}
