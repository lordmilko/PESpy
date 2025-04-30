using System;
using System.Collections.Generic;
using System.Linq;
using PESpy.View;

namespace PESpy.PDB
{
    public class FileInfo : IValue, IViewable
    {
        //cMods
        public short NumModules => chunk.PeekInt16(0);

        //cRefs
        public short NumSourceFiles => chunk.PeekInt16(2);

        public Span<short> ModuleIndices => chunk.PeekSpan<short>(4, NumModules);

        public Span<short> ModuleFileCounts => chunk.PeekSpan<short>(4 + (NumModules * 2), NumModules);

        public Span<int> FileNameOffsets
        {
            get
            {
                //NumSourceFiles is only 16-bit; to get the real number of source files you have to manually count them
                var numSourceFiles = 0;

                foreach (var item in ModuleFileCounts)
                    numSourceFiles += item;

                return chunk.PeekSpan<int>(4 + (NumModules * 4), numSourceFiles);
            }
        }

        public RawValue<FixedUtf8String>[] Names { get; }

        public int Offset => chunk.AbsoluteOffset;

        private readonly MemoryChunk chunk;

        internal unsafe FileInfo(in MemoryChunk chunk)
        {
            this.chunk = chunk;

            //I believe the information encapsulated by the FileInfo type is what you get when you call DBI1::QueryFileInfo

            //We need to calculate where this info ends; using our FileNameOffsets property would duplicate this info

            //NumSourceFiles is only 16-bit; to get the real number of source files you have to manually count them
            var numSourceFiles = 0;

            foreach (var item in ModuleFileCounts)
                numSourceFiles += item;

            var startPos = 4 + (NumModules * 4);
            var fileNameOffsets = chunk.PeekSpan<int>(startPos, numSourceFiles);
            startPos += (fileNameOffsets.Length * 4);

            var namesChunk = chunk.Slice(startPos);

            RawValue<FixedUtf8String>[] names;

            if (fileNameOffsets.Length > 0)
            {
                //Multiple entries can map to the same offset. If the PDB version is <= vc98 the strings are length prefixed.
                //Counting the length of strings isn't free, so I think we need to cache each string's offset
                //to worry about caching anything
                names = new RawValue<FixedUtf8String>[fileNameOffsets.Length];

                var pdb = chunk.PDBFile();

                if (pdb.PDB!.PDBHeader.ImplementationVersion <= ClrDebug.PDB.PDBIMPV.PDBImpvVC98)
                {
                    //The strings are length prefixed. I feel like doing dictionary lookups would be more expensive than not,
                    //so just read the ST strings as is

                    //These strings are ANSI not UTF8, but I feel like UTF8 encompasses ANSI
                    for (var i = 0; i < fileNameOffsets.Length; i++)
                    {
                        var offset = fileNameOffsets[i];

                        var length = namesChunk.PeekByte(offset);
                        var str = namesChunk.PeekUtf8FixedLength(offset + 1, length);
                        names[i] = new RawValue<FixedUtf8String>(namesChunk.AbsoluteOffset + offset, str);
                    }
                }
                else
                {
                    //The strings are null terminated. We need to do strlen in this case, so we cache
                    var dict = new Dictionary<int, RawValue<FixedUtf8String>>();

                    for (var i = 0; i < fileNameOffsets.Length; i++)
                    {
                        var offset = fileNameOffsets[i];

                        if (!dict.TryGetValue(offset, out var existing))
                        {
                            var str = namesChunk.PeekAnsiNullTerminatedString(offset);

                            existing = new RawValue<FixedUtf8String>(namesChunk.AbsoluteOffset + offset, new FixedUtf8String(str, str.Length));
                        }

                        names[i] = existing;
                    }
                }
            }
            else
                names = Array.Empty<RawValue<FixedUtf8String>>();

            Names = names;
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct("File Info", this, ViewKind.FileInfo);

            s.WriteField(nameof(NumModules), NumModules);
            s.WriteField(nameof(NumSourceFiles), NumSourceFiles);
            s.WriteField(nameof(ModuleIndices), ModuleIndices);
            s.WriteField(nameof(ModuleFileCounts), ModuleFileCounts);
            s.WriteField(nameof(FileNameOffsets), FileNameOffsets);

            //FileNameOffsets contains a list of relative offsets to each name. However, there could be multiple entries
            //pointing to the same name

            var seenAddress = new HashSet<int>();

            var pdb = chunk.PDBFile();

            if (pdb.PDB!.PDBHeader.ImplementationVersion <= ClrDebug.PDB.PDBIMPV.PDBImpvVC98)
            {
                //Write inline ST strings
                foreach (var name in Names.OrderBy(v => v.Offset))
                {
                    if (seenAddress.Add(name.Offset))
                        s.WriteInlineLengthPrefixedAnsiString(name);
                }
            }
            else
            {
                foreach (var name in Names.OrderBy(v => v.Offset))
                {
                    if (seenAddress.Add(name.Offset))
                        s.WriteInlineUtf8NullTerminated(name);
                }
            }
        }
    }
}
