using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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

        /* Tracks all pages that were freed in the current transaction that were previously committed in a prior transaction. microsoft-pdb
         * does not allow committed pages to be reused in the same transaction in which they are freed, ostensibly because someone else might be
         * accessing the PDB at the same time, based on the information contained in the previous committed active FPM number. The items in this list
         * are merged into the active FPM just prior to serializing the FPM to disk, thereby preventing anyone (in particular the stream table) from having
         * written data to any of these freed pages prior to the current transaction being committed */
        private BitArray fpmFreed;

        /* Stores a copy of the last committed FPM. Pages that were committed in a prior transaction cannot be immediately reused in the transaction in which
         * they are freed. However, pages that were free at the beginning of the current transaction that were temporarily allocated and then freed again _can_
         * immediately be returned to the active FPM. This value helps us keep track of which pages were free at the beginning of the current transaction
         * and so can be returned immediately */
        private BitArray fpmCommitted;

        private MSFParms msfParms; //Only set when writing

        //Open an existing file
        internal PDB7File(string fileName, in MemoryMappedFileHolder mmf) : base(fileName, mmf, PDBFileKind.V7)
        {
            if (mmf.Writable)
            {
                //We're going to need to track the freeing of FPM pages

                msfParms = MSFParms.FromPageSizeHC(PageSize);

                ref readonly var activeFpm = ref ActiveFPM;

                fpmFreed = new BitArray(activeFpm.PageMap.Count);
                fpmCommitted = new BitArray(activeFpm.PageMap);
            }
            else
            {
                fpmCommitted = null!;
                fpmFreed = null!;
            }

#if STRESS_TEST
            _ = PreviousStreamTable;
            _ = PDB;
            _ = TPI;
            _ = DBI;
            _ = IPI;

            _ = NameMap;
#endif
        }

        //Create a new file
        internal unsafe PDB7File(string fileName, int pageSize) : base(fileName, PDBFileKind.V7)
        {
            //When microsoft-pdb appends to a stream, it writes straight to the file. When you then go to read from the stream,
            //it reads it from the file. It doesn't keep the stream info in memory. This strategy won't work for us, because
            //we want to provide a continuous rich data model for accessing the data as it is written. Therefore, I think we will
            //need to store the whole PDB in memory.

            var msfParms = MSFParms.FromPageSizeHC(pageSize);
            this.msfParms = msfParms;

            //Our initial allocation will contain the amount of memory required to store the master index and both FPMs. All additional
            //pages will be allocated and managed by the stream table
            var initialBytes = pageSize * msfParms.MinNumPages;

            var mmf = Marshal.AllocHGlobal(initialBytes); //Ownership is transferred to the global block
            Unsafe.InitBlockUnaligned((void*) mmf, 0, (uint) initialBytes); //Zero out the memory
            globalBlock = new PDBGlobalMemoryBlock((byte*) mmf, initialBytes, true, ownsMemory: true, this);

            //MSF_HB::afterCreate
            var bigMsfHdr = new BigMsfHdr(new MemoryChunk(globalBlock, 0));
            bigMsfHdr.SetMagic(BigMsfHdr.BigHdrMagic);
            bigMsfHdr.PageSize = msfParms.PageSize;
            bigMsfHdr.FpmPageNo = msfParms.Fpm0PageNo;
            bigMsfHdr.NumPages = msfParms.MinNumPages;

            this.msfHeader = bigMsfHdr;

            //microsoft-pdb next initializes the FPM and Stream Table
            var fpm = new FPM(msfParms.Fpm0PageNo, msfParms.PageSize, msfParms.MinNumPages, globalBlock, true);

            //Mark all pages as free
            fpm.SetAll();
            fpm0 = fpm;
            fpm1 = new FPM(msfParms.Fpm1PageNo, msfParms.PageSize, msfParms.MinNumPages, globalBlock, true);

            //Used to defer frees on pages previously committed in prior transactions
            //until the end of the current transaction so that they cannot be immediately
            //be reused while someone else (theoretically) might be using them using the
            //current on-disk active FPM
            fpmFreed = new BitArray(fpm.PageMap.Count);
            fpmCommitted = new BitArray(fpm.PageMap);

            //Allocate pages in the FPM for each of our special pages (the master index and all pages we need for storing the FPMs themselves)
            for (var i = 0; i < msfParms.MinNumPages; i++)
            {
                var pn = AllocPage();
                Debug.Assert(i == pn, $"Expected to allocate page {i} but page {pn} was allocated instead");
            }

            /* Create an empty stream table. When we deserialize our stream table, we read all information from it into our StreamTable type.
             * We cannot have a StreamTable partially backed by an MMF. There needs to be an in-memory representation that is completely separate
             * from the on-disk implementation. This is because the stream table is backed by a list of pages that describe where it resides,
             * but those pages cannot be known until we've measured the size of a stream table that actually exists! So our initial StreamTable
             * does not have a MemoryChunk. Whenever we serialize it, we update the MemoryChunk to point to a list of pages that describe the
             * location of the stream table */
            StreamTable = new BigMsfHdr.StreamTable(globalBlock);

            Commit();
        }

        internal PN AllocPage()
        {
            ref var activeFPM = ref ActiveFPM;
            var pn = activeFPM.AllocPage();

            var numPages = msfHeader.NumPages;

            if (pn >= numPages) //pn is an index. So if we have a maximum of 11 pages, and allocate PN 11, that's the 12th page
            {
                var growth = msfParms.NumPagesToGrowBy;

                //Grow the MSF
                var newNumPages = numPages + growth;
                msfHeader.NumPages = newNumPages;

                //We need to update the size of our FPMs, else when we try and show a view it will crash because
                //there's fewer elements in the FPM than there are pages

                //They should all have the same length
                var oldLength = fpmCommitted.Length;
                Debug.Assert(fpmCommitted.Length == fpm0.PageMap.Length);
                Debug.Assert(fpmCommitted.Length == fpm1.PageMap.Length);
                Debug.Assert(fpmFreed.Length == fpm1.PageMap.Length);

                //todo: should we just give it a whole page's worth?

                //When the FPM is first created, its length is 1 byte (despite the fact we only need 3 bits).
                //When we allocate the 4th page, we're still within the 8 bits, so are fine. But if we now set
                //the size to exactly 11 bits for the new length of 11 pages, we're going to be in trouble
                //because when we add the 12th page, there isn't a 12th bit! So round up to the next multiple of 8.
                //When the FPM is deserialized I believe it will just give the bit array a whole page's worth
                var numBits = SI.DivideUp(newNumPages, 8) * 8;

                fpmCommitted.Length = numBits;
                fpmFreed.Length = numBits;
                fpm0.PageMap.Length = numBits;
                fpm1.PageMap.Length = numBits;

                for (var i = oldLength; i < numBits; i++)
                {
                    fpmCommitted[i] = true;
                    //fpmFreed should be false
                    fpm0.PageMap[i] = true;
                    fpm1.PageMap[i] = true;
                }

                //Mark all of the new pages as free
            }

            return pn;
        }

        internal void FreePage(PN pn)
        {
            if (fpmCommitted[pn])
                ActiveFPM.FreePage(pn);
            else
            {
                fpmFreed[pn] = true;
            }
        }

        public SN AllocStream() => ((BigMsfHdr.StreamTable) StreamTable).AllocStream();

        public unsafe void AppendStream(SN sn, IntPtr data, int size)
        {
            //Get the existing stream size
            var si = StreamTable[sn];

            if (si.ByteCount == -1)
                throw new InvalidOperationException($"Cannot append to stream {sn}: stream is not initialized");

            var start = si.ByteCount;
            var total = start + size;

            //How many pages will we need to store all existing and new data?
            var numPages = SI.DivideUp(total, PageSize);

            //Ensure we've got enough pages
            for (var i = si.PageList.Count; i < numPages; i++)
                si.PageList.Add(AllocPage());

            //If there's already data, we need to expand the buffer and copy the data over
            var chunk = globalBlock.SlicePaged(si.PageList, total, copyOld: true);
            chunk.PokeSpan<byte>(start, size, new Span<byte>((byte*) data, size));

            //Store the updated SI
            si.ByteCount = total;
            StreamTable[sn] = si;
        }

        public void Commit()
        {
            if (!globalBlock.writable)
                throw new InvalidOperationException("Cannot commit changes: PDB was not opened as writable");

            //MSF_HB::Commit

            /* Every time the PDB is committed, it seems like the location of the stream table's pages can change.
             *
             * There are two SI's we need to create, and four steps we need to take
             * 1. First, before we allocate any pages, if a SI for the stream table was already allocated previously, free all of these pages
             * 2. Then, create an SI for storing the contents of the stream table itself. We need to know how many bytes would be required to store the contents
             *    of the stream table, and then we call allocPn until we've allocated enough pages to be able to store its contents. We then go ahead and store
             *    the SI we created in the stream table for continued usage within the PDB manager
             * 3. Based on the total number of bytes (and therefore pages) that were required to store the stream table,
             *    allocate enough pages to store _those_ page numbers, to eventually be stored in siPnList, which will shortly be copied into BigMsfHdr.mpspnpnSt
             * 4. Finally, if we had a list of pages that describe the location of the stream table prior to executing #3 (i.e. the last siPnList),
             *    free all of the pages, and then update siPnList with the latest list of pages describing the location of the stream table
             */

            if (StreamTable.HasStream(SN.ST))
            {
                //Free all of the pages that were associated with the stream
                var oldSI = StreamTable[SN.ST];

                for (var i = 0; i < oldSI.PageList.Count; i++)
                {
                    FreePage(oldSI.PageList[i]);
                }

                //We must allocate a new list, because we need to serialize the contents of the old list to disk
                var newSI = new SI(default, -1, new List<PN>());

                var strmTbl = ((BigMsfHdr.StreamTable) StreamTable);

                //Serialize the stream table into this SI
                strmTbl.Serialize(ref newSI, this);

                //Because we allocated our own list, we need to store the stream into the stream table manually
                strmTbl.StreamSizes[SN.ST] = newSI.ByteCount;
                strmTbl.StreamInfos[SN.ST] = newSI;
                strmTbl.StreamPages[SN.ST] = newSI.PageList;
            }
            else
            {
                var strmTbl = ((BigMsfHdr.StreamTable) StreamTable);

                //The first 5 pages (snSt -> snIpi) are pre-allocated, so the List<PN> is already
                //in both the StreamList and PageList
                var si = strmTbl[SN.ST];

                //Serialize the stream table into this SI
                strmTbl.Serialize(ref si, this);

                strmTbl[SN.ST] = si;
            }

            //Now we create a fake SI for storing the location of the stream table itself
            var streamTableLocationPages = ((PagedMemoryBlock) ((BigMsfHdr.StreamTable) StreamTable).chunk.block).pageList;
            var streamTableLocationSize = streamTableLocationPages.Count * sizeof(int);

            var pagesOfStreamTablePageList = new List<PN>();

            var numPages = SI.DivideUp(streamTableLocationSize, PageSize);

            ref var activeFPM = ref ActiveFPM;

            for (var i = 0; i < numPages; i++)
                pagesOfStreamTablePageList.Add(AllocPage());

            this.streamTableLocation = new SI(
                globalBlock.SlicePaged(pagesOfStreamTablePageList, pagesOfStreamTablePageList.Count * msfHeader.PageSize, copyOld: false),
                streamTableLocationSize,
                streamTableLocationPages
            );

            //Now write the location of the stream table to disk
            streamTableLocation.Serialize();

            //If we had a previous list of pages describing the location of the stream table's pages, clear that list now
            var previousPagesOfStreamTablePageList = MsfHeader.PagesOfStreamTablePageList;

            for (var i = 0; i < previousPagesOfStreamTablePageList.Length; i++)
                FreePage(previousPagesOfStreamTablePageList[i]);

            //Now update the BigMsfHdr

            var siSt = msfHeader.StreamTableSizeInfo;
            siSt.ByteCount = StreamTable.StreamInfos[SN.ST].ByteCount;
            siSt.PageList = 0;

            //Copy siPnList.mpspnpn to bighdr.mpspnpnSt
            msfHeader.SetPagesOfStreamTablePageList(pagesOfStreamTablePageList);

            activeFPM.Add(fpmFreed);

            activeFPM.Serialize(globalBlock);

            if (ActiveFpmPageNo == 1)
            {
                fpm1.EnsureEmptyPages(this);
            }
            else
            {
                fpm0.EnsureEmptyPages(this);
            }

            //Because all of our data is memory mapped, if the total number of FPM pages has increased,
            //we need to allocate some memory for the inactive FPM and zero it out so that we don't end up
            //with garbage in the resulting file

            Save();

            /* After everything has been committed, the in-memory active FPM is toggled (the on-disk active FPM remains the previous FPM at the time of commit) 
             *
             * In our data model, there are two separate FPMs. So when we toggle the active FPM, we're now looking at a completely different set of pages.
             * But that's not how microsoft-pdb works. It has a single "fpm" which is then either written to the first FPM page or the second FPM page.
             * As such, we will simply replace the bits from the previously active one to the newly active one */

            if (msfHeader.FpmPageNo == 1)
            {
                fpm1.CopyFrom(fpm0);
                msfHeader.FpmPageNo = 2;
                fpmCommitted = new BitArray(fpm1.PageMap);
            }
            else
            {
                fpm0.CopyFrom(fpm1);
                msfHeader.FpmPageNo = 1;
                fpmCommitted = new BitArray(fpm0.PageMap);
            }

            if (fpmFreed.Length != activeFPM.PageMap.Length)
                fpmFreed.Length = activeFPM.PageMap.Length;

            fpmFreed.SetAll(false);
        }

        private unsafe void Save()
        {
            //File.OpenWrite opens the file with FileAccess.Write, but we need ReadWrite to memory map it
            using var fs = File.Open(FileName, FileMode.OpenOrCreate, FileAccess.ReadWrite);

            //todo: how does microsoft-pdb go about zeroing stuff

            var pageSize = PageSize;

            fs.SetLength(NumPages * pageSize);

            var mmf = new MemoryMappedFileHolder(fs, MemoryMappedFileAccess.ReadWrite);

            try
            {
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
                        for (var i = 0; i < kv.Key.Count; i++)
                        {
                            //In the source, the page start is relative to the location of that page within the block
                            //In the dest, the page start is relative to the actual page within the entire file.
                            var sourcePageStart = i * pageSize;
                            var destPageStart = kv.Key[i] * pageSize;

                            var length = pageSize;

                            var previous = kv.Key[i];

                            //Include adjacent pages in the batch
                            for (var j = i + 1; j < kv.Key.Count; j++)
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
            finally
            {
                mmf.Close();
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
                globalBlock.SlicePaged(pagesOfStreamTablePageList.ToList(), pagesOfStreamTablePageList.Length * msfHeader.PageSize, copyOld: false),
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

        protected override void WriteView(ViewWriter writer)
        {
            writer.WriteGlobal(MsfHeader);

            //We do not write the FPM; these raw bytes are automatically collected during merging
            //under the FPM0 and FPM1 regions

            writer.WriteGlobal(StreamTableLocation);

            WriteMsfStreamViews(writer);
        }
    }
}
