using System;
using System.Collections.Generic;
using System.Diagnostics;
using PESpy.PDB;
using PESpy.View.Builder;

namespace PESpy.View
{
    internal unsafe class PDBViewByteViewWriter : PDBViewWriter
    {
        private readonly int _pageSize;
        private readonly FileAnalyzer _fileAnalyzer;
        private readonly Dictionary<PN, int> _pageNumberToSIIndex;

        internal PDBViewByteViewWriter(
            PDBFile pdbFile,
            FileAccessor fileAccessor,
            FileAnalyzer fileAnalyzer,
            Dictionary<PN, int> pageNumberToSIIndex) : base(pdbFile, fileAccessor)
        {
            _pageSize = pdbFile.PageSize;
            _fileAnalyzer = fileAnalyzer;
            _pageNumberToSIIndex = pageNumberToSIIndex;
        }

        protected internal override IView? NewUnmanagedStruct<T>(in T value, ViewKind kind, int structSize)
        {
            return RegisterStruct(UnmanagedOffset, kind, structSize);
        }

        protected internal override IView? NewStruct<T>(in T value, ViewKind kind, int structSize)
        {
            //Every struct will call NewStruct(), so we want to take steps to minimize its size in NativeAOT
            return RegisterStruct(value.Offset, kind, structSize);
        }

        protected internal override IView? NewValue<T>(int offset, in T value, int size, ViewKind kind, bool fromRegion)
        {
            return RegisterValue(offset, size, kind, fromRegion);
        }

        public override void WriteGlobalField<T>(int offset, in T value, int size, ViewKind kind)
        {
            RegisterStruct(offset, kind, size);
        }

        private IView? RegisterStruct(int offset, ViewKind kind, int structSize)
        {
            var name = ViewProvider.GetName(kind);

            var pViewByte = _fileAccessor.GetViewByte(offset, out var sectionAccessorIndex);
            pViewByte->Kind = ViewByteKind.Data;
            pViewByte->DataKind = ViewByteDataKind.Struct;
            pViewByte->HasName = true;
            _fileAccessor.AddStructKind(offset, kind);

            SetPagedBody(offset, structSize, pViewByte, sectionAccessorIndex);

            //Return null here to reduce size of NewStruct in NativeAOT
            return null;
        }

        private IView? RegisterValue(int offset, int size, ViewKind kind, bool fromRegion)
        {
            var pViewByte = PEViewByteViewWriter.RegisterValueInternal(_fileAccessor, _fileAnalyzer, offset, size, kind, out var sectionAccessorIndex);

            SetPagedBody(offset, size, pViewByte, sectionAccessorIndex);

            return null;
        }

        private void SetPagedBody(int offset, int size, ViewByte* pViewByte, int sectionAccessorIndex)
        {
            var pageSize = _pageSize;

            var relativeOffset = offset & (pageSize - 1); //Faster modulo

            var dataEndOffset = relativeOffset + size;

            if (dataEndOffset < pageSize)
            {
                //The value fits within the current page

                /* Logically we want to execute
                 * 
                 * for (var i = pViewByte + 1; i < pViewByte + size; i++)
                 *     i->Kind = ViewByteKind.Body;
                 * 
                 * this is very hot, so we do this unrolled
                 */

                var ptr = (byte*) pViewByte + 1;
                var end = pViewByte + size;

                const byte mask = unchecked((byte) ~ViewByte.KindMask);
                const byte value = (byte) ViewByteKind.Body;

                //Unroll 8 bytes at a time
                while (ptr + 8 < end)
                {
                    ptr[0] = (byte) ((ptr[0] & mask) | value);
                    ptr[1] = (byte) ((ptr[1] & mask) | value);
                    ptr[2] = (byte) ((ptr[2] & mask) | value);
                    ptr[3] = (byte) ((ptr[3] & mask) | value);
                    ptr[4] = (byte) ((ptr[4] & mask) | value);
                    ptr[5] = (byte) ((ptr[5] & mask) | value);
                    ptr[6] = (byte) ((ptr[6] & mask) | value);
                    ptr[7] = (byte) ((ptr[7] & mask) | value);
                    ptr += 8;
                }

                //Execute the rest scalar
                while (ptr < end)
                {
                    *ptr = (byte) ((*ptr & mask) | value);
                    ptr++;
                }

#if DEBUG
                ref var sectionAccessor = ref _fileAccessor.SectionAccessors[sectionAccessorIndex];
                if (sectionAccessor.Bytes[sectionAccessor.Length - 2].Kind == ViewByteKind.Body)
                    Debug.Assert(sectionAccessor.Bytes[sectionAccessor.Length - 1].Kind == ViewByteKind.Body);

                var writtenLength = pViewByte->GetLength(sectionAccessor.pViewBytes + sectionAccessor.Length);
                Debug.Assert(writtenLength == size);
#endif
            }
            else
            {
                var currentSegmentStart = offset;
                var numBytesToWrite = 0;
                var remaining = size; //We already wrote the first byte, but our end is not inclusive

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
                        numBytesToWrite = Math.Min((pageSize - relativeOffset), remaining);
                    }
                    else
                    {
                        //We can write multiple page's worth in one go
                        numBytesToWrite = Math.Min(remaining, (sectionAccessor.EndAddress - currentSegmentStart));
                    }

                    /* We want to execute
                     * 
                     * for (var i = pViewByte + 1; i < pViewByte + numBytesToWrite; i++)
                     *     i->Kind = ViewByteKind.Body;
                     * 
                     * this is also very hot, so we do this unrolled
                     */

                    var ptr = (byte*) pViewByte + 1;
                    var end = pViewByte + numBytesToWrite;

                    const byte mask = unchecked((byte) ~ViewByte.KindMask);
                    const byte value = (byte) ViewByteKind.Body;

                    //Unroll 8 bytes at a time
                    while (ptr + 8 < end)
                    {
                        ptr[0] = (byte) ((ptr[0] & mask) | value);
                        ptr[1] = (byte) ((ptr[1] & mask) | value);
                        ptr[2] = (byte) ((ptr[2] & mask) | value);
                        ptr[3] = (byte) ((ptr[3] & mask) | value);
                        ptr[4] = (byte) ((ptr[4] & mask) | value);
                        ptr[5] = (byte) ((ptr[5] & mask) | value);
                        ptr[6] = (byte) ((ptr[6] & mask) | value);
                        ptr[7] = (byte) ((ptr[7] & mask) | value);
                        ptr += 8;
                    }

                    //Execute the rest scalar
                    while (ptr < end)
                    {
                        *ptr = (byte) ((*ptr & mask) | value);
                        ptr++;
                    }

#if DEBUG
                    if (sectionAccessor.Bytes[sectionAccessor.Length - 2].Kind == ViewByteKind.Body)
                        Debug.Assert(sectionAccessor.Bytes[sectionAccessor.Length - 1].Kind == ViewByteKind.Body);
#endif

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

                    //Note that there is not a 1:1 correspondence between page index and section accessor index,
                    //because we consolidate contiguous pages into one
                    pViewByte = _fileAccessor.GetViewByte(currentSegmentStart, out sectionAccessorIndex);
                    pViewByte->Kind = ViewByteKind.Body; //Need to set this to mark it as the head of a split
                    pViewByte->BodyKind = ViewByteBodyKind.SplitHead;

                    //Don't decrement remaining here, because we want to check against < end
                }
            }
        }
    }
}
