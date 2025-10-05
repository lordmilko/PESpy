using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;

namespace PESpy.PDB
{
    /// <summary>
    /// Provides facilities for building PDB Files.<para/>
    /// This type only supports building files compatible with <see cref="PDB7File"/>.
    /// </summary>
    public class PDBFileBuilder : IDisposable
    {
        public MsfStreamBuilder.PDB? PDB { get; set; }

        public MsfStreamBuilder.DBI? DBI { get; set; }

        public StreamTableBuilder StreamTable { get; private set; }

        public string FileName { get; }

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

        private FPMBuilder fpm;

        public static PDBFileBuilder Create(string fileName, int pageSize = 1024) => new PDBFileBuilder(fileName, pageSize);

        public static PDBFileBuilder FromPDB(string fileName) => FromPDB(PDBFile.FromFile(fileName));

        public static PDBFileBuilder FromPDB(PDBFile pdbFile)
        {
            if (pdbFile.PDBKind != PDBFileKind.V7)
                throw new NotImplementedException();

            return new PDBFileBuilder((PDB7File) pdbFile);
        }

        private readonly MSFParms msfParms;

        /// <summary>
        /// <see cref="BigMsfHdr.PageSize"/>
        /// </summary>
        internal readonly int PageSize;

        internal int NumPages;
        internal int ActiveFpmPage;

        //Only valid during serialization
        private PDBGlobalMemoryBlock? globalBlock;

        private Dictionary<SN, CustomStreamBuilder> customStreams = new();

        private PDBFileBuilder(string fileName, int pageSize)
        {
            FileName = fileName;
            this.PageSize = pageSize;
            msfParms = MSFParms.FromPageSizeHC(pageSize);

            fpm = new FPMBuilder(msfParms.MinNumPages + msfParms.NumPagesToGrowBy, this);

            fpmFreed = new BitArray(fpm.PageMap.Count);
            fpmCommitted = new BitArray(fpm.PageMap);

            StreamTable = new StreamTableBuilder(this);

            EnsureMSF();
        }

        private PDBFileBuilder(PDB7File pdbFile)
        {
            FileName = pdbFile.FileName!;
            PageSize = pdbFile.PageSize;
            msfParms = MSFParms.FromPageSizeHC(PageSize);

            var fpm = pdbFile.ActiveFPM;
            this.fpm = new FPMBuilder(fpm, this);

            //Used to defer frees on pages previously committed in prior transactions
            //until the end of the current transaction so that they cannot be immediately
            //be reused while someone else (theoretically) might be using them using the
            //current on-disk active FPM
            fpmFreed = new BitArray(fpm.PageMap.Count);
            fpmCommitted = new BitArray(fpm.PageMap);

            StreamTable = new StreamTableBuilder((BigMsfHdr.StreamTable) pdbFile.StreamTable, this);
        }

        public MsfStreamBuilder.PDB AcquirePDB(Guid? guid = null)
        {
            if (PDB == null)
            {
                PDB = new MsfStreamBuilder.PDB(this, guid);

#if PDB1_COMPATIBILITY
                //PDB1 commits the PDB immediately
                Commit(PDBCommitFlags.PDB);
#endif
            }

            return PDB;
        }

        public MsfStreamBuilder.TPI AcquireTPI()
        {
            if (TPI == null)
                TPI = new MsfStreamBuilder.TPI(this, SN.TPI);

            return TPI;
        }

        public MsfStreamBuilder.DBI AcquireDBI()
        {
            if (DBI == null)
            {
                DBI = new MsfStreamBuilder.DBI(this);
                DBI.Init();
            }

            return DBI;
        }

        public MsfStreamBuilder.TPI AcquireIPI()
        {
            if (IPI == null)
                IPI = new MsfStreamBuilder.TPI(this, SN.IPI);

            return IPI;
        }

        public MsfStreamBuilder.GSI AcquireGSI()
        {
            if (GSI == null)
                GSI = new MsfStreamBuilder.GSI(this);

            return GSI;
        }

