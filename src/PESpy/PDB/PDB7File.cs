using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using PESpy.PDB;
using PESpy.View;

namespace PESpy
{
    /// <summary>
    /// Represents a CodeView Program Database v7 (PDB) file.
    /// </summary>
    public class PDB7File : PDBFile
    {
        private BigMsfHdr msfHeader;

        /// <summary>
        /// Gets the MSF Header that provides high level information about the structure and layout of the PDB File.
        /// </summary>
        public ref readonly BigMsfHdr MsfHeader => ref msfHeader;

        protected SI streamTableLocation;

        /// <summary>
        /// Gets the pages that comprise the stream table that describes all of the streams that exist in the PDB
        /// </summary>
        public ref readonly SI StreamTableLocation => ref streamTableLocation;

        protected internal override int PageSize => msfHeader.PageSize;

        protected internal override int ActiveFpmPageNo => msfHeader.FpmPageNo;

        protected internal override int NumPages => msfHeader.NumPages;

        internal override IStreamTable CreateStreamTable(in MemoryChunk chunk, int pageSize) => new BigMsfHdr.StreamTable(chunk, pageSize);

        private MSFParms msfParms; //Only set when writing

        //Open an existing file
        internal PDB7File(string fileName, in MemoryMappedFileHolder mmf) : base(fileName, mmf, PDBFileKind.V7)
        {
#if STRESS_TEST
            _ = PreviousStreamTable;
            _ = PDB;
            _ = TPI;
            _ = DBI;
            _ = IPI;

            _ = NameMap;
#endif
        }

        //This does not perform a "Commit". We are just directly hacking the PDB
        public unsafe void Save()
        {
            //File.OpenWrite opens the file with FileAccess.Write, but we need ReadWrite to memory map it
            using var fs = File.Open(FileName, FileMode.OpenOrCreate, FileAccess.ReadWrite);

            //todo: how does microsoft-pdb go about zeroing stuff

            var pageSize = PageSize;

            fs.SetLength(NumPages * pageSize);

            using var mmf = new MemoryMappedFileHolder(fs, MemoryMappedFileAccess.ReadWrite);

            var dest = new Span<byte>(mmf.Address, (int) mmf.Length);

            /* Now we need to copy a bunch of stuff:
             * 1. The first 3 pages of the global block (which may contain more than 3 pages
             *    if we consolidated everything into it for the purposes of showing a view
             * 2. Every page of every block
             */

            //Copy the Master Index, FPM 0 and FPM 1
            new Span<byte>(globalBlock.LocalPointer, globalBlock.Length).Slice(0, pageSize * 3).CopyTo(dest);

            foreach (var kv in globalBlock.blockCache)
            {
                //Copy each (modified) page into its respective area of the MMF
                if (kv.Value.hasChanges)
                {
                    for (var i = 0; i < kv.Key.Length; i++)
                    {
                        //In the source, the page start is relative to the location of that page within the block
                        //In the dest, the page start is relative to the actual page within the entire file.
                        var sourcePageStart = i * pageSize;
                        var destPageStart = kv.Key[i] * pageSize;

                        var length = pageSize;

                        var previous = kv.Key[i];

                        //Include adjacent pages in the batch
                        for (var j = i + 1; j < kv.Key.Length; j++)
                        {
                            var current = kv.Key[j];

                            if (current == previous + 1)
                            {
                                i++;
                                previous = current;
                                length += pageSize;
                            }
                            else
                                break;
                        }

                        var source = new Span<byte>(kv.Value.LocalPointer + sourcePageStart, length);
                        var destination = new Span<byte>(mmf.Address + destPageStart, length);
                        source.CopyTo(destination);
                    }

                    kv.Value.hasChanges = false;
                }
            }
        }

