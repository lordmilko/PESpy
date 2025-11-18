using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using PESpy.PDB;
using PESpy.View.Builder;

namespace PESpy.View
{
    internal class PDBFileAccessor : FileAccessor
    {
        public PDBFile PDBFile { get; }

        public override IFile File => PDBFile;

        internal readonly Dictionary<PN, int> _pageNumberToSIIndex;
        private ViewWriter _viewWriter;

        public PDBFileAccessor(PDBFile pdbFile) : base(GetBitness(pdbFile))
        {
            PDBFile = pdbFile;

            var pages = new PooledList<DirectoryInfo>();
            var contiguousSections = new PooledList<PDBContiguousSectionInfo>();
            var sectionAccessors = new PooledList<SectionAccessor>();

            _pageNumberToSIIndex = Merger.GetPageNumberToSIIndex(pdbFile);

            Length = PDBFile.Length;

            try
            {
                PDBViewWriter.GetContiguousSectionInfos(pdbFile, ref contiguousSections, ref pages);

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
                    ref PooledList<DirectoryInfo> pages,
                    ref PooledList<SectionAccessor> sectionAccessors,
                    int currentPageIndex,
                    int endIndex)
                {
                    for (var j = currentPageIndex; j < endIndex; j++)
                    {
                        var page = pages[j];

                        sectionAccessors.Add(
                            new SectionAccessor(
                                page.Start,
                                page.End,
                                SectionAccessorKind.Page, j,
                                "[Page] " + pages[j].Name,
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

                        AddPages(ref pages, ref sectionAccessors, currentPageIndex, section.GlobalStartIndex);
                    }

                    //Now write the current contiguous section

                    sectionAccessors.Add(
                        new SectionAccessor(
                            section.GlobalStartIndex * pageSize,
                            (section.GlobalEndIndex + 1) * pageSize, //The way the NumPages works also shows you need to do +1
                            SectionAccessorKind.Section,
                            -1,
                            $"[Range] {section.GlobalStartIndex}-{section.GlobalEndIndex} | {section.Name} (Pages {section.LocalStartIndex + 1}-{section.LocalEndIndex + 1} of {section.NumPages})",
                            MemoryMappedFile.CreateNew(null, section.NumPages * pageSize)
                        )
                    );

                    currentPageIndex = section.GlobalEndIndex + 1;
                }

                if (currentPageIndex < pages.Count )
                {
                    AddPages(ref pages, ref sectionAccessors, currentPageIndex, pages.Count);
                }

                SectionAccessors = sectionAccessors.ToArray();
            }
            finally
            {
                pages.Dispose();
                contiguousSections.Dispose();
                sectionAccessors.Dispose();
            }
        }

        private static int GetBitness(PDBFile pdbFile)
        {
            var dbi = pdbFile.DBI;

            if (dbi == null)
                return 32; //We're in trouble! There can't really be much of anything in this file, so just assume 32-bit

            if (dbi.DbiHdr is NewDBIHdr n)
                return GetBitness(n.wMachine);
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
        protected override ViewWriter GetViewWriter()
        {
            if (_viewWriter == null)
            {
                _viewWriter = new PDBViewWriter(PDBFile);

#if DEBUG
                _viewWriter.ShouldVerifyXRefs = false;
#endif
            }

            return _viewWriter;
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

        internal override ISymbolAccessor GetSymbolAccessor(ILocatorProgress? progress = null)
        {
            throw new NotImplementedException();
        }

        internal unsafe void GetSplitHeadOrigin(ref ViewByte* pViewByte, ref int offset)
        {
            while (true)
            {
                Debug.Assert(pViewByte->Kind == ViewByteKind.Body && pViewByte->BodyKind == ViewByteBodyKind.SplitHead);

                offset = Merger.GetNextPageOffset(PDBFile, offset, _pageNumberToSIIndex, previous: true) + PDBFile.PageSize - 1;

                pViewByte = GetViewByte(offset, out _);

                Debug.Assert(pViewByte->Kind == ViewByteKind.Body && pViewByte->BodyKind == ViewByteBodyKind.SplitTail);

                while (true)
                {
                    if (pViewByte->Kind != ViewByteKind.Body)
                        return;

                    if (pViewByte->BodyKind == ViewByteBodyKind.SplitHead)
                    pViewByte--;
                    offset--;
                }
            }
        }
    }
}
