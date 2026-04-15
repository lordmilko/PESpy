using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents a Free Page Map which tracks which pages are available for use within the PDB.
    /// </summary>
    public struct FPM //While it's defined in msf.cpp, the struct is purely in memory
    {
        /// <summary>
        /// Gets the pages that the FPM spans. This value is determined based on the number
        /// of pages in the file PDB, which determines the number of FPM bits that would be required
        /// to list those pages.
        /// </summary>
        public PN[] FpmPages { get; }

        /// <summary>
        /// Gets the actual bits that encompass the Free Page Map. If a given bit is 1, that bit's page is free.
        /// Otherwise, that bit's page is already in use.
        /// </summary>
        public BitArray PageMap { get; private set; }

        //Big MSF ctor
        internal FPM(PN fpmPageNo, int pageSize, int numPages, PDBGlobalMemoryBlock globalBlock)
        {
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

            //In Big MSFs, there are multiple FPM pages scattered at regular intervals

            //This _should_ be pageSize * 8, but to emulate the bug, we don't do the * 8
            //var bitsPerPage = pageSize * 8;
            var bitsPerPage = pageSize;

            var numFpmPages = SI.DivideUp(numPages, bitsPerPage);

            var fpmPages = new PN[numFpmPages];
            var currentFpmPage = fpmPageNo;

            for (var i = 0; i < numFpmPages; i++)
            {
                fpmPages[i] = currentFpmPage;

                //If there's another FPM page, if we have 1024 byte pages, it's 1024 pages away from the last one
                currentFpmPage += pageSize;
            }

            FpmPages = fpmPages;

            //Now read the actual bits of the FPM
            var fpmNumBytes = SI.DivideUp(numPages, 8); //How many bytes does it take to represent all of the pages? e.g. if there's 25 pages, read 4 bytes (32-bits)
            var fpmReader = globalBlock.SlicePaged(FpmPages, fpmNumBytes);

            /* We're going to read, say, 4 byte's worth, but might only be interested in the first 25 bits. If you look at the hex in the FPM,
             * every bit after the first 25 (for the 25 pages we might have) will be all 1 (FF) indicating that all of these other "pages"
             * (that don't actually exist yet) are "free" */
            PageMap = new BitArray(fpmReader.PeekNativeSpan<byte>(0, fpmNumBytes).ToArray());
        }

        //Small MSF ctor
        internal FPM(PN fpmPageNo, int pageSize, int numPages, PDBGlobalMemoryBlock globalBlock, in MSFParms msfParms)
        {
            /* In small MSFs, a great big FPM is allocated up front capable of storing all possible page bits. While in theory
             * a single page is capable of representing 65536 bits, in practice only the 1024 and 2048 byte page sizes use all
             * 65536 bits. The 4096 byte version caps the number of bits to 32767
             *
             * And so, the number of pages required to represent the FPM will depend on both how big each page is, and the maximum
             * number of bits that are allowed to exist in a given page */

            var numFpmPages = msfParms.NumPagesPerFPM;

            var fpmPages = new PN[numFpmPages];

            for (var i = 0; i < numFpmPages; i++)
                fpmPages[i] = fpmPageNo + i;

            FpmPages = fpmPages;

            var fpmNumBytes = SI.DivideUp(numPages, 8);
            var fpmReader = globalBlock.SlicePaged(FpmPages, fpmNumBytes);

            PageMap = new BitArray(fpmReader.PeekNativeSpan<byte>(0, fpmNumBytes).ToArray());
        }
    }
}
