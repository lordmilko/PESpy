using System;
using System.Collections.Generic;
using System.Diagnostics;
using PESpy.View.Builder;

namespace PESpy.View
{
    public struct ViewEntityIterator
    {
        private readonly ISymbolAccessor _symbolAccessor;
        internal readonly SectionAccessor SectionAccessor;
        internal readonly int SectionAccessorIndex;
        private readonly int _startOffset;
        internal readonly int StartTargetAddress;
        private readonly int _sectionLength;
        private readonly IntPtr _pBytes;
        private readonly Dictionary<int, FileAccessor.ViewInfo> _infoMap;
        private readonly FixedUtf8String[] _names;
        private readonly Dictionary<int, int> _largeAddresses;

        private int _bytesRead;
        private bool _hasMovedNext; //When we SliceFromCurrent, if we haven't called MoveNext after calling MoveTo, bytesRead hasn't been incremented so we don't need to subtract Current.Length
        private ViewEntity _current;

        internal ViewEntityIterator(
            int startOffset,
            ISymbolAccessor symbolAccessor,
            in SectionAccessor sectionAccessor,
            int sectionAccessorIndex,
            int sectionLength,
            IntPtr pBytes,
            Dictionary<int, FileAccessor.ViewInfo> infoMap,
            FixedUtf8String[] names,
            Dictionary<int, int> largeAddresses)
        {
            _startOffset = startOffset;
            _bytesRead = startOffset;
            _symbolAccessor = symbolAccessor;
            SectionAccessor = sectionAccessor;
            StartTargetAddress = _startOffset + SectionAccessor.StartAddress;
            SectionAccessorIndex = sectionAccessorIndex;
            _sectionLength = startOffset + sectionLength;
            _pBytes = pBytes;
            _infoMap = infoMap;
            _names = names;
            _largeAddresses = largeAddresses;
        }

        public ViewEntityIterator SliceFromCurrent(int length)
        {
            return new ViewEntityIterator(
                _hasMovedNext ? _bytesRead - Current.Length : _bytesRead,
                _symbolAccessor,
                SectionAccessor,
                SectionAccessorIndex,
                length,
                _pBytes,
                _infoMap,
                _names,
                _largeAddresses
            );
        }

        public ViewEntity Current => _current;

        public bool MoveNext()
        {
            if (_bytesRead < _sectionLength)
            {
                _hasMovedNext = true;
                _current = GetEntity();
                _bytesRead += _current.Length;
                return true;
            }

            //Don't clear out Current so we can rewind it if we want
            return false;
        }

        public bool MovePrevious()
        {
            if (_bytesRead == _startOffset)
                return false;

            //Rewind the current entity
            _bytesRead -= Current.Length;
            _current = GetEntity();
            return true;
        }

        public bool MoveTo(int offset)
        {
            _bytesRead = offset - SectionAccessor.StartAddress;
            Debug.Assert(_bytesRead >= _startOffset);

            if (_bytesRead >= _sectionLength)
                return false;

            _current = GetEntity();
            return true;
        }

#if DEBUG_VIEWENTITY
        //Used to debug differences between ViewEntityIterator and GlobalViewProvider
        private List<object> _debugEntities = new List<object>();
#endif

        internal struct CountState
        {
            public IList<RegionBuilder> Regions;
            public int NextRegionIndex;
            public int NextRegionOffset;
            public bool HasRegions => Regions?.Count > 0;

            public NestedFileRange[] NestedFiles;
            public int NextNestedFileIndex;
            public int NextNestedFileOffset;
            public bool HasNestedFiles => NestedFiles?.Length > 0;

            public IList<RegionBuilder> DataDirectories;
            public int NextDataDirectoryIndex;
            public int NextDataDirectoryOffset;
            public bool HasDataDirectories => DataDirectories?.Count > 0;
        }

