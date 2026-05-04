using System;
using PESpy.PDB;

namespace PESpy
{
    struct MSFParms //While it's defined in msf.cpp, the struct is purely in memory
    {
        private static readonly MSFParms[] smallParams =
        {
            new MSFParms(pageSize: 1024, log2PageSize: 10, maxPages: ushort.MaxValue, numPagesToGrowBy: 8, numPagesPerFPM: 8, isHC: false),
            new MSFParms(pageSize: 2048, log2PageSize: 11, maxPages: ushort.MaxValue, numPagesToGrowBy: 4, numPagesPerFPM: 4, isHC: false),
            new MSFParms(pageSize: 4096, log2PageSize: 12, maxPages: 32767,           numPagesToGrowBy: 2, numPagesPerFPM: 1, isHC: false)
        };

        //Note that you can apparently specify custom page sizes by compiling with /pdbpagesize
        //Need to test what numPaegsToGrowBy becomes when using page sizes of 8192+. I'm guessing the growth factor will just be 1?
        private static readonly MSFParms[] highCapacityParams =
        {
            new MSFParms(pageSize: 1024, log2PageSize: 10, maxPages: 0x100000, numPagesToGrowBy: 8, numPagesPerFPM: 1, isHC: true),
            new MSFParms(pageSize: 2048, log2PageSize: 11, maxPages: 0x100000, numPagesToGrowBy: 4, numPagesPerFPM: 1, isHC: true),
            new MSFParms(pageSize: 4096, log2PageSize: 12, maxPages: 0x100000, numPagesToGrowBy: 2, numPagesPerFPM: 1, isHC: true)
        };

        public static ref readonly MSFParms FromPageSize(int pageSize)
        {
            for (var i = 0; i < smallParams.Length; i++)
            {
                ref var msfParms = ref smallParams[i];

                if (msfParms.PageSize == pageSize)
                    return ref msfParms;
            }

            throw new ArgumentException($"Invalid page size '{pageSize}'");
        }

        public static ref readonly MSFParms FromPageSizeHC(int pageSize)
        {
            for (var i = 0; i < highCapacityParams.Length; i++)
            {
                ref var msfParms = ref highCapacityParams[i];

                if (msfParms.PageSize == pageSize)
                    return ref msfParms;
            }

            throw new ArgumentException($"Invalid page size '{pageSize}'");
        }

        /// <summary>
        /// Gets the number of bytes that each page should occupy.<para/>
        /// Is stored in <see cref="BigMsfHdr.PageSize"/><para/>
        /// cbPg
        /// </summary>
        public int PageSize;

        /// <summary>
        /// Gets the log(2) of the <see cref="PageSize"/>.<para/>
        /// A common operation when editing PDBs is calculating the number of pages that would be required to store a given number of bytes. There are two ways that this can be done.
        /// The first is by using ceiling division ((numBytes + pageSize - 1) / pageSize). However, since the page size is always a power of 2, it is possible to use bit-shifting tricks
        /// to achieve higher performance: ((numBytes + pageSize - 1) >> log2PageSize). in microsoft-pdb, just the lgCbPg will be passed to functions. By bit-shifting "1" by the Log 2
        /// of the Page Size, you can recover the original Page Size. Thus, you will often see expressions like ((numBytes + ((1 &lt;&lt; log2PageSize) - 1) >> log2PageSize)<para/>
        /// lgCbPg
        /// </summary>
        public int Log2PageSize;

        /// <summary>
        /// Given an offset, you can get how far it partially protrudes into a page by doing offset % pageSize. However, modulo is computationally expensive.
        /// Since page size is always a power of 2, the same thing can be achieved by doing offset &amp; (pageSize - 1) since an odd number will have all lower bits set.
        /// maskCbPgMod stores the value of pageSize - 1 so it can be reused in any required calculations<para/>
        /// maskCbPgMod
        /// </summary>
        public int ModuloPageSizeBitMask;

        /// <summary>
        /// Gets the maximum number of bits that may exist in the FPM. This is typically the same value as <see cref="MaxPages"/>, however for some reason
        /// in the case of small PDBs this value is MaxPages + 1.<para/>
        /// cbitsFpm
        /// </summary>
        public int MaxFpmBits;

