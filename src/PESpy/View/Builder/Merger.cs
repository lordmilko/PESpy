using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
#if NET5_0_OR_GREATER
using System.Runtime.InteropServices;
#endif
using PESpy.PDB;


namespace PESpy.View.Builder
{
    internal ref partial struct Merger
    {
        private IFile file;

        private GapBuffer<IView> sortedStructs;

        private Span<DirectoryInfo> discoveredDataDirectories;

        private int nextStructIndex;
        private int nextDataDirectoryIndex;
        private ViewWriter viewWriter;
        private IView? nextValue;
        private DirectoryInfo? directory;
        private ByteViewProvider byteViewProvider;
        private RepeatingGroupMode repeatingGroupMode;

        private PooledList<IView> masterList;
        private PooledList<IView> currentList;
        private PooledList<IView> repeatingTypeList;

        internal Merger(IFile file, ViewWriter viewWriter, List<IView> sortedStructs, ByteViewProvider byteViewProvider) :
            this(file, viewWriter, sortedStructs, default, byteViewProvider)
        {
        }

        internal Merger(
            IFile file,
            ViewWriter viewWriter,
            List<IView> sortedStructs,
            Span<DirectoryInfo> discoveredDataDirectories,
            ByteViewProvider byteViewProvider)
        {
            this.file = file;
            this.viewWriter = viewWriter;
            this.sortedStructs = new GapBuffer<IView>(sortedStructs);
            this.discoveredDataDirectories = discoveredDataDirectories;
            this.byteViewProvider = byteViewProvider;

            nextStructIndex = default;
            nextDataDirectoryIndex = default;
            nextValue = default;
            directory = default;
            repeatingGroupMode = default;

            masterList = default;
            currentList = default;
            pageNumberToSIIndex = default;
            repeatingTypeList = default;
        }

        public void Dispose()
        {
            masterList.Dispose();
            currentList.Dispose();
            repeatingTypeList.Dispose();
        }

        internal IView[] BuildSection(
            int startRva,
            int endRva,
            Func<int, int>? getRealOffset = null,
            Func<int, int>? getRVA = null,
            bool isOverlay = false)
        {
            masterList.Clear();
            currentList.Clear();
            repeatingTypeList.Clear();

            long currentEnd = endRva;
            repeatingGroupMode = 0;

            directory = null;

            for (long rva = startRva; rva < endRva; rva++)
            {
                if (directory != null)
                {
                    //We have an active directory
                    if ((rva < directory.Value.Start || rva >= directory.Value.End))
                        FinalizeDirectoryRegion(endRva, ref currentEnd);
                }

                if (directory == null)
                    TryGetNextDirectory((int) rva, endRva, ref currentEnd);

                nextValue = null;

                GetValueOrBytes(ref rva, currentEnd, endRva, getRealOffset, getRVA, isOverlay);
            }

            FinalizeRepeatingTypeRegion();
            FinalizeDirectoryRegion(endRva, ref currentEnd);

            masterList.AddRange(currentList);

            return masterList.ToArray();
        }

        private void TryGetNextDirectory(int rva, int endRva, ref long currentEnd)
        {
            //If we skipped over the directory because we don't know it (meaning we read it as a byte blob), we need to skip to the next valid directory
            while (nextDataDirectoryIndex < discoveredDataDirectories.Length && discoveredDataDirectories[nextDataDirectoryIndex].Start < rva)
            {
                //Ordinarily, we want to assert here that we didn't inadvertently skip over a data directory. However, if we've got a rogue directory claiming that it starts before the previous
                //directory ends, we need to skip over it

                if (nextDataDirectoryIndex > 0 && discoveredDataDirectories[nextDataDirectoryIndex].Start < discoveredDataDirectories[nextDataDirectoryIndex - 1].End)
                    nextDataDirectoryIndex++;
                else
                {
                    //If you have a value that is incorrectly longer than it's supposed to be, and the result of that is the next directory is inside
                    //of that value's region, you will hit this assert. The value should be checked if it's really supposed to be as long as it is.
                    //Perhaps a sentinel that indicates you're meant to stop reading was missed
                    Debug.Assert(false, "Should not have read past the start of the next data directory");
                    throw new InvalidOperationException("Should not have read past the start of the next data directory");
                }
            }

            if (nextDataDirectoryIndex < discoveredDataDirectories.Length)
            {
                var candidateDirectory = discoveredDataDirectories[nextDataDirectoryIndex];

                if (rva >= candidateDirectory.Start && rva < candidateDirectory.End)
                {
                    FinalizeRepeatingTypeRegion();

                    masterList.AddRange(currentList);
                    currentList.Clear();
                    directory = candidateDirectory;
                    currentEnd = directory.Value.End; //We need to prevent reading bytes past the end of the directory
                }
                else
                    FinalizeDirectoryRegion(endRva, ref currentEnd);
            }
        }

        private void GetValueOrBytes(ref long rva, long currentEnd, int endRva, Func<int, int>? getRealOffset, Func<int, int>? getRVA, bool isOverlay)
        {
            if (nextStructIndex < sortedStructs.Count && (nextValue = sortedStructs[nextStructIndex]).Offset < currentEnd)
            {
                if (rva > nextValue.Offset)
                {
                    //Multiple RuntimeFunction entries may point to the same UnwindInfo

                    var previous = sortedStructs[nextStructIndex - 1];

                    ref var nextValue = ref this.nextValue;

                    while (previous.Kind == nextValue.Kind && previous.Offset == nextValue.Offset && previous.Size == nextValue.Size)
                    {
                        nextStructIndex++;

                        if (nextStructIndex < sortedStructs.Count)
                            nextValue = sortedStructs[nextStructIndex];
                        else
                            break; //Something has gone seriously wrong and we've run out of structs
                    }

#if DEBUG
                    if (previous.Offset == nextValue.Offset)
                        Debug.Assert(false, $"Two values have been written at RVA 0x{rva:X}: {previous}, {nextValue}");

                    Debug.Assert(rva <= nextValue.Offset);
#endif
                }

                if (nextValue.Offset == rva)
                {
                    //If we see a bunch of strings in a row, synthesize a virtual strings region.
                    //If we see a bunch of logical regions with the same in a row, synthesize a virtual region around them

                    ProcessValueOrRepeatingGroup(getRVA);

                    Debug.Assert(nextValue.Size != 0);
                    rva += nextValue.Size - 1;
                    nextStructIndex++;
                }
                else
                {
                    //Read all bytes up to the next metadata item
                    ReadByteBlob(ref rva, nextValue.Offset, endRva, getRealOffset, getRVA, isOverlay);
                }
            }
            else
            {
                //Read all bytes to the end
                ReadByteBlob(ref rva, currentEnd, endRva, getRealOffset, getRVA, isOverlay);
            }
        }

        private void ProcessValueOrRepeatingGroup(Func<int, int>? getRVA)
        {
            if (nextValue is ValueView<string> v) //Use generics to avoid boxing
            {
                if (repeatingGroupMode != 0 && (repeatingGroupMode != RepeatingGroupMode.Strings && repeatingGroupMode != RepeatingGroupMode.ImportFunctionNames))
                    FinalizeRepeatingTypeRegion();

                repeatingTypeList.Add(v);

                if (repeatingGroupMode == 0)
                    repeatingGroupMode = RepeatingGroupMode.Strings;
            }
            else if (nextValue is LogicalRegionView lr)
            {
                if (repeatingGroupMode != 0 && repeatingGroupMode != RepeatingGroupMode.LogicalRegion || (repeatingTypeList.Count > 0 && repeatingTypeList[0].Kind != lr.Kind))
                    FinalizeRepeatingTypeRegion();

                repeatingTypeList.Add(nextValue);

                if (repeatingGroupMode == 0)
                    repeatingGroupMode = RepeatingGroupMode.LogicalRegion;
            }
            else
            {
                FinalizeRepeatingTypeRegion();

                if (nextDataDirectoryIndex < discoveredDataDirectories.Length)
                {
                    var currentDirectory = discoveredDataDirectories[nextDataDirectoryIndex];

                    var nextValueEnd = nextValue!.Offset + nextValue.Size;

                    //The calling method is going to increment its rva based on how many bytes we wrote here; but in PDBs,
                    //values can span multiple pages. A DirectoryInfo only knows how many pages large it is. This says nothing
                    //about the number of pages the values within those pages span
                    if (nextValueEnd > currentDirectory.End)
                    {
                        if (file is not PDBFile)
                        {
                            /* It seems like it's possible to have PE Files under-report how big their directories are, and then have data that expands beyond the end of that directory.
                             * This is very common with the load config table directory. If we hit a scenario like this, expand the size of the directory to fit the value
                             * we're trying to process, but also assert on this for unknown scenarios so we can catch any issues that might actually be bugs.
                             *
                             * Also observed this happening with resources. There resource directory is inside the .rsrc section; there was a gap after the reported
                             * end of the resource table and the beginning of the .reloc section which would follow it; therefore, there was plenty of free room
                             * within the .rsrc section to fit the under reported data */
                            Debug.Assert(currentDirectory.Name == "LoadConfigTableDirectory" || currentDirectory.Name == "ResourceTableDirectory");

                            currentDirectory.End = checked((int) nextValueEnd);
                            Debug.Assert(directory.Value.Start == currentDirectory.Start);
                            directory = currentDirectory;

                            goto end;
                        }

                        //We need to split this value in two. If it's a structure, it becomes a SplitStructure.
                        //If the value that goes past the ends of the current directory's bounds is also a struct,
                        //it becomes a SplitStructure too. Ultimately, we'll get down to the individual overlapping value.
                        //That becomes a SplitValue, and then all members after the point where we split should become
                        //part of the new secondary SplitStructure. Finally, we insert it into the sorted struct list so that
                        //we add it to the next directory

                        var secondStartOffset = GetNextPageOffset((PDBFile) file, nextValue.Offset, pageNumberToSIIndex);

                        var (first, second) = ((ISplittableView) nextValue).Split(secondStartOffset, currentDirectory.End);
                        sortedStructs[nextStructIndex] = first;

                        /* While it is true that "most of the time" page numbers run in ascending order, due to the crazy way in which PDBs are constructed,
                         * you can have a very high page number at the front of the PageList, and then smaller page numbers following it. So, with the value
                         * we just split out, ideally we want it to belong to an offset that we haven't attempted to process yet. If so, we can just
                         * find the relevant insertion point and insert it into the sortedStructs array. If we've already gone past the offset where
                         * that struct should have belonged, we've now got a big mess and are going to need to backtrack and somehow patch up the views
                         * we've already constructed */
                        if (second.Offset >= currentDirectory.End)
                        {
                            //Good news! Just insert it into the list
                            for (var i = nextStructIndex + 1; i < sortedStructs.Count; i++)
                            {
                                if (second.Offset < sortedStructs[i].Offset)
                                {
                                    //In a very large PDB (with over 500,000 items in sorted structs) the repeated shuffling of items with
                                    //each insert is very slow. As such, we use a gap buffer instead

                                    //This is the insertion point
                                    sortedStructs.Insert(i, second);
                                    break;
                                }
                            }
                        }
                        else
                        {
                            //Oh boy. We're going to need to patch up data in the masterList, potentially removing junk we defaulted to reading
                            //and inserting this proper structure instead (and then re-reading junk to fill in any gaps)

                            //Find the item in the masterList that contains this address. Then drill into its children until we find the overlapping items.
                            //We expect they should be junk: ByteBlobView and ValueView<string> or a LogicalRegionView with any of these items in them.
                            //There may also be padding.
                            ReplaceGarbage(second, getRVA);
                        }

                        nextValue = first;
                    }
                }

end:
                currentList.Add(nextValue!);
            }
        }

        //Given an offset in the current page, gets the offset of the start of the page after it.
        //If previous is true, returns the offset of the start of the page before it
        internal static long GetNextPageOffset(
            PDBFile pdbFile,
            long offsetInCurrentPage,
            Dictionary<PN, int> pageNumberToSIIndex,
            bool previous = false)
        {
            /* The current value may exist between 0x1000-0x1120. However, the current directory may only
             * span from 0x1000-0x1100, meaning that the bytes at 0x1100-0x1120 need to be split off. However,
             * it's actually erroneous to say that these bytes necessarily existed at 0x1100-0x1120; they may have been
             * read from a page far, far away from here, e.g. in the 0x4000 range. To figure out the offset
             * to use for the split page, we must figure out what our current page is, which stream that's in,
             * what our index is within that stream, and then what the next page after us is */

            var currentPage = (PN) (offsetInCurrentPage / pdbFile.PageSize); //We want the current page, so don't divide up
            var siIndex = pageNumberToSIIndex[currentPage];

            Span<PN> siPageList;

            //MSF v2 files store their pages as ushort instead of uint, so we rent an array to expand these values to uint
            //to enable sharing the same pagelist lookup logic
            PN[] pdbV2PageList = null;

            try
            {
                //Get the appropriate page list to search

                if (siIndex == SPECIAL_STREAM_STREAMTABLE)
                {
                    if (pdbFile is PDB7File v7)
                        siPageList = v7.StreamTableLocation.PageList;
                    else
                    {
                        //In V2 mpspnpnSt lists the pages of the stream table, not the pages that the stream table's pages are found in
                        var rawPages = ((PDB2File) pdbFile).MsfHeader.StreamTablePageList;

                        pdbV2PageList = ArrayPool<PN>.Shared.Rent(rawPages.Length);

                        for (var i = 0; i < rawPages.Length; i++)
                            pdbV2PageList[i] = rawPages[i];

                        siPageList = pdbV2PageList.AsSpan(0, rawPages.Length);
                    }
                }
                else if (siIndex == SPECIAL_STREAM_STREAMTABLE_LOCATION)
                {
                    //It's a page describing the location of the stream table's pages
                    siPageList = ((PDB7File) pdbFile).MsfHeader.PagesOfStreamTablePageList;
                }
                else
                {
                    var si = pdbFile.StreamTable.StreamInfos[siIndex];

                    siPageList = si.PageList;
                }

                var currentIndex = siPageList.IndexOf(currentPage);

                if (currentIndex == -1)
                    throw new InvalidOperationException("Could not find the current page in the page list; this should be impossible");

                /* Suppose you have a DBI section with the following pages:
                 *     59: 0xEC00 - 0xF000
                 *     58: 0xE800 - 0xEC00
                 *
                 * Observe that the second page is _before_ the first page. You then might have an OMFSegMap that proclaims
                 * that it lies within 0xEFD4-0xF027. On the basis that 0xF027 is beyond the bounds of page 59 (0xF000) we determine
                 * that a split is required here, and we would normally say that the cutoff point for performing the split is 0xF000.
                 * However, when you look at the actual children of the OMFSegMap, you may have a bunch of items between 0xEFD4-0xEFFF
                 * and another item perfectly after the start of the next page at 0xE800. This is going to cause issues when we go to
                 * try and find the split point, because no child is actually ever after 0xF000; the parent OMFSegMap only reports that
                 * this is the case because the total size is 84. So, when performing the split what we really need to do is _either_
                 * look for values that are running over the edge of the current page, _or_ are perfectly situated at the start of
                 * the current page
                 */

                if (previous)
                {
                    var previousPage = siPageList[currentIndex - 1];
                    return previousPage * pdbFile.PageSize;
                }
                else
                {
                    //The next page in the list is the one that our split value begins from
                    var nextPage = siPageList[currentIndex + 1];
                    return nextPage * pdbFile.PageSize;
                }
            }
            finally
            {
                if (pdbV2PageList != null)
                    ArrayPool<PN>.Shared.Return(pdbV2PageList);
            }
        }

        private void ReplaceGarbage(IView replacement, Func<int, int>? getRVA)
        {
            for (var i = 0; i < masterList.Count; i++)
            {
                //We should only have LogicalRegionView items in the master list (at least, in PDBs this is the case)
                //And this method is exclusively used in splitting scenarios (which we currently only need to do in PDBs)
                var directory = (LogicalRegionView) masterList[i];

                if (directory.Offset >= replacement.Offset)
                {
                    //This is the directory that our value should be inserted into. Now get all of the values that conflict with the value we're inserting

                    void CheckSafeToDelete(IView view)
                    {
                        if (view is LogicalRegionView r)
                        {
                            foreach (var child in r.Children)
                                CheckSafeToDelete(child);

                            return;
                        }
                        else if (view is ValueView<string> || view is ValueView<AnsiString> || view is ByteBlobView)
                        {
                            return;
                        }

                        throw new NotImplementedException();
                    }

                    for (var j = 0; j < directory.Children.Count; j++)
                    {
                        var child = directory.Children[j];

                        if (child.Offset >= replacement.Offset)
                        {
                            //This is the first overlapping child

                            var replacementEnd = replacement.Offset + replacement.Size;

                            var k = j + 1;

                            for (; k < directory.Children.Count; k++)
                            {
                                var endChild = directory.Children[k];

                                if (endChild.Offset >= replacementEnd)
                                    break;

                                //endChild is marked for deletion. Let's double check that it's indeed safe to delete
                                CheckSafeToDelete(endChild);
                            }

                            var numItemsToReplace = k - j - 1;

                            using var newViews = new PooledList<IView>();

                            if (child.Offset > replacement.Offset)
                            {
                                //There's extra data prior to the start of our replacement value. Read the junk
                                throw new NotImplementedException();
                            }

                            newViews.Add(replacement);

                            var lastChild = directory.Children[j + numItemsToReplace];
                            var lastChildEnd = lastChild.Offset + lastChild.Size;

                            if (lastChildEnd > replacementEnd)
                            {
                                //There's additional space after the end of our replacement value. Read any junk

                                var junkStart = replacementEnd;

                                var views = byteViewProvider.ReadBytes(ref junkStart, lastChildEnd, null, null, getRVA, false);

                                //This method messes with global state. Backup our global state so that we can restore it afterwards

                                var oldRepeatingTypeList = repeatingTypeList;
                                var oldRepeatingGroupMode = repeatingGroupMode;
                                var oldCurrentList = currentList;

                                repeatingTypeList = new PooledList<IView>();
                                repeatingGroupMode = 0;
                                currentList = new PooledList<IView>();

                                try
                                {
                                    ProcessParsedByteViews(views!);

                                    FinalizeRepeatingTypeRegion();

                                    newViews.AddRange(currentList);
                                }
                                finally
                                {
                                    repeatingTypeList.Dispose();
                                    currentList.Dispose();

                                    repeatingTypeList = oldRepeatingTypeList;
                                    repeatingGroupMode = oldRepeatingGroupMode;
                                    currentList = oldCurrentList;
                                }
                            }

                            //Add all other children after the junk we're replacing in the directory to the new list of children
                            for (var l = k; l < directory.Children.Count; l++)
                                newViews.Add(directory.Children[l]);

                            //We've got everything we need now. Patch the original directory!
                            masterList[i] = new LogicalRegionView(directory.Offset, directory.Name, newViews.ToArray(), viewWriter, directory.Kind, directory.Size);
                            return;
                        }
                    }

                    throw new NotImplementedException();
                }
            }

            //If we're calling BuildSection multiple times, that will repeatedly clear the master list! So we need to make sure we don't do that

            throw new NotImplementedException();
        }

        void FinalizeDirectoryRegion(int endRva, ref long currentEnd)
        {
            if (directory == null)
                return;

            FinalizeRepeatingTypeRegion();

            Debug.Assert(currentList.Count > 0);

            var directoryRegion = new LogicalRegionView(directory.Value.Start, directory.Value.Name, currentList.ToArray(), viewWriter, file is PDBFile ? ViewKind.Page : ViewKind.DataDirectory, (int) (directory.Value.End - directory.Value.Start));

            masterList.Add(directoryRegion);

            directory = null;

            currentList.Clear();
            nextDataDirectoryIndex++;
            currentEnd = endRva;
        }

        void FinalizeRepeatingTypeRegion()
        {
            if (repeatingTypeList.Count == 0)
                return;

            if (repeatingTypeList.Count == 1)
            {
                currentList.Add(repeatingTypeList[0]);
                repeatingTypeList.Clear();
                repeatingGroupMode = 0;
                return;
            }

            ViewKind regionKind;

            switch (repeatingGroupMode)
            {
                case RepeatingGroupMode.Strings:
                    regionKind = ViewKind.Strings;
                    break;

                case RepeatingGroupMode.LogicalRegion:
                    regionKind = ((LogicalRegionView) repeatingTypeList[0]).Kind;

                    if (regionKind == ViewKind.ImportAddressTable)
                    {
                        //Prevent ImportAddressTable entries from being wrapped in a repeating item region; ImportAddressTable items
                        //will be wrapped in the Import Address Table directory
                        currentList.AddRange(repeatingTypeList);
                        repeatingTypeList.Clear();
                        repeatingGroupMode = 0;
                        return;
                    }

                    break;

                case RepeatingGroupMode.ImportFunctionNames:
                    regionKind = ViewKind.ImportStrings;
                    break;

                default:
                    throw new NotImplementedException($"Don't know how to handle {nameof(RepeatingGroupMode)} '{repeatingGroupMode}'");
            }

            var size = 0;

            for (var i = 0; i < repeatingTypeList.Count; i++)
                size += (int) repeatingTypeList[i].Size;

            currentList.Add(new LogicalRegionView(repeatingTypeList[0].Offset, regionKind.GetDescription(), repeatingTypeList.ToArray(), viewWriter, regionKind, size));
            repeatingTypeList.Clear();
            repeatingGroupMode = 0;
        }

        void ReadByteBlob(ref long rva, long end, int endRva, Func<int, int>? getRealOffset, Func<int, int>? getRVA, bool isOverlay)
        {
            var dirIndex = directory == null ? nextDataDirectoryIndex : nextDataDirectoryIndex + 1;

            if (dirIndex < discoveredDataDirectories.Length)
            {
                var nextDirectory = discoveredDataDirectories[dirIndex];

                //Try and protect against directories listing bogus addresses that overlap with each other.
                //This logic was initially implemented to protect against issues with the security directory...until I realized that the security directory uses absolute addressing. But this logic
                //is still useful for handling dodgy PE Files
                if (nextDirectory.Start > rva && end >= nextDirectory.Start && rva != nextDirectory.Start)
                {
                    var newEnd = Math.Min(end, nextDirectory.Start);

                    end = newEnd;
                }
            }

            var views = byteViewProvider.ReadBytes(ref rva, end, null, getRealOffset, getRVA, isOverlay);

            if (isOverlay && views == null)
            {
                rva = endRva;
                return;
            }

            switch (repeatingGroupMode)
            {
                case RepeatingGroupMode.ImportFunctionNames:
                case RepeatingGroupMode.Strings:
                case 0: //We don't have an existing repeating group

                    //Iterate through the contents and identify any repeating groups

                    ProcessParsedByteViews(views!);
                    break;

                default: //It's some other type of repeating group. Finalize it, and then parse the results of the byte array
                    FinalizeRepeatingTypeRegion();
                    ProcessParsedByteViews(views!);
                    break;
            }
        }

        private void ProcessParsedByteViews(IView[] views)
        {
            for (var i = 0; i < views.Length; i++)
            {
                var current = views[i];

                if (current is ByteBlobView b && b.Kind == ViewKind.Padding)
                {
                    //Padding does not establish a repeating group type
                    repeatingTypeList.Add(current);
                }
                else if (current is IValueView v && v.Value is string)
                {
                    repeatingTypeList.Add(current);

                    if (repeatingGroupMode == 0)
                        repeatingGroupMode = RepeatingGroupMode.Strings;
                }
                else
                {
                    FinalizeRepeatingTypeRegion();

                    currentList.Add(current);
                }
            }

            //If we started a repeating group, the next item to be processed will figure out whether to cancel it, or continue adding to it
        }
    }
}
