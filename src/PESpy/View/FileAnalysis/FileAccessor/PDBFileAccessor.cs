using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using PESpy.PDB;

namespace PESpy.View
{
    internal class PDBFileAccessor : FileAccessor
    {
        //Not streams, but we need their indices to be in the page number to SI index lookup map for file analysis
        //lookups (we don't need them for merging)
        internal const int SPECIAL_STREAM_MASTER_INDEX = -1;
        internal const int SPECIAL_STREAM_FPM_0 = -2;
        internal const int SPECIAL_STREAM_FPM_1 = -3;

        internal const int SPECIAL_STREAM_STREAMTABLE = -4;
        internal const int SPECIAL_STREAM_STREAMTABLE_LOCATION = -5;
        internal const int SPECIAL_STREAM_FREE = -6;

        public PDBFile PDBFile { get; }

        internal readonly Dictionary<PN, int> _pageNumberToSIIndex;
        internal int[] _pagesToSectionAccessors;

        public PDBFileAccessor(PDBFile pdbFile) : base(pdbFile, GetBitness(pdbFile))
        {
            PDBFile = pdbFile;
            FileViewKind = ViewKind.PDBFile;

            _pageNumberToSIIndex = GetPageNumberToSIIndex(pdbFile);
        }

        private static Dictionary<PN, int> GetPageNumberToSIIndex(PDBFile pdbFile)
        {
            var dict = new Dictionary<PN, int>();

            dict[0] = SPECIAL_STREAM_MASTER_INDEX;

            foreach (var page in pdbFile.FPM0.FpmPages)
                dict[page] = SPECIAL_STREAM_FPM_0;

            foreach (var page in pdbFile.FPM1.FpmPages)
                dict[page] = SPECIAL_STREAM_FPM_1;

            var streamInfos = pdbFile.StreamTable.StreamInfos;

            for (var i = 0; i < streamInfos.Length; i++)
            {
                var si = streamInfos[i];

                foreach (var pn in si.PageList)
                    dict[pn] = i;
            }

            if (pdbFile is PDB7File v7)
            {
                //For the pages that describe the location of the stream table, we list these as being at index -1, which we special case
                //to know that we need to retrieve the StreamTableLocation.PageList
                foreach (var page in v7.StreamTableLocation.PageList)
                    dict[page] = SPECIAL_STREAM_STREAMTABLE;

                //And for the pages that describe the location of the stream table's pages, we list these as being at -2
                foreach (var page in v7.MsfHeader.PagesOfStreamTablePageList)
                    dict[page] = SPECIAL_STREAM_STREAMTABLE_LOCATION;
            }
            else
            {
                //In V2, mpspnpnSt lists the pages of the stream table directly
                foreach (var page in ((PDB2File) pdbFile).MsfHeader.StreamTablePageList)
                    dict[page] = SPECIAL_STREAM_STREAMTABLE;
            }

            return dict;
        }