        protected override void ReadHeaders()
        {
            //Initialize the MSF

            /* An absolutely minimal PDB created by MSF::Open is 11KB large. cbPgDef is 0x400, which is the default page size that is used if no explicit
             * page size is provided. "Normal" PDBs tend to use 0x1000 (cbPgMax - 4096 bytes). 10 pages are allocated (after master) because lgCbPg is 10 in rgmsfparms_hc
             * for 0x400
             * 
             * The minimal PDB has the following page layout
             * 
             * 0: Master
             * - BIGMSF_HDR
             * - Padding
             * 
             * 1: FPM 0 (Active)
             * - 0xE0 in first byte, the rest are 0xFF
             * 
             * 2: FPM 1 (Inactive)
             * - 0x00 in all bytes
             * 
             * 3: Stream Table
             * - Stream Table
             *   - Just contains NumStreams: 0
             * - Padding
             * 
             * 4: Stream Table Page List
             * - SI Pages
             *   = Just contains "3", which is the singular page that the Stream Table encompasses (i.e. page above)
             * - Padding
             * 5-10: Free
             * 
             * Page numbers are allocated by calling FPM.nextPn(). There is nothing inherently hardcoded to say that pages 1-4 contain these items;
             * but they inherently do as a result of the order in which FPM.nextPn() was called for certain purposes
             */

            var globalChunk = new MemoryChunk(globalBlock, 0);

            //The PDB begins with the MSF Header
            msfHeader = new BigMsfHdr(globalChunk);
            globalBlock.pageSize = msfHeader.PageSize;

            /* PDBs have two Free Page Maps. The compiler plays games deciding which one is the active one as it constructs the PDB.
             * The definition of which page number is the first one and which is the second one is defined in the MSFParms structure
             * in rgmsfparms and rgmsfparms_hc respectively. In "high capacity" MSFs (i.e. anything with a BigMsfHeader) the FPMs are
             * always on pages 1 and 2 respectively. However, in V2 PDBs, the pages containing the first FPM0 and FPM1 record
             * are based on the selected page size of the PDB. As we currently only support V7 PDBs, we can hard code the fact that
             * the FPMs exist on either page 1 or page 2 */
            if (msfHeader.FpmPageNo != 1 && msfHeader.FpmPageNo != 2)
                throw new InvalidOperationException("Active FPM should either be 1 or 2");

            //We don't need to do any temporary slicing prior to constructing a paged memory block.
            //The pages in the block could be all over the place; it's up to the paged block to seek X bytes
            //into the PDB to read the correct data
            fpm0 = new FPM(1, msfHeader.PageSize, msfHeader.NumPages, globalBlock, isBig: true);
            fpm1 = new FPM(2, msfHeader.PageSize, msfHeader.NumPages, globalBlock, isBig: true);

            /* TLDR: In order to know what streams exist in the PDB, and which pages those streams span across, we need to read the Stream Table.
             * And the Stream Table _itself_ could potentially be very big, and span multiple pages! So first, we must read the list of pages
             * that the Stream Table spans
             * 
             * Detailed Explanation
             * --------------------
             * 
             * How many streams are embedded in the PDB? To answer this, we need to read the "stream directory" from the PDB. The stream directory lists three pieces of information
             * 1. How many streams are there?
             * 2. How big are each of these streams?
             * 3. Which pages do each of these streams occupy?
             *
             * e.g. we might have three streams:
             *
             * 1: 0x1000 bytes large, and occupies pages 1, 7 and 9
             * 2: 0x2000 bytes large, and occupies pages 2, 3 and 5
             * 3: 0x3000 bytes large, and occupies pages 4, 6 and 8
             *
             * All of this information, of course, is stored in pages within the PDB file. Which pages is the info stored in? And where? The answer to this can be found by utilizing the BIGMSF_HDR's
             * SI_PERSIST.cb and mpspnpnSt members. mpspnpnSt contains the pages that contain the pages of the stream table. SI_PERSIST.cb contains the size of the stream table.
             * pages occupy. So if "cb" is 5000, we need two pages to store the stream directory, and blockMapAddr points to the indices of those two pages.
             * mpspnpnSt typically contains a single page, however if the stream table consists of, say, 75 streams with 1024 pages in a PDB with 1024 byte pages, there could be so much data
             * that the stream table spans 301 pages. Simply listing the existance of those 301 pages would take 1204 bytes, which means mpspnpnSt would list the two pages that the stream table
             * page list can be found in. */

            var pagesOfStreamTablePageList = msfHeader.PagesOfStreamTablePageList;

            streamTableLocation = new SI(
                globalBlock.SlicePaged(pagesOfStreamTablePageList.ToArray(), pagesOfStreamTablePageList.Length * msfHeader.PageSize),
                msfHeader.StreamTableSizeInfo.ByteCount,
                msfHeader.PageSize
            );

            //Now read the Stream Table from all of those pages
            StreamTable = new BigMsfHdr.StreamTable(
                globalBlock.SlicePaged(StreamTableLocation),
                msfHeader.PageSize
            );

            //We have now read the minimum amount of info that must exist in a valid PDB file. All other sections like PDB, DBI, etc are completely optional
        }

        protected override void WriteGlobals(ViewWriter writer)
        {
            writer.WriteGlobal(MsfHeader);

            //We do not write the FPM; these raw bytes are automatically collected during merging
            //under the FPM0 and FPM1 regions

            writer.WriteGlobal(StreamTableLocation);

            WriteMsfStreamViews(writer);
        }
    }
}
