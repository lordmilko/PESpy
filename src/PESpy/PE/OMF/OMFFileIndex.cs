using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ClrDebug.OMF;
using PESpy.PDB;
using PESpy.View;

namespace PESpy
{
    /* In microsoft-pdb, the shape of the data format is described, and it is explicitly mentioned in dbp.cpp
     * that the format is the same as that of sstFileIndex. However, we can go one step further than this,
     * because in NT 4 there are multiple locations where this value is requested from DBI1::QueryFileInfo and
     * is directly cast to OMFFileIndex. As such, we can conclude that it is valid to the type that
     * PDB1 uses as OMFFileIndex as well. To aid in the comprehension of this data structure for callers,
     * we give the fields a different name based on whether the owning file is a PDB or not
     */

    /// <summary>
    /// Represents the file information pointed to by <see cref="SST.sstFileIndex"/> and <see cref="NewDBIHdr.cbFileInfo"/>.
    /// </summary>
    [Source(SourceKind.cvexefmt_h)]
    public partial class OMFFileIndex : IValue, IViewable
    {
        private const int NumModulesOffset = 0;
        private const int NumSourceFilesOffset = 2;
        private const int ModuleIndicesOffset = 4;

        //cMods
        public ushort NumModules => chunk.PeekUInt16(NumModulesOffset);

        //cRefs
        public ushort NumSourceFiles => chunk.PeekUInt16(NumSourceFilesOffset);

        /// <summary>/
        /// For each module, describes the position in <see cref="FileNameOffsets"/> where the given module's file names start (by treating <see cref="FileNameOffsets"/> like a flat array)<para/>
        /// For example, if <see cref="ModuleIndices"/> is 0, 3, 9, 11, then module 0's files occupy FileNameOffsets[0-2], module 1's files are in FileNameoffsets[3-8], etc. You don't need to
        /// explicitly deduce the number of files in each module however; this is told to you by <see cref="ModuleFileCounts"/>.<para/>
        /// Corresponds to /ushort iRefModStart[cMods]
        /// </summary>
        public NativeSpan<ushort> ModuleIndices => chunk.PeekNativeSpan<ushort>(ModuleIndicesOffset, NumModules);

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
        /// Provides access to the file names pointed to by <see cref="FileNameOffsets"/> within the "names" data region of this <see cref="OMFFileIndex"/> record.
        /// There is one element in the outer array for each module in the PDB, with each module then containing a file name for each file name offset pointed to by
        /// <see cref="FileNameOffsets"/>.
        /// </summary>
        public FileNameNamesList FileNames { get; }

        public long Offset => chunk.AbsoluteOffset;

        public static unsafe OMFFileIndex FromMemory(IntPtr pFileInfo, int length, bool isLengthPrefixedString)
        {
            var globalBlock = new GlobalMemoryBlock((byte*) pFileInfo, length, null);

            return new OMFFileIndex(new MemoryChunk(globalBlock, 0), length, isLengthPrefixedString);
        }

        private readonly MemoryChunk chunk;
        private readonly int length;

        internal unsafe OMFFileIndex(in MemoryChunk chunk, int length, bool isLengthPrefixedString)
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
            writer.NewStruct(this, ViewKind.OMFFileIndex, length);

        int IViewable.NumChildren() => throw StructWriter.GetEagerLoadOnlyException();

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            ////The names need to be sorted, so only support eager load
            if (index != -1)
                throw StructWriter.GetEagerLoadOnlyException();

            using var s = structWriter.CreateEagerWriter();

            string numModulesName;
            string numSourceFilesName;
            string moduleIndicesName;
            string moduleFileCountsName;
            string fileNameOffsetsName;

            if (chunk.File() is PDBFile)
            {
                numModulesName = "cMods";
                numSourceFilesName = "cRefs";
                moduleIndicesName = "iRefModStart";
                moduleFileCountsName = "cRefsForMod";
                fileNameOffsetsName = "rgICH";
            }
            else
            {
                numModulesName = "cmodules";
                numSourceFilesName = "cfilerefs";
                moduleIndicesName = "modulelist";
                moduleFileCountsName = "cfiles";
                fileNameOffsetsName = "ulNames";

                //There's then a "Names" field that contains all of the names
            }

            s.WriteField(numModulesName, NumModules);
            s.WriteField(numSourceFilesName, NumSourceFiles);
            s.WriteField(moduleIndicesName, ModuleIndices);
            s.WriteField(moduleFileCountsName, ModuleFileCounts);

            var flatFileNameOffsets = FileNameOffsets.AsFlat();

            if (flatFileNameOffsets.Length > 0)
                s.WriteField(fileNameOffsetsName, flatFileNameOffsets);

            //FileNameOffsets contains a list of relative offsets to each name. However, there could be multiple entries
            //pointing to the same name

            var seenAddress = new HashSet<long>();

            if (FileNames.isLengthPrefixedString)
            {
                //Write inline ST strings
                foreach (var name in FileNames.AsFlat().OrderBy(v => v.Offset))
                {
                    if (seenAddress.Add(name.Offset))
                        s.WriteInlineSymString(name);
                }
            }
            else
            {
                foreach (var name in FileNames.AsFlat().OrderBy(v => v.Offset))
                {
                    if (seenAddress.Add(name.Offset))
                        s.WriteInlineSymString(name);
                }
            }

            //microsoft-pdb shows it should be aligned
            s.Align(4);

            structWriter.EagerFields = s.ToArray();
        }
    }
}