        public MsfStreamBuilder.PSGSI AcquirePSGSI()
        {
            if (PSGSI == null)
                PSGSI = new MsfStreamBuilder.PSGSI(this);

            return PSGSI;
        }

        private unsafe void EnsureMSF()
        {
            //File.OpenWrite opens with FileMode.OpenOrCreate
            using var fs = File.Open(FileName, FileMode.Create, FileAccess.ReadWrite);

            var initialPages = msfParms.MinNumPages + msfParms.NumPagesToGrowBy;

            var minAlloc = initialPages * msfParms.PageSize;

            fs.SetLength(minAlloc);

            using var mmf = new MemoryMappedFileHolder(fs, MemoryMappedFileAccess.ReadWrite);

            globalBlock = new PDBGlobalMemoryBlock(mmf.Address, minAlloc, true, PageSize, null!);
            var bigMsfHdr = new BigMsfHdr(new MemoryChunk(globalBlock, 0));

            //MSF_HB::afterCreate
            bigMsfHdr.SetMagic(BigMsfHdr.BigHdrMagic);
            bigMsfHdr.PageSize = msfParms.PageSize;
            bigMsfHdr.FpmPageNo = msfParms.Fpm0PageNo;
            this.ActiveFpmPage = msfParms.Fpm0PageNo;
            bigMsfHdr.NumPages = initialPages; //We're doing everything at once here, so we're never going to update NumPages on the BigMsfHdr again before this method returns
            NumPages = initialPages;

            //Mark all pages as free
            fpm.SetAll();

            //Allocate pages in the FPM for each of our special pages (the master index and all pages we need for storing the FPMs themselves)
            for (var i = 0; i < msfParms.MinNumPages; i++)
            {
                var pn = AllocPage();
                Debug.Assert(i == pn, $"Expected to allocate page {i} but page {pn} was allocated instead");
            }

            //No need to measure the FPM
            var newSiSt = StreamTable.Measure();

            //Now we create a fake SI for storing the location of the stream table itself
            var streamTableLocationSize = newSiSt.PageList.Count * sizeof(int);

            var newSiStLocation = AllocTempStream(streamTableLocationSize);

            FinalizeCommit(ref bigMsfHdr, newSiSt, newSiStLocation);

            globalBlock = null;
        }

        internal void AllocPages(SN sn, int size)
        {
            ref var si = ref StreamTable[sn];

            var numPages = SI.DivideUp(size, PageSize);

            for (var i = si.PageList.Count; i < numPages; i++)
                si.PageList.Add(AllocPage());

            si.ByteCount = size;
        }

        internal StreamInfoBuilder AllocTempStream(int size)
        {
            var numPages = SI.DivideUp(size, PageSize);

            var si = new StreamInfoBuilder
            {
                sn = SN.Nil,
                ByteCount = size,
                PageList = new List<PN>(numPages)
            };

            for (var i = si.PageList.Count; i < numPages; i++)
                si.PageList.Add(AllocPage());

            return si;
        }

        private PN AllocPage()
        {
            var pn = fpm.AllocPage();

            if (pn >= NumPages) //pn is an index. So if we have a maximum of 11 pages, and allocate PN 11, that's the 12th page
            {
                var growth = msfParms.NumPagesToGrowBy;

                //Grow the MSF
                var newNumPages = NumPages + growth;
                NumPages = newNumPages;

                //We need to update the size of our FPMs, else when we try and show a view it will crash because
                //there's fewer elements in the FPM than there are pages

                //They should all have the same length
                var oldLength = fpmCommitted.Length;
                Debug.Assert(fpmCommitted.Length == fpm.PageMap.Length);
                Debug.Assert(fpmFreed.Length == fpm.PageMap.Length);

                //When the FPM is first created, its length is 1 byte (despite the fact we only need 3 bits).
                //When we allocate the 4th page, we're still within the 8 bits, so are fine. But if we now set
                //the size to exactly 11 bits for the new length of 11 pages, we're going to be in trouble
                //because when we add the 12th page, there isn't a 12th bit! So round up to the next multiple of 8.
                //When the FPM is deserialized I believe it will just give the bit array a whole page's worth
                var numBits = SI.DivideUp(newNumPages, 8) * 8;

                fpmCommitted.Length = numBits;
                fpmFreed.Length = numBits;
                fpm.PageMap.Length = numBits;

                for (var i = oldLength; i < numBits; i++)
                {
                    fpmCommitted[i] = true;
                    //fpmFreed should be false
                    fpm.PageMap[i] = true;
                }
            }

            return pn;
        }

