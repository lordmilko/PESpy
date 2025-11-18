using System;
using System.Collections.Generic;
using System.Diagnostics;
using PESpy.PDB;
using PESpy.View.Builder;

namespace PESpy.View
{
    internal unsafe class PDBViewByteViewWriter : PDBViewWriter
    {
        private readonly FileAccessor _fileAccessor;
        private readonly int _pageSize;
        private readonly FileAnalyzer _fileAnalyzer;
        private readonly Dictionary<PN, int> _pageNumberToSIIndex;

        internal PDBViewByteViewWriter(
            PDBFile pdbFile,
            FileAccessor fileAccessor,
            FileAnalyzer fileAnalyzer,
            Dictionary<PN, int> pageNumberToSIIndex) : base(pdbFile)
        {
            _fileAccessor = fileAccessor;
            _pageSize = pdbFile.PageSize;
            _fileAnalyzer = fileAnalyzer;
            _pageNumberToSIIndex = pageNumberToSIIndex;
        }

        protected internal override IView? NewStruct<T>(FixedUtf8String name, in T value, ViewKind kind, int structSize)
        {
            //Every struct will call NewStruct(), so we want to take steps to minimize its size in NativeAOT
            return RegisterStruct(name, value.Offset, kind, structSize);
        }

        protected internal override IView? NewValue<T>(int offset, in T value, int size, ViewKind kind)
        {
            throw new NotImplementedException();
        }

        private IView? RegisterStruct(FixedUtf8String name, int offset, ViewKind kind, int structSize)
        {
            var pViewByte = _fileAccessor.GetViewByte(offset, out var sectionAccessorIndex);
            pViewByte->Kind = ViewByteKind.Data;
            pViewByte->DataKind = ViewByteDataKind.Struct;
            _fileAnalyzer.AddName(offset, pViewByte, name);
            _fileAccessor.AddStructKind(offset, kind);

            var pageSize = _pageSize;

            var relativeOffset = offset % pageSize;

            var dataEndOffset = relativeOffset + structSize;

            if (dataEndOffset < pageSize)
            {
                //The value fits within the current page

                for (var i = pViewByte + 1; i < pViewByte + structSize; i++)
                    i->Kind = ViewByteKind.Body;
            }
            else
            {
                var currentSegmentStart = offset;
                var numBytesToWrite = 0;
                var remaining = structSize - 1; //We already wrote the first byte

                //The value spans multiple pages. The question now is, what should we do?

                //Each time we move to a new section accessor, we loop again
                while (true)
                {
                    ref var sectionAccessor = ref _fileAccessor.SectionAccessors[sectionAccessorIndex];

                    if (sectionAccessor.Kind == SectionAccessorKind.Page)
                    {
                        //The section accessor only has room for a single page here; we need to write to the end,
                        //lookup where the next page begins, get the corresponding view byte and section accessor index for it
                        //and loop again
                        numBytesToWrite = Math.Min(pageSize - relativeOffset, remaining);
                    }
                    else
                    {
                        //We can write multiple page's worth in one go
                        numBytesToWrite = Math.Min(remaining, sectionAccessor.EndAddress - currentSegmentStart);
                    }

                    //I believe (but haven't confirmed) the compiler will check if the condition is constant
                    //and if so move it out of the loop so it isn't re-evaluated each time
                    for (var i = pViewByte + 1; i < pViewByte + numBytesToWrite; i++)
                        i->Kind = ViewByteKind.Body;

                    remaining -= numBytesToWrite;

                    //If we're a multi-page section, we want GetNextPageOffset to get the page after the last page in the section range we just processed, not the first page in the range
                    currentSegmentStart += numBytesToWrite - 1;

                    Debug.Assert(remaining >= 0);

                    if (remaining == 0)
                        break;

                    //Whatever we just wrote ends in a split tail then
                    (pViewByte + numBytesToWrite - 1)->BodyKind = ViewByteBodyKind.SplitTail;

                    currentSegmentStart = Merger.GetNextPageOffset(pdbFile, currentSegmentStart, _pageNumberToSIIndex);
                    relativeOffset = 0;

                    pViewByte = _fileAccessor.GetViewByte(currentSegmentStart, out sectionAccessorIndex) - 1; //The body builder always does +1
                    (pViewByte + 1)->Kind = ViewByteKind.Body; //Need to set this to mark it as the head of a split
                    (pViewByte + 1)->BodyKind = ViewByteBodyKind.SplitHead;
                }
            }            

            //Return null here to reduce size of NewStruct in NativeAOT
            return null;
        }
    }
}