        //The number of pages to grow the PDB by when the PDB runs out of free pages
        /// <summary>
        /// Gets the number of pages to grow by the PDB by when the PDB runs out of free pages.<para/>
        /// By default <see cref="BigMsfHdr.NumPages"/> will contain just enough pages to store the master
        /// index and both FPMs. However, when the PDB goes go serialize the stream table, it will see its
        /// run out of pages and will increment the maximum page number by this value. e.g. if we're a high
        /// capacity PDB and our initial page count is 3, a growth factor of 8 will bring us to an initial
        /// count of 11 pages.<para/>
        /// cpgFileGrowth
        /// </summary>
        public int NumPagesToGrowBy;

        /// <summary>
        /// Gets the maximum number of pages that can exist in the PDB<para/>
        /// pnMax
        /// </summary>
        public PN MaxPages;

        //I'm not sure how you would describe this. Fpm0PageNo + this value gives you the offset of the second FPM.
        //But then (2*cpnFpm) + 1 gives pnDataMin. I don't know what that means. I don't think cpnFpm is the number of pages
        //needed to store the FPM, because I don't understand how in 16-bit world it required 8 pages to store
        /// <summary>
        /// Gets the number of pages required to represent the FPM. In 16-bit (V2) PDBs, the FPM is a contiguous block of pages allocated at
        /// the beginning of the file, containing all of the bits required to represent every single possible page that may exist in the PDB
        /// (i.e. all 65536 of them). The number of pages required to represent every single bit of a V2 PDB depends on its page size. e.g.
        /// if the page size is 1024, you can represent 8192 bits per page, and would require 8 pages in order to represent the complete FPM.<para/>
        ///
        /// In 32-bit (high capacity, V7) PDBs, the FPM is structured differently. A single FPM page for FPM 0 and FPM 1 each is allocated at the beginning
        /// of the file, and then additional FPM pages are allocated non-contiguously at intervals based on the page size as the PDB grows in size.
        /// <para/>
        /// cpnFpm
        /// </summary>
        public PN NumPagesPerFPM;

        /// <summary>
        /// Gets the page number that the first FPM page should occupy. This is always page 1.<para/>
        /// Is stored in <see cref="BigMsfHdr.FpmPageNo"/><para/>
        /// pnFpm0
        /// </summary>
        public PN Fpm0PageNo;

        /// <summary>
        /// Gets the page number that the second FPM page should occupy.<para/>
        /// pnFpm1
        /// </summary>
        public PN Fpm1PageNo;

        //Is stored in BigMsfHdr.NumPages
        /// <summary>
        /// Gets the minimum number of pages required to represent a PDB with this configuration. Is stored in <see cref="BigMsfHdr.NumPages"/>.<para/>
        /// At a minimum, a PDB must be able to represent the master index and two FPMs. The number of pages required to do this depends on the number of pages
        /// needed to represent each FPM. In high capacity PDBs this value is 3 (1+1+1, since additional FPM pages are allocated at set intervals as required)
        /// but in V2 PDBs, the entire FPM is allocated up front, which means we need _way_ more pages initially
        /// </summary>
        public PN MinNumPages;

        public MSFParms(int pageSize, int log2PageSize, uint maxPages, int numPagesToGrowBy, uint numPagesPerFPM, bool isHC)
        {
            PageSize = pageSize;
            Log2PageSize = log2PageSize;
            ModuloPageSizeBitMask = pageSize - 1;
            MaxFpmBits = (int) (isHC ? maxPages : maxPages + 1);
            NumPagesToGrowBy = numPagesToGrowBy;
            MaxPages = maxPages;
            NumPagesPerFPM = numPagesPerFPM;
            Fpm0PageNo = 1;
            Fpm1PageNo = 1 + NumPagesPerFPM; //The second FPM begins immediately after the first one (which itself begins after the master index)
            MinNumPages = 1 + numPagesPerFPM + numPagesPerFPM; //At a minimum, a PDB contains a master index and enough pages to represent two FPMs
        }
    }
}