        internal void FreePage(PN pn)
        {
            if (fpmCommitted[pn])
                fpm.FreePage(pn);
            else
            {
                fpmFreed[pn] = true;
            }
        }

        internal MemoryChunk SlicePaged(SN sn)
        {
            ref var si = ref StreamTable[sn];

            return globalBlock!.SlicePaged(si.PageList.ToArray(), si.ByteCount);
        }

        internal MemoryChunk SlicePaged(PN[] pageList, int size) =>
            globalBlock!.SlicePaged(pageList, size);

        internal void ReplaceStream(SN sn, in StreamInfoBuilder si)
        {
            ref var oldSi = ref StreamTable[sn];

            for (var i = 0; i < oldSi.PageList.Count; i++)
                FreePage(oldSi.PageList[i]);

            StreamTable[sn] = new StreamInfoBuilder
            {
                sn = sn,
                ByteCount = si.ByteCount,
                PageList = si.PageList
            };
        }

        public unsafe void AppendCustomStream(SN sn, IntPtr data, int size)
        {
            ref var si = ref StreamTable[sn];

            if (si.ByteCount == -1)
                si.ByteCount = 0;

            if (customStreams.TryGetValue(sn, out var customStream))
            {
                throw new NotImplementedException();
            }
            else
            {
                var numPages = SI.DivideUp(size, PageSize);

                for (var i = 0; i < numPages; i++)
                    si.PageList.Add(AllocPage());

                var totalData = numPages * PageSize;

                customStream = new CustomStreamBuilder(new Span<byte>((byte*) data, size), totalData);

                customStreams[sn] = customStream;

                si.ByteCount = size;
            }
        }

        internal void SetNewStreamTableLocation(ref BigMsfHdr bigMsfHdr, int streamTableSize, List<PN> pagesOfStreamTablePageList)
        {
            //If we had a previous list of pages describing the location of the stream table's pages, clear that list now
            var previousPagesOfStreamTablePageList = bigMsfHdr.PagesOfStreamTablePageList;

            for (var i = 0; i < previousPagesOfStreamTablePageList.Length; i++)
                FreePage(previousPagesOfStreamTablePageList[i]);

            var siSt = bigMsfHdr.StreamTableSizeInfo;
            siSt.ByteCount = streamTableSize;
            siSt.PageList = 0;

            //Copy siPnList.mpspnpn to bighdr.mpspnpnSt
            bigMsfHdr.SetPagesOfStreamTablePageList(pagesOfStreamTablePageList);
        }

        public unsafe void Commit()
        {
            //We can't open the MMF until we've resized the file. When each stream acquires its pages, we'd like to be returning the
            //actual MMF area that it should be writing into. As such, we will process the streams in two passes: Measure and then Serialize

            #region Measure

            PDB?.Measure();
            DBI?.Measure();

            //Pages are allocated to custom streams when their data is appended, so they've already been measured

            var newSiSt = StreamTable.Measure();

            //Now we create a fake SI for storing the location of the stream table itself
            var streamTableLocationSize = newSiSt.PageList.Count * sizeof(int);

            var newSiStLocation = AllocTempStream(streamTableLocationSize);

            //There's no need to measure the FPM, because its pages will always be less than the maximum number of pages allocated
            //via other means

            #endregion
            #region Serialize

            using var fs = File.Open(FileName, FileMode.Open);

            fs.SetLength(NumPages * PageSize);

            using var mmf = new MemoryMappedFileHolder(fs, MemoryMappedFileAccess.ReadWrite);

            globalBlock = new PDBGlobalMemoryBlock(mmf.Address, (int) mmf.Length, true, PageSize, null!);
            var bigMsfHdr = new BigMsfHdr(new MemoryChunk(globalBlock, 0));
            bigMsfHdr.NumPages = NumPages;
            bigMsfHdr.FpmPageNo = ActiveFpmPage;

            if (PDB?.Changed == true)
                PDB.Serialize();

            if (DBI?.Changed == true)
                DBI.Serialize();

            WriteCustomStreams();

            FinalizeCommit(ref bigMsfHdr, newSiSt, newSiStLocation);

            #endregion

            globalBlock = null;
        }

