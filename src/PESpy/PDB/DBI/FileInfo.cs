using System;
using System.Collections.Generic;
using System.Linq;
using ClrDebug.OMF;
using PESpy.View;

namespace PESpy.PDB
{
    //Name is made up. Format is described as a blob
    //in DBI1::QueryFileInfo. This is the same format as sstFileIndex (as is noted in dbi.cpp)

    /// <summary>
    /// Represents the file information pointed to by <see cref="NewDBIHdr.cbFileInfo"/>.
    /// The name of this data structure is made up; the actual format is described as a blob in DBI1::QueryFileInfo,
    /// however dbi.cpp also notes that this is the same format as <see cref="SST.sstFileIndex"/>.
    /// </summary>
    {
        //cMods
        public ushort NumModules => chunk.PeekUInt16(0);

        //cRefs
        public ushort NumSourceFiles => chunk.PeekUInt16(2);

        /// <summary>
        /// For each module, describes the position in <see cref="FileNameOffsets"/> where the given module's file names start (by treating <see cref="FileNameOffsets"/> like a flat array)<para/>
        /// For example, if <see cref="ModuleIndices"/> is 0, 3, 9, 11, then module 0's files occupy FileNameOffsets[0-2], module 1's files are in FileNameoffsets[3-8], etc. You don't need to
        /// explicitly deduce the number of files in each module however; this is told to you by <see cref="ModuleFileCounts"/>.<para/>
        /// Corresponds to /ushort iRefModStart[cMods]
        /// </summary>
        public NativeSpan<ushort> ModuleIndices => chunk.PeekNativeSpan<ushort>(4, NumModules);

        /// <summary>
        /// Gets the number of files contained in each module.<para/>
        /// Used by <see cref="FileNameOffsets"/> to calculate how many file names to read for a given module from the location pointed to by <see cref="ModuleIndices"/>.
        /// Corresponds to ushort cRefsForMod[cMods]
        /// </summary>
        public NativeSpan<ushort> ModuleFileCounts => chunk.PeekNativeSpan<ushort>(4 + (NumModules * 2), NumModules);

        /// <summary>
        /// Gets the jagged array of file name offsets for the files referenced by the PDB. There is one element in the outer array for each
        /// module in the PDB, with each of those modules containing a number of elements equal to the file count indicated by <see cref="ModuleFileCounts"/>.<para/>
        /// When indexing into this member, the list of file name offsets is treated like a flat array, and <see cref="ModuleIndices"/> is then used to jump straight to the location
        /// where the given module's file name offsets start. <see cref="ModuleFileCounts"/> is then used to determine how many offsets to read from this location.<para/>
        /// Corresponds to ICH rgICH[cMods][cRefsForMod(iMod)]
        /// </summary>
        public FileNameOffsetsList FileNameOffsets => new FileNameOffsetsList(this);

        /// <summary>
        /// Provides access to the file names pointed to by <see cref="FileNameOffsets"/> within the "names" data region of this <see cref="FileInfo"/> record.
        /// There is one element in the outer array for each module in the PDB, with each module then containing a file name for each file name offset pointed to by
        /// <see cref="FileNameOffsets"/>.
        /// </summary>
        public FileNameNamesList FileNames { get; }

        public int Offset => chunk.AbsoluteOffset;

        public static unsafe FileInfo FromMemory(IntPtr pFileInfo, int length, bool isLengthPrefixedString)
        {
            var globalBlock = new GlobalMemoryBlock((byte*) pFileInfo, length, null);

            return new FileInfo(new MemoryChunk(globalBlock, 0), length, isLengthPrefixedString);
        }

        private readonly MemoryChunk chunk;
        private readonly int length;

        internal unsafe FileInfo(in MemoryChunk chunk, int length, bool isLengthPrefixedString)
        {
            this.chunk = chunk;
            this.length = length;

            //I believe the information encapsulated by the FileInfo type is what you get when you call DBI1::QueryFileInfo

            //We need to calculate where this info ends; using our FileNameOffsets property would duplicate this info

            //NumSourceFiles is only 16-bit; to get the real number of source files you have to manually count them
            var numSourceFiles = 0;

            foreach (var item in ModuleFileCounts)
                numSourceFiles += item;

            var startPos = 4 + (NumModules * sizeof(int));
            var fileNameOffsets = chunk.PeekSpan<int>(startPos, numSourceFiles);
            startPos += (fileNameOffsets.Length * sizeof(int));

            var namesChunk = chunk.Slice(startPos);

            FileNames = new FileNameNamesList(namesChunk, this, isLengthPrefixedString);
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.FileInfo, this, ViewKind.FileInfo, length);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField("cMods", NumModules);
            s.WriteField("cRefs", NumSourceFiles);
            s.WriteField("iRefModStart", ModuleIndices);
            s.WriteField("cRefsForMod", ModuleFileCounts);
            s.WriteField("rgICH", FileNameOffsets.AsFlat());

            //FileNameOffsets contains a list of relative offsets to each name. However, there could be multiple entries
            //pointing to the same name

            var seenAddress = new HashSet<int>();

            var pdb = chunk.PDBFile();

            if (pdb.PDB!.PDBHeader.ImplementationVersion <= ClrDebug.PDB.PDBIMPV.PDBImpvVC98)
            {
                //Write inline ST strings
                foreach (var name in FileNames.AsFlat().OrderBy(v => v.Offset))
                {
                    if (seenAddress.Add(name.Offset))
                        s.WriteInlineLengthPrefixedAnsiString(name);
                }
            }
            else
            {
                foreach (var name in FileNames.AsFlat().OrderBy(v => v.Offset))
                {
                    if (seenAddress.Add(name.Offset))
                        s.WriteInlineUtf8NullTerminated(name);
                }
            }

            //microsoft-pdb shows it should be aligned
            s.Align(4);
            return s.ToArray();
        }
    }
}
