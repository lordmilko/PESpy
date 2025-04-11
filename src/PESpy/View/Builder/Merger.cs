using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.View.Builder
{
    abstract class Merger
    {
        private List<IView> sortedStructs;
        private HashSet<IView> delayNameViews;
        private List<DirectoryInfo> discoveredDataDirectories;

        private int nextStructIndex;
        private int nextDataDirectoryIndex;
        private IView nextValue;
        private DirectoryInfo? directory;
        private Extension extension;
        private RepeatingGroupMode repeatingGroupMode;

        private List<IView> masterList = new List<IView>();
        private List<IView> currentList = new List<IView>();
        private List<IView> repeatingTypeList = new List<IView>();

        protected Merger(
            List<IView> sortedStructs,
            HashSet<IView> delayNameViews,
            List<DirectoryInfo> discoveredDataDirectories,
            Extension extension)
        {
            this.sortedStructs = sortedStructs;
            this.delayNameViews = delayNameViews;
            this.discoveredDataDirectories = discoveredDataDirectories;
            this.extension = extension;
        }

        internal abstract IView[] Merge();

        protected IView[] BuildSection(
            RawOffset startRva,
            RawOffset endRva,
            Func<int, int> getRealOffset,
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

                GetValueOrBytes(ref rva, currentEnd, endRva, getRealOffset, isOverlay);
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

        private void GetValueOrBytes(ref RawOffset rva, RawOffset currentEnd, RawOffset endRva, Func<int, int> getRealOffset, bool isOverlay)
        {
            if (nextStructIndex < sortedStructs.Count && (nextValue = sortedStructs[nextStructIndex]).Offset < currentEnd)
            {
#if DEBUG
                if (rva > nextValue.Offset)
                {
                    var previous = sortedStructs[nextStructIndex - 1];

                    Debug.Assert(rva <= nextValue.Offset);
                }
#endif

                if (nextValue.Offset == rva)
                {
                    //If we see a bunch of strings in a row, synthesize a virtual strings region.
                    //If we see a bunch of logical regions with the same in a row, synthesize a virtual region around them

                    ProcessValueOrRepeatingGroup();

                    Debug.Assert(nextValue.Size != 0);
                    rva += nextValue.Size - 1;
                    nextStructIndex++;
                }
                else
                {
                    //Read all bytes up to the next metadata item
                    ReadByteBlob(ref rva, nextValue.Offset, endRva, getRealOffset, isOverlay);
                }
            }
            else
            {
                //Read all bytes to the end
                ReadByteBlob(ref rva, currentEnd, endRva, getRealOffset, isOverlay);
            }
        }

        private void ProcessValueOrRepeatingGroup()
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
                currentList.Add(nextValue);
            }
        }

        void FinalizeDirectoryRegion(RawOffset endRva, ref RawOffset currentEnd)
        {
            if (directory == null)
                return;

            FinalizeRepeatingTypeRegion();

            Debug.Assert(currentList.Count > 0);

            if (currentList.Count == 1 && currentList[0] is not ByteBlobView)
            {
                //Only one item; no point creating a region view around it. But if it's a byte blob, we likely don't support this directory yet, so we should create a region around it
                //so that it's clear that something is supposed to be there
                masterList.Add(currentList[0]);
            }
            else
            {
                //We've been building up the members of a directory

                var directoryRegion = new LogicalRegionView(directory.Value.Start, directory.Value.Name, currentList.ToArray(), this is PdbMerger ? ViewKind.Page : ViewKind.DataDirectory, (int) (directory.Value.End - directory.Value.Start));

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

        void ReadByteBlob(ref RawOffset rva, RawOffset end, RawOffset endRva, Func<int, int> getRealOffset, bool isOverlay)
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

            var views = extension.ReadBytes(ref rva, end, null, getRealOffset, isOverlay);

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

                    ProcessParsedByteViews(views);
                    break;

                default: //It's some other type of repeating group. Finalize it, and then parse the results of the byte array
                    FinalizeRepeatingTypeRegion();
                    ProcessParsedByteViews(views);
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