        private void FinalizeCommit(ref BigMsfHdr bigMsfHdr, in StreamInfoBuilder newSiSt, in StreamInfoBuilder newSiStLocation)
        {
            /* Every time the PDB is committed, it the location of the stream table's pages will change.
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
            StreamTable.Serialize(newSiSt);

            var chunk = SlicePaged(newSiStLocation.PageList.ToArray(), newSiStLocation.ByteCount);
            chunk.PokeSpan<PN>(0, newSiSt.PageList.Count, newSiSt.PageList.ToArray());

            SetNewStreamTableLocation(ref bigMsfHdr, newSiSt.ByteCount, newSiStLocation.PageList);

            fpm.Add(fpmFreed);
            fpm.Serialize();

            //Total number of pages should not have changed. If we needed to grow in order to store the stream table or
            //stream table page list, we should have done that prior to opening the MMF
            Debug.Assert(NumPages == bigMsfHdr.NumPages);

            WriteSplitPages();

            fpmCommitted = new BitArray(fpm.PageMap);

            if (fpmFreed.Length != fpm.PageMap.Length)
                fpmFreed.Length = fpm.PageMap.Length;

            fpmFreed.SetAll(false);

            ActiveFpmPage = ActiveFpmPage == msfParms.Fpm0PageNo ? msfParms.Fpm1PageNo : msfParms.Fpm0PageNo;
        }

        private unsafe void WriteCustomStreams()
        {
            var pageSize = PageSize;

            foreach (var kv in customStreams)
            {
                ref var si = ref StreamTable[kv.Key];

                for (var i = 0; i < si.PageList.Count; i++)
                {
                    var page = si.PageList[i];

                    //In the source, the page start is relative to the location of that page within the block
                    //In the dest, the page start is relative to the actual page within the entire file.
                    var sourcePageStart = i * pageSize;
                    var destPageStart = page * pageSize;

                    var length = pageSize;

                    var previous = page;

                    //Include adjacent pages in the batch
                    for (var j = i + 1; j < si.PageList.Count; j++)
                    {
                        var current = si.PageList[j];

                        if (current == previous + 1)
                        {
                            i++;
                            previous = current;
                            length += pageSize;
                        }
                        else
                            break;
                    }

                    var source = kv.Value.Slice(sourcePageStart, length);
                    var destination = new Span<byte>(globalBlock!.LocalPointer + destPageStart, length);
                    source.CopyTo(destination);
                }
            }
        }

        private unsafe void WriteSplitPages()
        {
            foreach (var kv in globalBlock!.blockCache)
            {
                //Copy any split pages into the MMF. Non-split pages would have written directly to the MMF.
                //Since our globalBlock is temporarily created just while we're serializing, we don't have to worry about
                //having stale PN[] items in the blockCache

                var pageSize = PageSize;

                if (kv.Value.OwnsMemory)
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
                        var destination = new Span<byte>(globalBlock.LocalPointer + destPageStart, length);
                        source.CopyTo(destination);
                    }

                    kv.Value.hasChanges = false;
                }
            }
        }

        public void Dispose()
        {
            foreach (var kv in customStreams)
                kv.Value.Dispose();

            customStreams.Clear();
        }
    }
}
