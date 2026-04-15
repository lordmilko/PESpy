using System;
using System.Collections.Generic;
using System.Diagnostics;
using PESpy.View.Builder;

namespace PESpy.View
{
    internal enum GlobalViewProviderKind
    {
        Global,
        Directory,
        Region,
        NestedFile
    }

    internal class GlobalViewProvider : IViewable
    {
        private readonly int _sectionIndex;
        private readonly FileAccessor _fileAccessor;
        private readonly GlobalViewProviderKind _kind;

        private int _numChildren;

        //As long as we keep iterating children serially, we can continue reusing our enumerator
        private int _nextChild = -1;
        private int _depthAtStartOffset;
        private ViewEntityIterator _entities;

        public GlobalViewProvider(int sectionIndex, FileAccessor fileAccessor)
        {
            _sectionIndex = sectionIndex;
            _fileAccessor = fileAccessor;
            _kind = GlobalViewProviderKind.Global;
            _entities = _fileAccessor.EnumerateEntities(_sectionIndex);
        }

        public GlobalViewProvider(
            in ViewEntityIterator iterator,
            FileAccessor fileAccessor,
            GlobalViewProviderKind kind,
            int depthAtStartOffset)
        {
            _sectionIndex = iterator.SectionAccessorIndex;
            _fileAccessor = fileAccessor;
            _entities = iterator;
            _kind = kind;
            _depthAtStartOffset = depthAtStartOffset;
        }

        public void WriteGlobals(ViewWriter writer) => throw new NotImplementedException();

        public IView? WriteStruct(ViewWriter writer) => throw new NotImplementedException();

        public int NumChildren()
        {
            var sw = Stopwatch.StartNew();

            if (_numChildren == 0)
                _numChildren = _entities.GetCount(_fileAccessor, _kind, _depthAtStartOffset);

            return _numChildren;
        }

#if DEBUG_VIEWENTITY
        private List<object> _debugEntities = new List<object>();
#endif