        internal unsafe int GetCount(FileAccessor fileAccessor, GlobalViewProviderKind kind, int depthAtStartOffset)
        {
            var bytesRead = _startOffset;

            var count = 0;

            var symbolAccessor = _symbolAccessor;
            var sectionAccessor = SectionAccessor;
            var sectionLength = _sectionLength;
            var pBytes = _pBytes;
            var infoMap = _infoMap;
            var names = _names;
            var largeAddresses = _largeAddresses;

            var state = new CountState
            {
                Regions = fileAccessor.TopLevelRegions,
                NextRegionIndex = 0,
                NextRegionOffset = -1,

                NestedFiles = fileAccessor.NestedFileRanges,
                NextNestedFileIndex = 0,
                NextNestedFileOffset = -1,

                NextDataDirectoryOffset = -1,
            };

            var targetStart = sectionAccessor.StartAddress + _startOffset;

            if (state.HasRegions)
            {
                //Find the first region prior to the start of this section

                FindStartRegion(state.Regions, targetStart, ref state.NextRegionIndex, ref state.NextRegionOffset);

                if (kind == GlobalViewProviderKind.Region)
                    DrillIntoRegion(ref state.Regions, depthAtStartOffset, ref state.NextRegionIndex, ref state.NextRegionOffset);
            }

            if (state.HasNestedFiles)
            {
                FindStartNestedFile(targetStart, ref state);
            }

            if (kind != GlobalViewProviderKind.Region)
            {
                state.DataDirectories = fileAccessor.TopLevelDirectories;
                state.NextDataDirectoryIndex = 0;

                if (state.HasDataDirectories)
                {
                    //Find the first directory prior to the start of this section

                    FindStartRegion(state.DataDirectories, targetStart, ref state.NextDataDirectoryIndex, ref state.NextDataDirectoryOffset);

                    if (kind == GlobalViewProviderKind.Directory)
                        DrillIntoRegion(ref state.DataDirectories, depthAtStartOffset, ref state.NextDataDirectoryIndex, ref state.NextDataDirectoryOffset);
                }

                while (bytesRead < sectionLength)
                {
                    //We can't use unsafe in an iterator, so we need to put all the logic in the FileEntity ctor
                    Debug.Assert(_names != null);
                    var entity = new ViewEntity(symbolAccessor, sectionAccessor, bytesRead, sectionLength, pBytes, infoMap, names, largeAddresses, measureOnly: true);
                    Debug.Assert(entity.ViewByte->Kind != ViewByteKind.Body || entity.ViewByte->BodyKind == ViewByteBodyKind.SplitHead);

                    if (entity.TargetAddress == state.NextDataDirectoryOffset)
                    {
                        SkipOverDirectory(dataDirectories, sectionAccessor, ref bytesRead, ref nextDataDirectoryIndex, ref nextDataDirectoryOffset);
#if DEBUG_VIEWENTITY
                        _debugEntities.Add("Directory");
#endif
                        SkipOverDirectory(
                            sectionAccessor,
                            ref bytesRead,
                            ref state
                        );
                    }
                    else if (entity.TargetAddress == state.NextRegionOffset)
                    {
#if DEBUG_VIEWENTITY
                        _debugEntities.Add("Region");
#endif
                        SkipOverRegion(sectionAccessor, ref bytesRead, ref state);
                    }
                    else if (kind != GlobalViewProviderKind.NestedFile && entity.TargetAddress == state.NextNestedFileOffset)
                    {
#if DEBUG_VIEWENTITY
                        _debugEntities.Add("NestedFile");
#endif
                        SkipOverNestedFile(sectionAccessor, ref bytesRead, ref state);
                    }
                    else
                    {
                        bytesRead += entity.Length;

#if DEBUG
                        if (state.NextDataDirectoryOffset != -1)
                            Debug.Assert(entity.TargetAddress < state.NextDataDirectoryOffset);

                        if (state.NextRegionOffset != -1)
                            Debug.Assert(entity.TargetAddress < state.NextRegionOffset);
#endif
#if DEBUG_VIEWENTITY
                        entities.Add(entity);
#endif
                    }

                    count++;
                }

#if DEBUG
                if (state.DataDirectories != null && state.NextDataDirectoryIndex < state.DataDirectories.Count)
                {
                    var dataDirectory = state.DataDirectories[state.NextDataDirectoryIndex];

                    var end = StartTargetAddress + sectionLength;

                    Debug.Assert(dataDirectory.Start >= end, "Failed to process all data directories");
                }
#endif
            }
            else
            {
                while (bytesRead < sectionLength)
                {
                    //We can't use unsafe in an iterator, so we need to put all the logic in the FileEntity ctor
                    Debug.Assert(_names != null);
                    var entity = new ViewEntity(symbolAccessor, sectionAccessor, bytesRead, sectionLength, pBytes, infoMap, names, largeAddresses, measureOnly: true);
                    Debug.Assert(entity.ViewByte->Kind != ViewByteKind.Body);

                    if (entity.TargetAddress == state.NextRegionOffset)
                    {
#if DEBUG_VIEWENTITY
                        _debugEntities.Add("Region");
#endif
                        SkipOverRegion(sectionAccessor, ref bytesRead, ref state);
                    }
                    }
                    else
                    {
                        bytesRead += entity.Length;
#if DEBUG_VIEWENTITY
                        _debugEntities.Add(entity);
#endif
                    }

                    count++;
                }
            }

            return count;
        }

        private void FindStartRegion(
            IList<RegionBuilder> list,
            int targetStart,
            ref int index,
            ref int offset)
        {
            while (index < list.Count)
            {
                var region = list[index];

                if (region.Start < targetStart)
                    index++;
                else
                {
                    offset = region.Start;
                    break;
                }
            }
        }

