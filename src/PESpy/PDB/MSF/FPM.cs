using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents a Free Page Map which tracks which pages are available for use within the PDB.
    /// </summary>
    public struct FPM
    {
        /// <summary>
        /// Gets the pages that the FPM spans. This value is determined based on the number
        /// of pages in the file PDB, which determines the number of FPM bits that would be required
        /// to list those pages.
        /// </summary>
        public List<PN> FpmPages { get; }

        /// <summary>
        /// Gets the actual bits that encompass the Free Page Map. If a given bit is 1, that bit's page is free.
        /// Otherwise, that bit's page is already in use.
        /// </summary>
        public BitArray PageMap { get; private set; }

        private int pageSize;

        internal FPM(PN fpmPageNo, int pageSize, int numPages, PDBGlobalMemoryBlock globalBlock, bool isBig)
        {
            this.pageSize = pageSize;
            nextFreePage = default;

            /* The FPM uses bits to indicate whether a given page is free or not. If there are 1024 bytes per page, that means
             * that a singular FPM page can track the state of the first 8192 pages in the file. Given that FPM0 is on page 1 and FPM1
             * is on page 2, you can calculate whether a given page should be reserved for use by the FPM using the following expression
             *
             *     pageNumber % (pageSize * 8)
             *
             * e.g. if we have 1024 byte pages:
             *
             * Page 1:       1 % 8192 = 1 (FPM Page)
             * Page 3:       3 % 8192 = 3 (Non-FPM Page)
             * Page 8193: 8193 % 8192 = 1 (FPM Page)
             *
             * However, microsoft-pdb has a bug in its formula. Instead of doing pageNuber % (pageSize * 8), they do pageNumber % pageSize
             *
             * Which means we end up with the following
             *
             * Page 1:       1 % 1024 = 1 (FPM Page)
             * Page 1025: 1025 % 1024 = 1 (FPM Page)
             * Page 2049: 2049 % 1024 = 1 (FPM Page)
             * ...
             * Page 8193: 8193 % 8192 = 1 (FPM Page)
             *
             * We end up reserving 8x too many pages for use by the FPM! This bug is propagated everywhere that deals with writing data to the FPM.
             * serializeFpm() correctly identifies the correct _number_ of pages that the FPM needs to span, but still has to utilize the buggy logic
             * of writing the first set of data into Page 1, and the second into Page 1025, etc */

            if (isBig)
            {
                //In Big MSFs, there are multiple FPM pages scattered at regular intervals

                //This _should_ be pageSize * 8, but to emulate the bug, we don't do the * 8
                //var bitsPerPage = pageSize * 8;
                var bitsPerPage = pageSize;

                var numFpmPages = SI.DivideUp(numPages, bitsPerPage);

                var fpmPages = new List<PN>(numFpmPages);
                var currentFpmPage = fpmPageNo;

                for (var i = 0; i < numFpmPages; i++)
                {
                    fpmPages.Add(currentFpmPage);

                    //If there's another FPM page, if we have 1024 byte pages, it's 1024 pages away from the last one
                    currentFpmPage += pageSize;
                }

                FpmPages = fpmPages;
            }
            else
            {
                //In small MSFs a great big FPM is allocated up front capable of storing all 65536 page bits. The number of pages
                //required to represent the FPM will depend on how big each page is (e.g. if we have 1024 byte pages, we can represent
                //8192 bits per page which means we need 8 pages to represent all 65536 page bits)

                var numFpmPages = 65536 / (pageSize * 8);

                var fpmPages = new List<PN>(numFpmPages);

                for (var i = 0; i < numFpmPages; i++)
                    fpmPages.Add(fpmPageNo + i);

                FpmPages = fpmPages;
            }

            //Now read the actual bits of the FPM
            var fpmNumBytes = SI.DivideUp(numPages, 8); //How many bytes does it take to represent all of the pages? e.g. if there's 25 pages, read 4 bytes (32-bits)
            var fpmReader = globalBlock.SlicePaged(FpmPages, fpmNumBytes, copyOld: false);
            //var fpmReader = new MemoryChunk(new PdbPageStream(reader.GetStreamUnsafe(), fpmPages, fpmNumBytes, pageSize), false);

            /* We're going to read, say, 4 byte's worth, but might only be interested in the first 25 bits. If you look at the hex in the FPM,
             * every bit after the first 25 (for the 25 pages we might have) will be all 1 (FF) indicating that all of these other "pages"
             * (that don't actually exist yet) are "free" */
            PageMap = new BitArray(fpmReader.PeekSpan<byte>(0, fpmNumBytes).ToArray());
        }

        public readonly void SetAll() => PageMap.SetAll(true);

        private int nextFreePage;

        //FPM::nextPn
        public PN AllocPage()
        {
            /* Get the next free page in the map. A given page may have 3 potential purposes
             * 1. It may be a special page that is reserved for the master index / initial FPMs
             * 2. It may be a non-contiguous FPM page scattered throughout the file
             * 3. It may be a regular page that the user is free to use
             *
             * In the case of #2, we internally allocate this page and don't report it to the user. In the cases of #1 and #3
             * however, we _do_ report them to the user, meaning that in the case of the initial FPMs at the start of the file,
             * they will be reported. */

            const int maxFpmPages = 0x100000;

            for (; nextFreePage < maxFpmPages; nextFreePage++)
            {
                Debug.Assert(nextFreePage < PageMap.Length, "It should not be possible to run out of free pages in the page map. The PDB should have expanded the page map by the growth factor prior to calling this method. The PageMap contains bits, and since our starting page count is 3 and we have 8 bits in the page map to start with, when we hit the 4th bit the maximum page number will have been expanded");

                if (IsFpmPage(nextFreePage))
                {
                    //This is one of the non-contiguous pages scattered throughout the big PDB.

                    //Only add the page if it specifically belongs to _this_ FPM
                    if ((nextFreePage & (pageSize - 1)) == FpmPages[0])
                        FpmPages.Add(nextFreePage);

                    PageMap[nextFreePage] = false;
                    continue;
                }

                if (PageMap[nextFreePage])
                    break;
            }

            if (nextFreePage >= maxFpmPages)
                throw new InvalidOperationException("Out of pages");

            Debug.Assert(nextFreePage < PageMap.Length, "It should not be possible to run out of free pages in the page map. The PDB should have expanded the page map by the growth factor prior to calling this method. The PageMap contains bits, and since our starting page count is 3 and we have 8 bits in the page map to start with, when we hit the 4th bit the maximum page number will have been expanded");

            //We may need to grow the size of the bit array
            PageMap[nextFreePage] = false;
            return nextFreePage;
        }

        private readonly bool IsFpmPage(PN pn)
        {
            /* Pages 0, 1 and 2 in a big MSF file contain the master index, FPM 0 and FPM 1.
             * When the pages for those are allocated in the stream table, we assert that we're
             * getting the pages that we expect. As such, we need to _not_ suppress the fact
             * that we're allocating an FPM page here. However, for any subsequent FPM pages,
             * we need to silently allocate the page and then allocate another page for the caller */
            if (pn < 3)
                return false;

            var remainder = pn & (pageSize - 1); //pn % pageSize

            if (remainder == 1 || remainder == 2)
                return true; //e.g. it's page 1025 or 1026, which are FPM pages when you have 1024 byte pages

            return false;
        }

        internal readonly void Add(BitArray other)
        {
            PageMap.Or(other);
        }

        internal void CopyFrom(in FPM other)
        {
            PageMap = new BitArray(other.PageMap);
        }

        internal readonly void Serialize(PDBGlobalMemoryBlock globalBlock)
        {
            //During MSF_HB::serializeFpm it calculates how many bits will exist in the FPM, based on the number of pages
            //that the FPM will span, multiplied by the page size. It then expands the FPM and sets all newly added bits to -1
            var numFpmBytesUsed = SI.DivideUp(globalBlock.PDBFile.NumPages, 8);
            var numPhysicalFpmPages = SI.DivideUp(numFpmBytesUsed, pageSize);
            var numFpmBitsUsed = numFpmBytesUsed * 8;
            var numPhysicalFpmBytesNeeded = numPhysicalFpmPages * pageSize;
            var numPhysicalFpmBitsNeeded = numPhysicalFpmBytesNeeded * 8;

            var numPhysicalBytesActual = FpmPages.Count * pageSize;

            if (PageMap.Length < numPhysicalFpmBitsNeeded)
            {
                var oldLength = PageMap.Length;
                PageMap.Length = numPhysicalFpmBitsNeeded;

                //Observe that we only mark bits as free that are in pages that are actually needed to store the number of pages we have in the FPM. Overallocated FPM pages
                //remain as 0
                //todo: wont this cause issues? how does microsoft-pdb deserialize the fpm?
                for (var i = oldLength; i < numPhysicalFpmBitsNeeded; i++)
                {
                    PageMap[i] = true;
                }
            }
            var size = numPhysicalBytesActual;

            var rentedArray = ArrayPool<byte>.Shared.Rent(size);

            try
            {
                PageMap.CopyTo(rentedArray, 0);

                var chunk = globalBlock.SlicePaged(FpmPages, size, copyOld: false); //No need to copy, we're going to replace the whole thing

                Span<byte> span = new Span<byte>(rentedArray, 0, size);
                chunk.PokeSpan(0, size, span);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rentedArray);
            }
        }

        internal void EnsureEmptyPages(PDBFile pdbFile)
        {
            //When we're editing PDBs, its all being done in memory. All of the action happens in the active FPM, however this can create a bit
            //of a problem: if we've needed to allocate an additional FPM page, the in-memory area where that page would live is currently filled
            //with garbage. As such, if the non-active FPM needs to grow into additional pages, we need to expand the memory buffer that covers
            //the non-active FPM

            //This _should_ be pageSize * 8, but to emulate the bug, we don't do the * 8
            //var bitsPerPage = pageSize * 8;
            var bitsPerPage = pageSize;
            var numFpmPages = SI.DivideUp(pdbFile.NumPages, bitsPerPage);

            if (numFpmPages == FpmPages.Count)
                return;

            var currentPage = FpmPages[FpmPages.Count - 1];

            for (var i = FpmPages.Count; i < numFpmPages; i++)
            {
                currentPage += pageSize;
                FpmPages.Add(currentPage);
            }

            pdbFile.globalBlock.SlicePaged(FpmPages, FpmPages.Count * pageSize, true);
        }

        public readonly void FreePage(PN pn)
        {
            PageMap[pn] = true;
        }
    }
}