        protected override void InitializeSectionAccessors(ViewMode viewMode)
        {
            var pdbFile = (PDBFile) File;

            var numPages = pdbFile.NumPages;

            var pages = ArrayPool<DirectoryInfo>.Shared.Rent(numPages);
            var contiguousSections = new ValueList<PDBContiguousSectionInfo>();
            var sectionAccessors = new ValueList<SectionAccessor>();
            var nameBuilder = new ValueStringBuilder();

            var pagesToSectionAccessors = new int[numPages];

            Length = pdbFile.Length;

            try
            {
                PDBFileViewWriterHelper.GetContiguousSectionInfos(pdbFile, ref contiguousSections, pages);

                /* Pages contains a list of every single page and the name of that page. We now need
                 * to convert this list of pages into a collection of segments. MergePDB works by first
                 * constructing a list of views for all of the pages (which are represented by DirectoryInfo
                 * objects) and then grouping them up based on the bounds of each contiguous section.
                 * 
                 * We basically want to do the same thing here, except we don't need to convert our page list
                 * to views, we can just iterate over the page list manully */

                var currentPageIndex = 0;

                var pageSize = pdbFile.PageSize;

                void AddPages(
                    DirectoryInfo[] pages,
                    ref ValueList<SectionAccessor> sectionAccessors,
                    ref ValueStringBuilder nameBuilder,
                    int currentPageIndex,
                    int endIndex)
                {
                    for (var j = currentPageIndex; j < endIndex; j++)
                    {
                        var page = pages[j];
                        pagesToSectionAccessors[j] = sectionAccessors.Count;

                        nameBuilder.Clear();
                        nameBuilder.Append("[Page] ");
                        pages[j].NameInfo.ToString(ref nameBuilder, true);

                        sectionAccessors.Add(
                            new SectionAccessor(
                                page.Start,
                                page.End,
                                SectionAccessorKind.Page, j,
                                nameBuilder.ToString(),
                                MemoryMappedFile.CreateNew(null, pageSize)
                            )
                        );
                    }
                }

                for (var i = 0; i < contiguousSections.Count; i++)
                {
                    var section = contiguousSections[i];

                    if (currentPageIndex < section.GlobalStartIndex)
                    {
                        //Write all pages up to the start of this page

                        AddPages(pages, ref sectionAccessors, ref nameBuilder, currentPageIndex, section.GlobalStartIndex);
                    }

                    //Now write the current contiguous section

                    for (var j = section.GlobalStartIndex; j <= section.GlobalEndIndex; j++)
                    {
                        pagesToSectionAccessors[j] = sectionAccessors.Count;
                    }

                    nameBuilder.Clear();
                    nameBuilder.Append("[Range] ");
                    nameBuilder.Append(section.GlobalStartIndex);
                    nameBuilder.Append('-');
                    nameBuilder.Append(section.GlobalEndIndex);
                    nameBuilder.Append(" | ");
                    section.NameInfo.ToString(ref nameBuilder, false);
                    nameBuilder.Append(" (Pages");
                    nameBuilder.Append(section.LocalStartIndex + 1);
                    nameBuilder.Append('-');
                    nameBuilder.Append(section.LocalEndIndex + 1);
                    nameBuilder.Append(" of ");
                    nameBuilder.Append(section.TotalPagesInStream);

                    sectionAccessors.Add(
                        new SectionAccessor(
                            (uint) section.GlobalStartIndex * pageSize,
                            (uint) (section.GlobalEndIndex + 1) * pageSize, //The way the NumPages works also shows you need to do +1
                            SectionAccessorKind.Section,
                            -1,
                            nameBuilder.ToString(),
                            MemoryMappedFile.CreateNew(null, section.NumPages * pageSize)
                        )
                    );

                    currentPageIndex = section.GlobalEndIndex + 1;
                }

                if (currentPageIndex < numPages)
                {
                    AddPages(pages, ref sectionAccessors, ref nameBuilder, currentPageIndex, numPages);
                }

                _pagesToSectionAccessors = pagesToSectionAccessors;
                SectionAccessors = sectionAccessors.ToArray();
            }
            finally
            {
                ArrayPool<DirectoryInfo>.Shared.Return(pages);
                contiguousSections.Dispose();
                sectionAccessors.Dispose();
                nameBuilder.Dispose();
            }
        }

        private static int GetBitness(PDBFile pdbFile)
        {
            var dbi = pdbFile.DBI;

            if (dbi == null)
                return 32; //We're in trouble! There can't really be much of anything in this file, so just assume 32-bit

            if (dbi.DbiHdr is NewDBIHdr n)
                return GetBitness(n.wMachine);

            //MSF2 was 16-bit but you only have MSF when you've got at least NB10 which is VC2 which is 32-bit
            return 32;
        }

        protected override object CreateOverview() => new PDBFileOverview(PDBFile, PDBFile.GetSymbolAccessor());

        public override unsafe void GetRawSectionData(
            in SectionAccessor sectionAccessor,
            out byte* pByte,
            out int rva,
            out int remainingLength)
        {
            //Given each section accessor tells us where it is in the file, I feel like we can just return what we're being asked for

            pByte = PDBFile.globalBlock.LocalPointer;
            rva = default;
            remainingLength = sectionAccessor.Length;
        }

        internal override void GetMemoryChunkFromAddress(long address, out MemoryChunk chunk, out ViewWriter viewWriter)
        {
            //We need to figure out which page the address belongs to, which stream _that_ belongs to,
            //and then slice the appropriate amount into the target stream

            var pageSize = PDBFile.PageSize;
            var pageNum = (uint) (address / pageSize);

            if (pageNum == 0)
            {
                chunk = new MemoryChunk(PDBFile.globalBlock, address);
                viewWriter = GetViewWriter();
                return;
            }

            var siIndex = _pageNumberToSIIndex[pageNum];

            PN[] pageList;
            int byteCount;

            switch (siIndex)
            {
                case SPECIAL_STREAM_MASTER_INDEX: //-1
                case SPECIAL_STREAM_FPM_0: //-2
                case SPECIAL_STREAM_FPM_1: //-3
                    throw new NotImplementedException();

                case SPECIAL_STREAM_STREAMTABLE: //-4
                    if (PDBFile is PDB7File v7)
                    {
                        pageList = v7.StreamTableLocation.PageList;
                        byteCount = v7.StreamTableLocation.ByteCount;
                    }
                    else
                    {
                        var v2 = (PDB2File) PDBFile;
                        var msfHeader = v2.MsfHeader;
                        pageList = v2.StreamTablePageListArray;
                        byteCount = msfHeader.StreamTableSizeInfo.ByteCount;
                    }
                    break;

                case SPECIAL_STREAM_STREAMTABLE_LOCATION: //-5
                    pageList = ((PDB7File) PDBFile)._pagesOfStreamTablePageListArray;
                    byteCount = pageList.Length * PDBFile.PageSize;
                    break;

                default:
                    var si = PDBFile.StreamTable.StreamInfos[siIndex];
                    pageList = si.PageList;
                    byteCount = si.ByteCount;
                    break;
            }

            var currentIndex = Array.IndexOf(pageList, (PN) pageNum);
            Debug.Assert(currentIndex != -1);

            var offsetInPage = (int) (address & (pageSize - 1)); //Faster modulo
            chunk = PDBFile.globalBlock.SlicePaged(pageList, byteCount).Slice((currentIndex * pageSize) + offsetInPage);
            viewWriter = GetViewWriter();
        }

