using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Threading;
using PESpy.PDB;
using PESpy.View.Builder;

namespace PESpy.View
{
    internal class PDBFileAccessor : FileAccessor
    {
        public PDBFile PDBFile { get; }

        public override IFile File => PDBFile;

        protected override ViewKind FileViewKind => ViewKind.PDBFile;

        internal readonly Dictionary<PN, int> _pageNumberToSIIndex;
        internal int[] _pagesToSectionAccessors;
        private ViewWriter _viewWriter;

        public PDBFileAccessor(PDBFile pdbFile) : base(GetBitness(pdbFile))
        {
            PDBFile = pdbFile;

            var numPages = PDBFile.NumPages;

            var pages = ArrayPool<DirectoryInfo>.Shared.Rent(numPages);
            var contiguousSections = new PooledList<PDBContiguousSectionInfo>();
            var sectionAccessors = new PooledList<SectionAccessor>();
            var nameBuilder = new ValueStringBuilder();

            var pagesToSectionAccessors = new int[numPages];

            _pageNumberToSIIndex = Merger.GetPageNumberToSIIndex(pdbFile);

            Length = PDBFile.Length;

            try
            {
                PDBViewWriter.GetContiguousSectionInfos(pdbFile, ref contiguousSections, pages);

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
                    ref PooledList<SectionAccessor> sectionAccessors,
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
                    nameBuilder.Append(section.NumPages);

                    sectionAccessors.Add(
                        new SectionAccessor(
                            section.GlobalStartIndex * pageSize,
                            (section.GlobalEndIndex + 1) * pageSize, //The way the NumPages works also shows you need to do +1
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

            throw new NotImplementedException("Don't know whether DbiHdr strictly indicates the EXE was 16-bit");
        }

        protected override object CreateOverview() => new PDBFileOverview(PDBFile, PDBFile.GetSymbolAccessor());

        public override bool TryGetTargetAddress(int rva, out int targetAddress, out int sectionIndex)
        {
            throw new NotImplementedException();
        }

        public override unsafe void GetRawSectionData(
            in SectionAccessor sectionAccessor,
            out byte* pByte,
            out int rva,
            out int remainingLength)
        {
            //Given each section accessor tells us where it is in the file, I feel like we can just return what we're being asked for

            pByte = PDBFile.globalBlock.LocalPointer;
            rva = sectionAccessor.StartAddress;
            remainingLength = sectionAccessor.Length;
        }

        internal override MemoryChunk GetMemoryChunkFromRVA(int rva)
        {
            throw new NotImplementedException();
        }

        internal override MemoryChunk GetMemoryChunkFromAddress(int address)
        {
            //We need to figure out which page the address belongs to, which stream _that_ belongs to,
            //and then slice the appropriate amount into the target stream

            var pageSize = PDBFile.PageSize;
            var pageNum = address / pageSize;

            if (pageNum == 0)
                return new MemoryChunk(PDBFile.globalBlock, address);

            var siIndex = _pageNumberToSIIndex[pageNum];

            PN[] pageList;
            int byteCount;

            switch (siIndex)
            {
                case Merger.SPECIAL_STREAM_MASTER_INDEX: //-1
                case Merger.SPECIAL_STREAM_FPM_0: //-2
                case Merger.SPECIAL_STREAM_FPM_1: //-3
                    throw new NotImplementedException();

                case Merger.SPECIAL_STREAM_STREAMTABLE: //-4
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

                case Merger.SPECIAL_STREAM_STREAMTABLE_LOCATION: //-5
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

            var offsetInPage = address & (pageSize - 1); //Faster modulo
            var chunk = PDBFile.globalBlock.SlicePaged(pageList, byteCount).Slice((currentIndex * pageSize) + offsetInPage);

            return chunk;
        }

        protected override ViewWriter GetViewWriter()
        {
            if (_viewWriter == null)
            {
                _viewWriter = new PDBViewWriter(PDBFile, this);

#if DEBUG
                _viewWriter.ShouldVerifyXRefs = false;
#endif
            }

            return _viewWriter;
        }

        protected override ViewWriter GetViewWriterForAddress(int targetOffset)
        {
            throw new NotImplementedException();
        }

        internal override bool TryGetDataSymbol(ulong address, int rva, out FixedUtf8String name, out int displacement)
        {
            throw new NotImplementedException();
        }

        public override bool TryGetVirtualAddress(in SectionAccessor sectionAccessor, int targetAddress, out int rva)
        {
            throw new NotImplementedException();
        }

        internal override ISectionDataAccessor CreateThreadLocalSectionDataAccessor()
        {
            throw new NotImplementedException();
        }

        //We are our own symbol accessor, so we don't need to be afraid to return ourselves right away even if we don't want to allow loading.
        //The PDBFile stores the actual reference to the symbol accessor
        internal override ISymbolAccessor GetSymbolAccessor(
            bool load,
            LocatorHttpPolicy httpPolicy,
            ILocatorProgress? progress,
            CancellationToken cancellationToken = default) => PDBFile.GetSymbolAccessor(httpPolicy, progress, cancellationToken);

        internal unsafe void GetSplitHeadOrigin(ref ViewByte* pViewByte, ref int offset, out int sectionIndex, out int bytesRewound)
        {
            bytesRewound = 0;

            while (true)
            {
                Debug.Assert(pViewByte->Kind == ViewByteKind.Body && pViewByte->BodyKind == ViewByteBodyKind.SplitHead);

                offset = Merger.GetNextPageOffset(PDBFile, offset, _pageNumberToSIIndex, previous: true) + PDBFile.PageSize - 1;

                pViewByte = GetViewByte(offset, out sectionIndex);

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
    }
}