        //Find the nest nested file at or after the current address
        private void FindStartNestedFile(
            int targetStart,
            ref CountState state)
        {
            while (state.NextNestedFileIndex < state.NestedFiles.Length)
            {
                var nestedFileRange = state.NestedFiles[state.NextNestedFileIndex];

                if (nestedFileRange.StartOffset < targetStart)
                    state.NextNestedFileIndex++;
                else
                {
                    state.NextNestedFileOffset = nestedFileRange.StartOffset;
                    break;
                }
            }
        }

        private void DrillIntoRegion(
            ref IList<RegionBuilder> list,
            int depthAtStartOffset,
            ref int index,
            ref int offset)
        {
            //Drill in to the current depth

            var depthRemaining = depthAtStartOffset + 1;

            while (depthRemaining > 0)
            {
                list = list[index].Children;

                index = 0;
                depthRemaining--;
            }

            if (list == null)
            {
                //No more regions beneath us
                index = 0;
                offset = -1;
            }
            else
            {
                index = 0;
                offset = list[0].Start;
            }
        }

        private void SkipOverDirectory(
            in SectionAccessor sectionAccessor,
            ref int bytesRead,
            ref CountState state)
        {
            var directory = state.DataDirectories[state.NextDataDirectoryIndex];

            var endOffset = directory.End - sectionAccessor.StartAddress;

            //If the next region we want to read was inside this directory, we need to skip over that too
            while (state.NextRegionOffset != -1 && state.NextRegionOffset < directory.End)
            {
                var temp = bytesRead;

                SkipOverRegion(sectionAccessor, ref temp, ref state);
            }

            //This entity will be subsumed within a data directory, so it won't count. Skip all other entities
            //that would be contained within this directory
            bytesRead = endOffset;

            state.NextDataDirectoryIndex++;

        repeat:
            if (state.NextDataDirectoryIndex < state.DataDirectories.Count)
            {
                //Watch out for multiple directories sharing the same bounds! e.g. ExeptionTableDirectory
                //can share the same bounds as R2R RuntimeFunctionsDirectory
                var nextDirectory = state.DataDirectories[state.NextDataDirectoryIndex];

                if (nextDirectory.Start == directory.Start)
                {
                    state.NextDataDirectoryIndex++;
                    goto repeat;
                }

                state.NextDataDirectoryOffset = nextDirectory.Start;
            }
            else
                state.NextDataDirectoryOffset = -1;
        }

        private void SkipOverRegion(
            in SectionAccessor sectionAccessor,
            ref int bytesRead,
            ref CountState state)
        {
            var region = state.Regions[state.NextRegionIndex];

            var endOffset = region.End - sectionAccessor.StartAddress;

            bytesRead = endOffset;

            state.NextRegionIndex++;

            //Note that in the case of a bundle manifest, while there might be a region for the manifest
            //the actual data is still in the overlay outside of the region, so we still won't have to
            //worry about having directories inside of regions

        repeat:
            if (state.NextRegionIndex < state.Regions.Count)
            {
                //Watch out for multiple directories sharing the same bounds! e.g. ExeptionTableDirectory
                //can share the same bounds as R2R RuntimeFunctionsDirectory
                var nextRegion = state.Regions[state.NextRegionIndex];

                if (nextRegion.Start == region.Start)
                {
                    state.NextRegionIndex++;

                    goto repeat;
                }

                state.NextRegionOffset = nextRegion.Start;
            }
            else
                state.NextRegionOffset = -1;
        }

        private void SkipOverNestedFile(
            in SectionAccessor sectionAccessor,
            ref int bytesRead,
            ref CountState state)
        {
            var nestedFile = state.NestedFiles[state.NextNestedFileIndex];

            var endOffset = nestedFile.EndOffset - sectionAccessor.StartAddress;

            //If the next region we want to read was inside this nested file, we need to skip over that too
            while (state.NextRegionOffset != -1 && state.NextRegionOffset < nestedFile.EndOffset)
            {
                var temp = bytesRead;

                SkipOverRegion(sectionAccessor, ref temp, ref state);
            }

            //The same is also true of any directories that may have been contained within the nested file
            while (state.NextDataDirectoryOffset != -1 && state.NextDataDirectoryOffset < nestedFile.EndOffset)
            {
                var temp = bytesRead;

                SkipOverDirectory(sectionAccessor, ref temp, ref state);
            }

            bytesRead = endOffset;

            state.NextNestedFileIndex++;

            if (state.NextNestedFileIndex < state.NestedFiles.Length)
            {
                var nextNestedFile = state.NestedFiles[state.NextNestedFileIndex];

                state.NextNestedFileOffset = nextNestedFile.StartOffset;
            }
            else
                state.NextNestedFileOffset = -1;
        }

        public void Reset()
        {
            _bytesRead = _startOffset;
        }

        private ViewEntity GetEntity() => new ViewEntity(_symbolAccessor, SectionAccessor, _bytesRead, _sectionLength, _pBytes, _infoMap, _names, _largeAddresses);
    }
}
