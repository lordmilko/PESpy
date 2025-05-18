using System;
using System.Buffers;
using System.Collections;
using System.Diagnostics;

namespace PESpy.PDB
{
    internal class FPMBuilder
    {
        public BitArray PageMap { get; private set; }

        private PDBFileBuilder pdbFileBuilder;

        public FPMBuilder(int length, PDBFileBuilder pdbFileBuilder)
        {
            this.pdbFileBuilder = pdbFileBuilder;

            PageMap = new BitArray(length);
        }

        public FPMBuilder(in FPM fpm, PDBFileBuilder pdbFileBuilder)
        {
            throw new NotImplementedException();
        }

        public void SetAll() => PageMap.SetAll(true);

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

                    PageMap[nextFreePage] = false; //Reserve FPM 0's page
                    PageMap[nextFreePage + 1] = false; //Reserve FPM 1's page
                    nextFreePage++;
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

        private bool IsFpmPage(PN pn)
        {
            /* Pages 0, 1 and 2 in a big MSF file contain the master index, FPM 0 and FPM 1.
             * When the pages for those are allocated in the stream table, we assert that we're
             * getting the pages that we expect. As such, we need to _not_ suppress the fact
             * that we're allocating an FPM page here. However, for any subsequent FPM pages,
             * we need to silently allocate the page and then allocate another page for the caller */
            if (pn < 3)
                return false;

            var remainder = pn & (pdbFileBuilder.PageSize - 1); //pn % pageSize

            if (remainder == 1 || remainder == 2)
                return true; //e.g. it's page 1025 or 1026, which are FPM pages when you have 1024 byte pages

            return false;
        }

        internal void Add(BitArray other)
        {
            PageMap.Or(other);
        }

        internal void CopyFrom(in FPM other)
        {
            PageMap = new BitArray(other.PageMap);
        }

        internal void Serialize()
        {
            var pageSize = pdbFileBuilder.PageSize;

            //During MSF_HB::serializeFpm it calculates how many bits will exist in the FPM, based on the number of pages
            //that the FPM will span, multiplied by the page size. It then expands the FPM and sets all newly added bits to -1

            //Each page can be represented in a single bit, so how many bytes would actually be needed to store all those bits
            var numBytesNeededToStoreFpm = SI.DivideUp(pdbFileBuilder.NumPages, 8);

            //How many pages would that be
            var numPagesNeededToStoreFpm = SI.DivideUp(numBytesNeededToStoreFpm, pageSize);

            var numBytesActualToStoreFpm = numPagesNeededToStoreFpm * pageSize;

            var numBitsActualToStoreFpm = numBytesActualToStoreFpm * 8;

            /* Note that the actual number of pages will comprise the FPM may be much more than the number of pages actually
             * _needed_ to store the FPM, due to the fact that we emulate a bug in mspdbcore.dll wherein we assume only
             * pageSize bits can fit per page, instead of 8*pageSize. However, this buggy logic only applies when it comes
             * to _reserving_ pages. When it comes to serializing, we don't need to worry about that */

            if (PageMap.Length < numBitsActualToStoreFpm)
            {
                var oldLength = PageMap.Length;
                PageMap.Length = numBitsActualToStoreFpm;

                //Observe that we only mark bits as free that are in pages that are actually needed to store the number of pages we have in the FPM. Overallocated FPM pages
                //remain as 0
                //todo: wont this cause issues? how does microsoft-pdb deserialize the fpm?
                for (var i = oldLength; i < numBitsActualToStoreFpm; i++)
                {
                    PageMap[i] = true;
                }
            }

            var fpmPages = new PN[numPagesNeededToStoreFpm];
            var currentPage = pdbFileBuilder.ActiveFpmPage;

            for (var i = 0; i < numPagesNeededToStoreFpm; i++)
            {
                fpmPages[i] = currentPage;
                currentPage += pageSize;
            }

            var size = numBytesActualToStoreFpm;

            var rentedArray = ArrayPool<byte>.Shared.Rent(size);

            try
            {
                PageMap.CopyTo(rentedArray, 0);

                var chunk = pdbFileBuilder.SlicePaged(fpmPages, size);

                Span<byte> span = new Span<byte>(rentedArray, 0, size);
                chunk.PokeSpan(0, size, span);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rentedArray);
            }
        }

        public void FreePage(PN pn)
        {
            PageMap[pn] = true;
        }
    }
}
