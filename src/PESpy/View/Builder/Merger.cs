using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using PESpy.PDB;

#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.View.Builder
{
    abstract class Merger
    {
        protected List<IView> sortedStructs;
        private HashSet<IView>? delayNameViews;
        protected List<DirectoryInfo> discoveredDataDirectories;

        protected int nextStructIndex;
        protected int nextDataDirectoryIndex;
        private IView? nextValue;
        private DirectoryInfo? directory;
        protected Extension extension;
        private RepeatingGroupMode repeatingGroupMode;

        private List<IView> masterList = new List<IView>();
        private List<IView> currentList = new List<IView>();
        private List<IView> repeatingTypeList = new List<IView>();

        protected Merger(
            List<IView> sortedStructs,
            HashSet<IView>? delayNameViews,
            List<DirectoryInfo> discoveredDataDirectories,
            Extension extension)
        {
            this.sortedStructs = sortedStructs;
            this.delayNameViews = delayNameViews;
            this.discoveredDataDirectories = discoveredDataDirectories;
            this.extension = extension;
        }

        internal abstract IView[] Merge();

        internal IView[] BuildSection(
            RawOffset startRva,
            RawOffset endRva,
            Func<int, int>? getRealOffset = null,
            Func<int, int>? getRVA = null,
            bool isOverlay = false)
        {
            masterList.Clear();
            currentList.Clear();
            repeatingTypeList.Clear();

            var currentEnd = endRva;
            repeatingGroupMode = 0;

            directory = null;

            for (var rva = startRva; rva < endRva; rva++)
            {
                if (directory != null)
                {
                    //We have an active directory
                    if ((rva < directory.Value.Start || rva >= directory.Value.End))
                        FinalizeDirectoryRegion(endRva, ref currentEnd);
                }

                if (directory == null)
                    TryGetNextDirectory(rva, endRva, ref currentEnd);

                nextValue = null;

                GetValueOrBytes(ref rva, currentEnd, endRva, getRealOffset, getRVA, isOverlay);
            }

            FinalizeRepeatingTypeRegion();
            FinalizeDirectoryRegion(endRva, ref currentEnd);

            masterList.AddRange(currentList);

            return masterList.ToArray();
        }

        private void TryGetNextDirectory(RawOffset rva, RawOffset endRva, ref RawOffset currentEnd)
        {
            //If we skipped over the directory because we don't know it (meaning we read it as a byte blob), we need to skip to the next valid directory
            while (nextDataDirectoryIndex < discoveredDataDirectories.Count && discoveredDataDirectories[nextDataDirectoryIndex].Start < rva)
            {
                //Ordinarily, we want to assert here that we didn't inadvertently skip over a data directory. However, if we've got a rogue directory claiming that it starts before the previous
                //directory ends, we need to skip over it

                if (nextDataDirectoryIndex > 0 && discoveredDataDirectories[nextDataDirectoryIndex].Start < discoveredDataDirectories[nextDataDirectoryIndex - 1].End)
                    nextDataDirectoryIndex++;
                else
                    Debug.Assert(false, "Should not have read past the start of the next data directory");
            }

            if (nextDataDirectoryIndex < discoveredDataDirectories.Count)
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

        private void GetValueOrBytes(ref RawOffset rva, RawOffset currentEnd, RawOffset endRva, Func<int, int>? getRealOffset, Func<int, int>? getRVA, bool isOverlay)
        {
            if (nextStructIndex < sortedStructs.Count && (nextValue = sortedStructs[nextStructIndex]).Offset < currentEnd)
            {
                if (rva > nextValue.Offset)
                {
                    //Multiple RuntimeFunction entries may point to the same UnwindCode

                    var previous = sortedStructs[nextStructIndex - 1];

                    while (previous.Kind == nextValue.Kind && previous.Offset == nextValue.Offset && previous.Size == nextValue.Size)
                    {
                        nextStructIndex++;

                        if (nextStructIndex < sortedStructs.Count)
                            nextValue = sortedStructs[nextStructIndex];
                        else
                            break; //Something has gone seriously wrong and we've run out of structs
                    }

                    Debug.Assert(rva <= nextValue.Offset);
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
            else if (nextValue is StructView s && s.Name == "IMAGE_IMPORT_BY_NAME")
            {
                if (repeatingGroupMode != 0 && (repeatingGroupMode != RepeatingGroupMode.ImportFunctionNames && repeatingGroupMode != RepeatingGroupMode.DelayImportFunctionNames))
                    FinalizeRepeatingTypeRegion();

                repeatingTypeList.Add(nextValue);

                if (repeatingGroupMode == 0)
                {
                    if (delayNameViews?.Contains(nextValue) == true)
                        repeatingGroupMode = RepeatingGroupMode.DelayImportFunctionNames;
                    else
                        repeatingGroupMode = RepeatingGroupMode.ImportFunctionNames;
                }
            }
            else
            {
                FinalizeRepeatingTypeRegion();

                if (nextDataDirectoryIndex < discoveredDataDirectories.Count)
                {
                    var currentDirectory = discoveredDataDirectories[nextDataDirectoryIndex];

                    var nextValueEnd = nextValue!.Offset + nextValue.Size;

                    //The calling method is going to increment its rva based on how many bytes we wrote here; but in PDBs,
                    //values can span multiple pages. A DirectoryInfo only knows how many pages large it is. This says nothing
                    //about the number of pages the values within those pages span
                    if (nextValueEnd > currentDirectory.End)
                    {
                        //We need to split this value in two. If it's a structure, it becomes a SplitStructure.
                        //If the value that goes past the ends of the current directory's bounds is also a struct,
                        //it becomes a SplitStructure too. Ultimately, we'll get down to the individual overlapping value.
                        //That becomes a SplitValue, and then all members after the point where we split should become
                        //part of the new secondary SplitStructure. Finally, we insert it into the sorted struct list so that
                        //we add it to the next directory

                        /* The current value may exist between 0x1000-0x1120. However, the current directory may only
                         * span from 0x1000-0x1100, meaning that the bytes at 0x1100-0x1120 need to be split off. However,
                         * it's actually erroneous to say that these bytes necessarily existed at 0x1100-0x1120; they may have been
                         * read from a page far, far away from here, e.g. in the 0x4000 range. To figure out the offset
                         * to use for the split page, we must figure out what our current page is, which stream that's in
                         * what our index is within that stream, and then what the next page after us is */

                        var pdbMerger = (PdbMsfMerger) this;
                        var currentPage = (PN) (nextValue.Offset / pdbMerger.pdbFile.PageSize); //We want the current page, so don't divide up
                        var siIndex = pdbMerger.pageNumberToSIIndex[currentPage];

                        Span<PN> siPageList;

                        if (siIndex == -1)
                        {
                            //For the pages of the stream table itself, we list these as belonging to "index -1"
                            if (pdbMerger.pdbFile is PDB7File v7)
                                siPageList = v7.StreamTableLocation.PageList;
                            else
                            {
                                //In V2 mpspnpnSt lists the pages of the stream table, not the pages that the stream table's pages are found in
                                var rawPages = ((PDB2File) pdbMerger.pdbFile).MsfHeader.StreamTablePageList;

                                var arr = new PN[rawPages.Length];

                                for (var i = 0; i < rawPages.Length; i++)
                                    arr[i] = rawPages[i];

                                siPageList = arr;
                            }
                        }
                        else if (siIndex == -2)
                        {
                            //It's a page describing the location of the stream table's pages
                            siPageList = ((PDB7File) pdbMerger.pdbFile).MsfHeader.PagesOfStreamTablePageList;
                        }
                        else
                        {
                            ref var si = ref pdbMerger.pdbFile.StreamTable!.StreamInfos[siIndex];
                            siPageList = si.PageList;
                        }

                        var nextPageFound = false;

                        var secondStartOffset = 0;

                        for (var i = 0; i < siPageList.Length; i++)
                        {
                            if (siPageList[i] == currentPage)
                            {
                                //The next page in the list is the one that our split value begins from
                                var nextPage = siPageList[i + 1];
                                secondStartOffset = nextPage * pdbMerger.pdbFile.PageSize;
                                nextPageFound = true;
                                break;
                            }
                        }

                        if (!nextPageFound)
                            throw new NotImplementedException();

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

                currentList.Add(nextValue!);
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
                        else if (view is ValueView<string> || view is ByteBlobView)
                        {
                            return;
                        }

                        throw new NotImplementedException();
                    }

                    for (var j = 0; j < directory.Children.Length; j++)
                    {
                        var child = directory.Children[j];

                        if (child.Offset >= replacement.Offset)
                        {
                            //This is the first overlapping child

                            var replacementEnd = replacement.Offset + replacement.Size - 1;

                            var k = j + 1;

                            for (; k < directory.Children.Length; k++)
                            {
                                var endChild = directory.Children[k];

                                if (endChild.Offset > replacementEnd)
                                    break;

                                //endChild is marked for deletion. Let's double check that it's indeed safe to delete
                                CheckSafeToDelete(endChild);
                            }

                            var numItemsToReplace = k - j - 1;

                            var newViews = new List<IView>();

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

                                var views = extension.ReadBytes(ref junkStart, lastChildEnd, null, null, getRVA, false);

                                //This method messes with global state. Backup our global state so that we can restore it afterwards

                                var oldRepeatingTypeList = repeatingTypeList;
                                var oldRepeatingGroupMode = repeatingGroupMode;
                                var oldCurrentList = currentList;

                                repeatingTypeList = new List<IView>();
                                repeatingGroupMode = 0;
                                currentList = new List<IView>();

                                ProcessParsedByteViews(views!);

                                FinalizeRepeatingTypeRegion();

                                newViews.AddRange(currentList);

                                repeatingTypeList = oldRepeatingTypeList;
                                repeatingGroupMode = oldRepeatingGroupMode;
                                currentList = oldCurrentList;
                            }

                            //Add all other children after the junk we're replacing in the directory to the new list of children
                            for (var l = k; l < directory.Children.Length; l++)
                                newViews.Add(directory.Children[l]);

                            //We've got everything we need now. Patch the original directory!
                            masterList[i] = new LogicalRegionView(directory.Offset, directory.Name, newViews.ToArray(), directory.Kind, directory.Size);
                            return;
                        }
                    }

                    throw new NotImplementedException();
                }
            }

            throw new NotImplementedException();
        }

        void FinalizeDirectoryRegion(RawOffset endRva, ref RawOffset currentEnd)
        {
            if (directory == null)
                return;

            FinalizeRepeatingTypeRegion();

            Debug.Assert(currentList.Count > 0);

            if (currentList.Count == 1 && currentList[0] is not ByteBlobView && this is not PdbMsfMerger) //When constructing PDB Views, even if we have one big value that takes up an entire page, it should still be wrapped in a page logical view
            {
                //Only one item; no point creating a region view around it. But if it's a byte blob, we likely don't support this directory yet, so we should create a region around it
                //so that it's clear that something is supposed to be there
                masterList.Add(currentList[0]);
            }
            else
            {
                //We've been building up the members of a directory

                var directoryRegion = new LogicalRegionView(directory.Value.Start, directory.Value.Name, currentList.ToArray(), this is PdbMsfMerger ? ViewKind.Page : ViewKind.DataDirectory, (int) (directory.Value.End - directory.Value.Start));

                masterList.Add(directoryRegion);
            }

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
                    break;

                case RepeatingGroupMode.ImportFunctionNames:
                    regionKind = ViewKind.ImportStrings;
                    break;

                case RepeatingGroupMode.DelayImportFunctionNames:
                    regionKind = ViewKind.DelayImportStrings;
                    break;

                default:
                    throw new NotImplementedException($"Don't know how to handle {nameof(RepeatingGroupMode)} '{repeatingGroupMode}'");
            }

            var size = repeatingTypeList.Sum(v => v.Size);
            currentList.Add(new LogicalRegionView(repeatingTypeList[0].Offset, regionKind.GetDescription(), repeatingTypeList.Cast<IView>().ToArray(), regionKind, size));
            repeatingTypeList.Clear();
            repeatingGroupMode = 0;
        }

        void ReadByteBlob(ref RawOffset rva, RawOffset end, RawOffset endRva, Func<int, int>? getRealOffset, Func<int, int>? getRVA, bool isOverlay)
        {
            var dirIndex = directory == null ? nextDataDirectoryIndex : nextDataDirectoryIndex + 1;

            if (dirIndex < discoveredDataDirectories.Count)
            {
                var nextDirectory = discoveredDataDirectories[dirIndex];

                //Try and protect against directories listing bogus addresses that overlap with each other.
                //This logic was initially implemented to protect against issues with the security directory...until I realized that the security directory uses absolute addressing. But this logic
                //is still useful for handling dodgy PE Files
                if (nextDirectory.Start > rva && end >= nextDirectory.Start && rva != nextDirectory.Start)
                {
                    var newEnd = (RawOffset) Math.Min((int) end, (int) nextDirectory.Start);

                    end = newEnd;
                }
            }

            var views = extension.ReadBytes(ref rva, end, null, getRealOffset, getRVA, isOverlay);

            if (isOverlay && views == null)
            {
                rva = endRva;
                return;
            }

            switch (repeatingGroupMode)
            {
                case RepeatingGroupMode.ImportFunctionNames:
                case RepeatingGroupMode.DelayImportFunctionNames:
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