        protected override ViewWriter GetViewWriter()
        {
            if (_viewWriter == null)
            {
                _viewWriter = new ViewWriter(
                    new PDBFileViewWriterHelper(PDBFile),
                    PDBFile.CreateByteViewProvider(this),
                    fileAccessor: this
                );

#if DEBUG
                _viewWriter.ShouldVerifyXRefs = false;
#endif
            }

            return _viewWriter;
        }

        internal unsafe void GetSplitHeadOrigin(ref ViewByte* pViewByte, ref long offset, out int sectionIndex, out int bytesRewound)
        {
            bytesRewound = 0;

            while (true)
            {
                Debug.Assert(pViewByte->Kind == ViewByteKind.Body && pViewByte->BodyKind == ViewByteBodyKind.SplitHead);

                offset = GetNextPageOffset(PDBFile, offset, _pageNumberToSIIndex, previous: true) + PDBFile.PageSize - 1;

                pViewByte = GetViewByte(offset, out sectionIndex);

                //If we've got a great big contiguous section, then we're not going to have a SplitTail between each page.
                //However, in that case we shouldn't have had a SplitHead in the first place
                Debug.Assert(pViewByte->Kind == ViewByteKind.Body && pViewByte->BodyKind == ViewByteBodyKind.SplitTail);

                while (true)
                {
                    if (pViewByte->Kind != ViewByteKind.Body)
                    {
                        //I was 1 off the length when we encountered the head so I think I need to do +1 here. Not sure what would happen
                        //if we have multiple segments to rewind through
                        bytesRewound++;
                        return;
                    }

                    if (pViewByte->BodyKind == ViewByteBodyKind.SplitHead)
                    {
                        break; //It's the start of another body chunk; repeat the outer loop to continue trying to find the head
                    }

                    pViewByte--;
                    offset--;
                    bytesRewound++; //I wanted to try and optimize this and just subtract from offset when we return, but if we span multiple pages we can't just do offset - bytesRewound
                }
            }

            throw new InvalidOperationException("Failed to get the origin of a split head");
        }

        internal unsafe int GetFullLength(ViewByte* pViewByte, long offset)
        {
            var pageSize = PDBFile.PageSize;

            var totalLength = pageSize - (int) (offset % pageSize);

            while (true)
            {
                offset = GetNextPageOffset(PDBFile, offset, _pageNumberToSIIndex);

                pViewByte = GetViewByte(offset, out var sectionIndex);

                ref var sectionAccessor = ref SectionAccessors[sectionIndex];

                //If the first page is actually part of a SectionAccessor with multiple contiguous pages in it, that's going
                //to cause an issue, because we need to ignore the initial length before the start of the entity. This is only
                //a potential issue on the first section; after that, we should be processing an entire section at a time from
                //the start
                var distanceInSection = (int) (pViewByte - sectionAccessor.pViewBytes);

                pViewByte++; //Skip over the head

                var end = sectionAccessor.pViewBytesEnd;

                while (true)
                {
                    if (pViewByte >= end)
                    {
                        //We reached the end of a page without having reached a SplitTail; we're done
                        totalLength += (int) (pViewByte - sectionAccessor.pViewBytes) - distanceInSection;
                        return totalLength;
                    }

                    if (pViewByte->Kind != ViewByteKind.Body)
                    {
                        totalLength += (int) (pViewByte - sectionAccessor.pViewBytes) - distanceInSection;
                        return totalLength;
                    }

                    if (pViewByte->BodyKind != ViewByteBodyKind.SplitTail)
                    {
                        pViewByte++;
                        continue;
                    }

                    var sectionAccessorUsed = sectionAccessor.Length - distanceInSection;

                    totalLength += sectionAccessorUsed;
                    offset += sectionAccessorUsed - 1;
                    distanceInSection = 0;
                    break;
                }
            }
        }

        internal long GetNextPageOffset(long offset) => GetNextPageOffset(PDBFile, offset, _pageNumberToSIIndex);

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
    }
}
