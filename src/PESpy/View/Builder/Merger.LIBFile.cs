using System.Diagnostics;
using PESpy.LIB;

namespace PESpy.View.Builder
{
    internal ref partial struct Merger
    {
        internal IView[] MergeLIB()
        {
            var libFile = (LIBFile) file;

            var results = new PooledList<IView>();
            var nestedObjRegions = new PooledList<IView>();

            try
            {
                var length = byteViewProvider.FileOrSectionLength;

                //Temporarily pretend we're past all directories while trying to construct section regions
                nextDataDirectoryIndex = discoveredDataDirectories.Length;

                //We also need to build regions around each section inside a long names member. This is a bit tricky, because we don't support building custom child regions
                //while we're in the process of building a parent region. So Plan B: we'll eagerly construct the section regions for each long names member, and then
                //add them to our list of sorted structs so that they naturally get included
                foreach (var item in libFile.ImportLibrary)
                {
                    if (item is LongImportLibraryMember l)
                    {
                        var lastSectionEnd = -1;

                        for (var i = 0; i < l.SectionHeaders.Length; i++)
                        {
                            ref var section = ref l.SectionHeaders[i];

                            var start = section.PointerToRawData;

                            if (start == 0)
                                continue; //You can have a section like .bss which says it has a length of 8 but the PointerToRawData is0

                            var size = section.SizeOfRawData;

                            start += l.FileHeader.Offset;

                            if (i == 0)
                            {
                                //Skip ahead to find the first struct that pertains to this obj file
                                //We only do this for the first section, because for all subsequent sections there may be inter-section data (like relocations)
                                //that we want to detect and create a special region around
                                for (; nextStructIndex < sortedStructs.Count; nextStructIndex++)
                                {
                                    if (sortedStructs[nextStructIndex].Offset >= start)
                                        break;
                                }
                            }

                            var nextStructIndexToInsertAt = nextStructIndex;

                            ProcessSectionHeader(section, start, size, lastSectionEnd, ref this, ref nestedObjRegions);

                            var endNextStructIndex = nextStructIndex;

                            var numStructsInserted = endNextStructIndex - nextStructIndexToInsertAt;

#if DEBUG
                            //Assert that we're inserting these in order
                            var itemBeforeInsert = sortedStructs[nextStructIndexToInsertAt - 1];
                            var itemAfterInsert = sortedStructs[nextStructIndexToInsertAt + numStructsInserted];

                            var firstItemToInsert = nestedObjRegions[0];
                            var lastItemToInsert = nestedObjRegions[nestedObjRegions.Count - 1];

                            Debug.Assert(firstItemToInsert.Offset > itemBeforeInsert.Offset);
                            Debug.Assert(lastItemToInsert.Offset < itemAfterInsert.Offset);
#endif

                            sortedStructs.RemoveRange(nextStructIndexToInsertAt, numStructsInserted);

                            sortedStructs.InsertRange(nextStructIndexToInsertAt, nestedObjRegions);

                            //Decrement the nextStructIndex by the total number of items we removed. We don't reset nextStructIndex to 0 each time,
                            //as the next struct we're going to be looking for is going to be after the previous long members + sections we've already processed
                            nextStructIndex -= (numStructsInserted - nestedObjRegions.Count);

                            nestedObjRegions.Clear();

                            lastSectionEnd = start + size;
                        }

                        var objEnd = l.FileHeader.Offset + l.ArchiveHeader.Size;

                        if (objEnd > lastSectionEnd && lastSectionEnd != -1)
                        {
                            var originalNextStructIndex = nextStructIndex;

                            ProcessOverlay(lastSectionEnd, objEnd, ref this, ref nestedObjRegions);

                            var endNextStructIndex = nextStructIndex;

                            var numStructsInserted = endNextStructIndex - originalNextStructIndex;

                            sortedStructs.RemoveRange(originalNextStructIndex, numStructsInserted);

                            sortedStructs.InsertRange(originalNextStructIndex, nestedObjRegions);

                            nextStructIndex -= (numStructsInserted - nestedObjRegions.Count);

                            nestedObjRegions.Clear();
                        }
                    }
                }

                nextStructIndex = 0;
                nextDataDirectoryIndex = 0;

                var result = BuildSection(0, length, v => v, v => v);

                return result;
            }
            finally
            {
                results.Dispose();
                nestedObjRegions.Dispose();
            }
        }
    }
}
