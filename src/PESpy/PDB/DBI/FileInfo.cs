using System;
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

                return chunk.PeekSpan<int>(4 + (NumModules * 4), numSourceFiles); //4 + ((NumModules * 2) * 2)
            }
        }

        public int Offset => chunk.AbsoluteOffset;

        private readonly MemoryChunk chunk;

        internal FileInfo(in MemoryChunk chunk)
        {
            this.chunk = chunk;

            var fileNameOffsets = FileNameOffsets;

            if (fileNameOffsets.Length > 0)
            {
            }
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct("File Info", this, ViewKind.FileInfo);

            s.WriteField(nameof(NumModules), NumModules);
            s.WriteField(nameof(NumSourceFiles), NumSourceFiles);
            s.WriteField(nameof(ModuleIndices), ModuleIndices);
            s.WriteField(nameof(ModuleFileCounts), ModuleFileCounts);
            s.WriteField(nameof(FileNameOffsets), FileNameOffsets);
    }
}