        public unsafe void WriteChild(int index, ref StructWriter structWriter)
        {
            var sw = Stopwatch.StartNew();

            ViewEntity entity;
            RegionBuilder directory;
            NestedFileRange nestedFileRange;

            if (index != _nextChild)
            {
                //Construct a new enumerator and fast forward to the specified child

                _entities.Reset();

                for (var i = 0; i < index; i++)
                {
                    //We're supposed to know how many children we have
                    if (!_entities.MoveNext())
                        throw new InvalidOperationException($"Failed to move to child {index}: ran out of children while processing index {i}. This may potentially indicate a critical error between {nameof(GlobalViewProvider)} and{nameof(ViewEntityIterator)}.");

                    entity = _entities.Current;

                    if (_kind == GlobalViewProviderKind.Global)
                    {
                        //An entity that represents the first child of a directory or region could appear at any time,
                        //so we need to check after each child to see if it's directory time

                        if (_fileAccessor.TryGetNestedFileRange(entity.TargetAddress, out nestedFileRange) && structWriter.ViewWriter is not NestedPEViewWriter)
                            _entities.MoveTo(nestedFileRange.EndOffset);
                        if (_fileAccessor.TryGetDirectory(entity.TargetAddress, _depthAtStartOffset, out directory))
                            _entities.MoveTo(directory.End);
                        else if (_fileAccessor.TryGetRegion(entity.TargetAddress, _depthAtStartOffset, out var region))
                            _entities.MoveTo(region.End);
                    }
                    else
                    {
                        if (_kind != GlobalViewProviderKind.NestedFile && _fileAccessor.TryGetNestedFileRange(entity.TargetAddress, out nestedFileRange) && structWriter.ViewWriter is not NestedPEViewWriter)
                            _entities.MoveTo(nestedFileRange.EndOffset);
                        else if (_fileAccessor.TryGetRegion(entity.TargetAddress, _depthAtStartOffset, out var region))
                            _entities.MoveTo(region.End);
                    }
                }

                _nextChild = index;
            }
            entity = _entities.Current;
            int childOffset;

            //Watch our for a struct inside of a region inside of a nested file
            if (_kind != GlobalViewProviderKind.NestedFile && _fileAccessor.TryGetNestedFileRange(entity.TargetAddress, out nestedFileRange) && structWriter.ViewWriter is not NestedPEViewWriter)
            {
                structWriter.Field = new FileView(
                    nestedFileRange,
                    _fileAccessor,
                    _entities.SliceFromCurrent(nestedFileRange.Length)
                );

#if DEBUG_VIEWENTITY
                _debugEntities.Add(structWriter.Field);
#endif

                //Skip over the file
                _entities.MoveTo(nestedFileRange.EndOffset);

                _nextChild++;
                return;
            }
            else if (_kind != GlobalViewProviderKind.Region && _fileAccessor.TryGetDirectory(entity.TargetAddress, (childOffset = (entity.TargetAddress == _entities.StartTargetAddress && _kind == GlobalViewProviderKind.Directory ? _depthAtStartOffset + 1 : 0)), out directory))
            {
                structWriter.Field = new LogicalRegionView(
                    directory,
                    _fileAccessor,
                    structWriter.ViewWriter,
                    _entities.SliceFromCurrent(directory.Length),
                    childOffset
                );

#if DEBUG_VIEWENTITY
                _debugEntities.Add(structWriter.Field);
#endif

                //Skip over the directory
                _entities.MoveTo(directory.End);

                _nextChild++;
                return;
            }
            else if (_fileAccessor.TryGetRegion(entity.TargetAddress, (childOffset = (entity.TargetAddress == _entities.StartTargetAddress && _kind == GlobalViewProviderKind.Region ? _depthAtStartOffset + 1 : 0)), out var region))
            {
                structWriter.Field = new LogicalRegionView(
                    region,
                    _fileAccessor,
                    structWriter.ViewWriter,
                    _entities.SliceFromCurrent(region.Length),
                    childOffset
                );

                Debug.Assert(region.Length > 0);

#if DEBUG_VIEWENTITY
                _debugEntities.Add(structWriter.Field);
#endif

                //Skip over the region
                _entities.MoveTo(region.End);

                _nextChild++;
                return;
            }

            //Now write the appropriate view based on the type of the entity

            if (entity.Kind != 0)
            {
                structWriter.Field = ViewProvider.CreateStructView(entity.Kind, entity.Length, _fileAccessor.GetMemoryChunkFromAddress(entity.TargetAddress), structWriter.ViewWriter, entity.IsSplit);
            }
            else
            {
                switch (entity.ViewByte->Kind)
                {
                    case ViewByteKind.Data:
                        switch (entity.ViewByte->DataKind)
                        {
                            case ViewByteDataKind.Padding:
                                if (entity.IsSplit)
                                    throw new NotImplementedException(); //Just get the bytes before the split?

                                structWriter.Field = new ByteBlobView(entity.TargetAddress, entity.Bytes, null);
                                break;

                            case ViewByteDataKind.String:
                                if (entity.IsSplit)
                                    throw new NotImplementedException(); //Just get the bytes before the split?

                                //Note that even if it _is_ null terminated, we _do_ still want to include the null terminator
                                //in the name so we can print it properly
                                if (entity.ViewByte->IsWide)
                                {
                                    var str = new FixedUtf16String((char*) (byte*) entity.Bytes, entity.Length / 2);
                                    structWriter.Field = new ValueView<FixedUtf16String>(entity.TargetAddress, str, entity.Length, ViewKind.String, entity.Name);
                                }
                                else
                                {
                                    var str = new FixedUtf8String((byte*) entity.Bytes, entity.Length);
                                    structWriter.Field = new ValueView<FixedUtf8String>(entity.TargetAddress, str, entity.Length, ViewKind.String, entity.Name);
                                }
                                break;

                            case ViewByteDataKind.Unknown:
                                if (entity.IsSplit)
                                    throw new NotImplementedException(); //Just get the bytes before the split?

                                structWriter.Field = new ByteBlobView(entity.TargetAddress, entity.Bytes, null, entity.Name);
                                break;

                            case ViewByteDataKind.Decimal:
                                if (entity.IsSplit)
                                if (entity.Length == 4)
                                    structWriter.Field = new ValueView<float>(entity.TargetAddress, *(float*) (byte*) entity.Bytes, entity.Length, ViewKind.Decimal, entity.Name);
                                else
                                    structWriter.Field = new ValueView<double>(entity.TargetAddress, *(double*) (byte*) entity.Bytes, entity.Length, ViewKind.Decimal, entity.Name);

                                break;

                            default:
                                throw new NotImplementedException();
                        }
                        break;

                    case ViewByteKind.Code:
                        if (entity.IsSplit)
                        var range = new AsmRange<object>(startOffset: entity.TargetAddress, startRVA: entity.TargetAddress, functionRVA: (int) (entity.TargetAddress - entity.Displacement), entity.Name);
                        range.EndOffset = entity.TargetAddress + entity.Length;

                        structWriter.Field = new AsmView<object>(entity.TargetAddress, (byte) _fileAccessor.Bitness, range, entity.ViewByte->IsIL ? ViewKind.IL : ViewKind.Assembly);
                        break;

                    case ViewByteKind.Unknown:
                        if (entity.IsSplit)
                            throw new NotImplementedException(); //Just get the bytes before the split?

                        structWriter.Field = new ByteBlobView(entity.TargetAddress, entity.Bytes, null);
                        break;

                    case ViewByteKind.Body:
                        Debug.Assert(entity.ViewByte->BodyKind == ViewByteBodyKind.SplitHead);

                        //We want to create a view that just encapsulates the portion that this body encapsulates.
                        //First, we need to rewind to get the head

                        var origin = entity.GetSplitHeadOrigin(_fileAccessor, out var bytesRewound);

                        if (origin.Kind != 0)
                        {
                            //We use origin.TargetAddress to get the head; we're then going to split it at the point we're actually after
                            var baseView = ViewProvider.CreateStructView(origin.Kind, entity.Length, _fileAccessor.GetMemoryChunkFromAddress(origin.TargetAddress), structWriter.ViewWriter, entity.IsSplit);
                            if (baseView is ISplittableView sv)
                            {
                                var (first, second) = sv.Split(entity.TargetAddress, baseView.Offset + bytesRewound);
                                structWriter.Field = second;
                            }
                        break;

                    default:
                        throw new NotImplementedException();
                }
            }

#if DEBUG_VIEWENTITY
            _debugEntities.Add(structWriter.Field);
#endif

            _nextChild++;
        }
    }
}
