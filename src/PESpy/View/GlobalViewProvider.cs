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
        Region
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

        public unsafe void WriteChild(int index, ref StructWriter structWriter)
        {
            var sw = Stopwatch.StartNew();

            ViewEntity entity;
            DirectoryInfo directoryInfo;

            if (index != _nextChild)
                        if (_fileAccessor.TryGetDirectory(entity.TargetAddress, out directoryInfo))
                            _entities.MoveTo(directoryInfo.End);
                        else if (_fileAccessor.TryGetRegion(entity.TargetAddress, _depthAtStartOffset, out var region))
                            _entities.MoveTo(region.End);
                    }
                    else
                    {
                        if (_fileAccessor.TryGetRegion(entity.TargetAddress, _depthAtStartOffset, out var region))
                            _entities.MoveTo(region.End);
                    }
                }

                _nextChild = index;
            }
            entity = _entities.Current;
            int childOffset;

            if (_kind != GlobalViewProviderKind.Region && _fileAccessor.TryGetDirectory(entity.TargetAddress, (childOffset = (entity.TargetAddress == _entities.StartTargetAddress && _kind == GlobalViewProviderKind.Directory ? _depthAtStartOffset + 1 : 0)), out directory))
            {
                structWriter.Field = new LogicalRegionView(
                    directory,
                    _fileAccessor,
                    structWriter.ViewWriter,
                    _entities.SliceFromCurrent(directory.Length),
                    childOffset
                );

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

                //Skip over the region
                _entities.MoveTo(region.End);

                _nextChild++;
                return;
            }

            //Now write the appropriate view based on the type of the entity

            if (entity.Kind != 0)
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

                                if (entity.ViewByte->IsWide)
                                    structWriter.Field = new ValueView<FixedUtf16String>(entity.TargetAddress, new FixedUtf16String((char*) (byte*) entity.Bytes, entity.Length), entity.Length, ViewKind.String);
                                else
                                    structWriter.Field = new ValueView<FixedUtf8String>(entity.TargetAddress, new FixedUtf8String((byte*) entity.Bytes, entity.Length), entity.Length, ViewKind.String);
                                break;

                            case ViewByteDataKind.Unknown:
                                if (entity.IsSplit)
                                    throw new NotImplementedException(); //Just get the bytes before the split?

                                structWriter.Field = new ByteBlobView(entity.TargetAddress, entity.Bytes, null);
                                break;

                            default:
                                throw new NotImplementedException();
                        }
                    case ViewByteKind.Unknown:
                        if (entity.IsSplit)
                            throw new NotImplementedException(); //Just get the bytes before the split?

                        structWriter.Field = new ByteBlobView(entity.TargetAddress, entity.Bytes, null);
                        break;
                        break;

                    default:
                        throw new NotImplementedException();
                }
            }

            _nextChild++;
        }
    }
}
