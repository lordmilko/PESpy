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
                _bytesRead - Current.Length,
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

            IList<RegionBuilder> regions = fileAccessor.TopLevelRegions;
            var nextRegionIndex = 0;
            var nextRegionOffset = -1;

            var targetStart = sectionAccessor.StartAddress + _startOffset;

            if (regions?.Count > 0)
            {
                //Find the first directory prior to the start of this section

                FindStartRegion(regions, targetStart, ref nextRegionIndex, ref nextRegionOffset);

                if (kind == GlobalViewProviderKind.Region)
                    DrillIntoRegion(ref regions, depthAtStartOffset, ref nextRegionIndex, ref nextRegionOffset);
            }

            if (kind != GlobalViewProviderKind.Region)
            {
                IList<RegionBuilder> dataDirectories = fileAccessor.TopLevelDirectories;
                var nextDataDirectoryIndex = 0;
                var nextDataDirectoryOffset = -1;

                if (dataDirectories?.Count > 0)
                {
                    //Find the first directory prior to the start of this section

                    FindStartRegion(dataDirectories, targetStart, ref nextDataDirectoryIndex, ref nextDataDirectoryOffset);

                    if (kind == GlobalViewProviderKind.Directory)
                        DrillIntoRegion(ref dataDirectories, depthAtStartOffset, ref nextDataDirectoryIndex, ref nextDataDirectoryOffset);
                }

                while (bytesRead < sectionLength)
                {
                    //We can't use unsafe in an iterator, so we need to put all the logic in the FileEntity ctor
                    Debug.Assert(_names != null);
                    var entity = new ViewEntity(symbolAccessor, sectionAccessor, bytesRead, sectionLength, pBytes, infoMap, names, largeAddresses, measureOnly: true);
                    Debug.Assert(entity.ViewByte->Kind != ViewByteKind.Body);

                    if (entity.TargetAddress == nextDataDirectoryOffset)
                    {
                        SkipOverDirectory(dataDirectories, sectionAccessor, ref bytesRead, ref nextDataDirectoryIndex, ref nextDataDirectoryOffset);
                        SkipOverDirectory(
                            dataDirectories,
                            sectionAccessor,
                            ref bytesRead,
                            ref nextDataDirectoryIndex,
                            ref nextDataDirectoryOffset,
                            regions,
                            ref nextRegionIndex,
                            ref nextRegionOffset
                        );
                    }
                    else if (entity.TargetAddress == nextRegionOffset)
                    {
                        SkipOverRegion(regions, sectionAccessor, ref bytesRead, ref nextRegionIndex, ref nextRegionOffset);
                    }
                    else
                    {
                        bytesRead += entity.Length;

#if DEBUG
                        if (nextDataDirectoryOffset != -1)
                            Debug.Assert(entity.TargetAddress < nextDataDirectoryOffset);

                        if (nextRegionOffset != -1)
                            Debug.Assert(entity.TargetAddress < nextRegionOffset);
#endif
                    }

                    count++;
                }

#if DEBUG
                if (dataDirectories != null && nextDataDirectoryIndex < dataDirectories.Count)
                {
                    var dataDirectory = dataDirectories[nextDataDirectoryIndex];

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

                    if (entity.TargetAddress == nextRegionOffset)
                    {
                        SkipOverRegion(regions, sectionAccessor, ref bytesRead, ref nextRegionIndex, ref nextRegionOffset);
                    }
                    else
                    {
                        bytesRead += entity.Length;

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
            IList<RegionBuilder> dataDirectories,
            in SectionAccessor sectionAccessor,
            ref int bytesRead,
            ref int nextDataDirectoryIndex,
            ref int nextDataDirectoryOffset,
            IList<RegionBuilder> regions,
            ref int nextRegionIndex,
            ref int nextRegionOffset)
        {
            ref var directory = ref dataDirectories[nextDataDirectoryIndex];

            var endOffset = directory.End - sectionAccessor.StartAddress;

            //If the next region we want to read was inside this directory, we need to skip over that too
            while (nextRegionOffset != -1 && nextRegionOffset < directory.End)
            {
                var temp = bytesRead;

                SkipOverRegion(regions, sectionAccessor, ref temp, ref nextRegionIndex, ref nextRegionOffset);
            }

            //This entity will be subsumed within a data directory, so it won't count. Skip all other entities
            //that would be contained within this directory
            bytesRead = endOffset;

            nextDataDirectoryIndex++;

        repeat:
            if (nextDataDirectoryIndex < dataDirectories.Count)
            {
                //Watch out for multiple directories sharing the same bounds! e.g. ExeptionTableDirectory
                //can share the same bounds as R2R RuntimeFunctionsDirectory
                var nextDirectory = dataDirectories[nextDataDirectoryIndex];

                if (nextDirectory.Start == directory.Start)
                {
                    nextDataDirectoryIndex++;
                    goto repeat;
                }

                nextDataDirectoryOffset = nextDirectory.Start;
            }
            else
                nextDataDirectoryOffset = -1;
        }

        private void SkipOverRegion(
            IList<RegionBuilder> regions,
            in SectionAccessor sectionAccessor,
            ref int bytesRead,
            ref int nextRegionIndex,
            ref int nextRegionOffset)
        {
            var region = regions[nextRegionIndex];

            var endOffset = region.End - sectionAccessor.StartAddress;

            bytesRead = endOffset;

            nextRegionIndex++;

        repeat:
            if (nextRegionIndex < regions.Count)
            {
                //Watch out for multiple directories sharing the same bounds! e.g. ExeptionTableDirectory
                //can share the same bounds as R2R RuntimeFunctionsDirectory
                var nextRegion = regions[nextRegionIndex];

                if (nextRegion.Start == region.Start)
                {
                    nextRegionIndex++;

                    goto repeat;
                }

                nextRegionOffset = nextRegion.Start;
            }
            else
                nextRegionOffset = -1;
        }

        public void Reset()
        {
            _bytesRead = _startOffset;
        }

        private ViewEntity GetEntity() => new ViewEntity(_symbolAccessor, SectionAccessor, _bytesRead, _sectionLength, _pBytes, _infoMap, _names, _largeAddresses);
    }
}
