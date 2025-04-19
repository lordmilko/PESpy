using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents a CodeView Program Database (PDB) file.
    /// </summary>
    public unsafe class PDBFile : IViewable, IDisposable
    {
        public static PDBFile FromFile(string path)
        {
            using var fs = File.OpenRead(path);

            return new PDBFile(fs);
        }

        private MemoryMappedFile mmf;
        private MemoryMappedViewAccessor mma;
        private byte* baseAddress;
        private long length;

        private bool disposed;

        private BigMsfHdr msfHeader;

        /// <summary>
        /// Gets the MSF Header that provides high level information about the structure and layout of this PDB File.
        /// </summary>
        public ref readonly BigMsfHdr MsfHeader => ref msfHeader;

        private FPM fpm0;

        /// <summary>
        /// Gets the first Free Page Map (FPM 0) which describes which pages are free vs in use in the PDB.<para/>
        /// This is the active FPM if <see cref="BigMsfHdr.FpmPageNo"/> == 1.
        /// </summary>
        public ref readonly FPM FPM0 => ref fpm0;

        private FPM fpm1;

        /// <summary>
        /// Gets the second Free Page Map (FPM 1) which describes which pages are free vs in use in the PDB.<para/>
        /// This is the active FPM if <see cref="BigMsfHdr.FpmPageNo"/> == 2.
        /// </summary>
        public ref readonly FPM FPM1 => ref fpm1;

        /// <summary>
        /// Gets the active Free Page Map, based on the FPM listed in <see cref="BigMsfHdr.FpmPageNo"/>.
        /// </summary>
        public ref readonly FPM ActiveFPM
        {
            get
            {
                if (msfHeader.FpmPageNo == 1)
                    return ref fpm0;

                return ref fpm1;
            }
        }

        private SI streamTableLocation;

        /// <summary>
        /// Gets the pages that comprise the stream table that describes all of the streams that exist in the PDB
        /// </summary>
        public ref readonly SI StreamTableLocation => ref streamTableLocation;

        public StreamTable StreamTable { get; private set; }

        #region Streams
        #region snSt (0)

        //Whenever the PDB is committed, a copy of the previous stream table pointed to by the BIGMSF_HDR is copied into Stream 0 (snSt)
        private StreamTable? previousStreamTable;

        public StreamTable? PreviousStreamTable
        {
            get
            {
                if (previousStreamTable == null)
                {
                    if (TryGetStreamChunk(SN.ST, out var chunk))
                        previousStreamTable = new StreamTable(chunk, msfHeader.PageSize);
                }

                return previousStreamTable;
            }
        }

        #endregion
        #region snPDB (1)

        private MsfStream.PDB? pdb;

        public MsfStream.PDB? PDB
        {
            get
            {
                if (pdb == null)
                {
                    if (TryGetStreamChunk(SN.PDB, out var chunk))
                        pdb = new MsfStream.PDB(chunk);
                }

                return pdb;
            }
        }

        #endregion
        #region snTpi (2)

        private MsfStream.TPI? tpi;

        public MsfStream.TPI? TPI
        {
            get
            {
                if (tpi == null)
                {
                    if (TryGetStreamChunk(SN.TPI, out var chunk))
                        tpi = new MsfStream.TPI(chunk);
                }

                return tpi;
            }
        }

        #endregion
        #region snDbi (3)

        private MsfStream.DBI? dbi;

        public MsfStream.DBI? DBI
        {
            get
            {
                if (dbi == null)
                {
                    if (TryGetStreamChunk(SN.DBI, out var chunk))
                        dbi = new MsfStream.DBI(chunk);
                }

                return dbi;
            }
        }

        #endregion
        #endregion

        private PDBGlobalMemoryBlock globalBlock;

        private PDBFile(FileStream fs)
        {
            //Initialize the MMF

            StreamTable = null!;

            mmf = MemoryMappedFile.CreateFromFile(fs, null, 0, MemoryMappedFileAccess.Read, HandleInheritability.None, false);
            mma = mmf.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);

            RuntimeHelpers.PrepareConstrainedRegions();

            try
            {
                //Empty; needed to make constrained region work
            }
            finally
            {
                //While MMA does have some helper methods on it that can be used to read certain value types,
                //it acquires/releases the pointer after each value read, inside of a try/finally block, which I feel
                //adds a bit of overhead
                mma.SafeMemoryMappedViewHandle.AcquirePointer(ref baseAddress);
                length = (long) mma.SafeMemoryMappedViewHandle.ByteLength;
            }

            globalBlock = new PDBGlobalMemoryBlock(baseAddress, (int) length, this);

            //Read the PDB Headers
            ReadMsfHeaders();

#if DEBUG
            _ = PreviousStreamTable;
            _ = PDB;
            _ = TPI;
            _ = DBI;
#endif
        }

        private void ReadMsfHeaders()
        {
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
            fpm0 = new FPM(1, msfHeader.PageSize, msfHeader.NumPages, globalBlock);
            fpm1 = new FPM(2, msfHeader.PageSize, msfHeader.NumPages, globalBlock);

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
             * "cb" and blockMapAddr members. blockMapAddr points to the list of pages that we should look at to find the stream directory. "cb" describes the total amount of size that all of those
             * pages occupy. So if "cb" is 5000, we need two pages to store the stream directory, and blockMapAddr points to the indices of those two pages */
            streamTableLocation = new SI(
                globalChunk.Slice(msfHeader.PageOfStreamTablePageList * msfHeader.PageSize),
                msfHeader.StreamTableSizeInfo.ByteCount,
                msfHeader.PageSize
            ); //Note: not sure if the byte count could exceed a single page?

            //Now read the Stream Table from all of those pages
            StreamTable = new StreamTable(
                globalBlock.SlicePaged(StreamTableLocation),
                msfHeader.PageSize
            );

            //We have now read the minimum amount of info that must exist in a valid PDB file. All other sections like PDB, DBI, etc are completely optional
        }

        internal bool TryGetStreamChunk(SN sn, out MemoryChunk chunk)
        {
            if (sn == SN.Nil)
            {
                chunk = default;
                return false;
            }

            if (StreamTable.StreamBlocks.Length > sn)
            {
                ref var si = ref StreamTable.StreamInfos[sn];

                if (si.PageList.Length > 0)
                {
                    chunk = globalBlock.SlicePaged(si);
                    return true;
                }
            }

            chunk = default;
            return false;
        }
        public PdbFileView GetView()
        {
#if NEW_PDB
            //todo: temp using reader+stream while we're still in transition
            var writer = new PdbViewWriter(this, new StreamFileReader(new MMFStream(baseAddress), new object()));
            ((IViewable) this).WriteView(writer);

            return (PdbFileView) writer.Finalize();
#else
            throw new NotImplementedException();
#endif
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            writer.WriteGlobal(MsfHeader);

            //We do not write the FPM; these raw bytes are automatically collected during merging
            //under the FPM0 and FPM1 regions

            writer.WriteGlobal(StreamTableLocation);
            writer.WriteGlobal(StreamTable);

            writer.WriteGlobal(PreviousStreamTable); //snSt
            writer.WriteGlobal(PDB); //snPDB
            writer.WriteGlobal(DBI); //snDbi

            //Any Free pages are automatically detected during merging
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
                GC.SuppressFinalize(this);

            RuntimeHelpers.PrepareConstrainedRegions();

            if (baseAddress != (byte*) 0)
            {
                RuntimeHelpers.PrepareConstrainedRegions();

                try
                {
                    //Empty
                }
                finally
                {
                    mma.SafeMemoryMappedViewHandle.ReleasePointer();
                    baseAddress = (byte*) 0;
                }
            }

            mma.Dispose();
            mmf.Dispose();

            mma = null;
            mmf = null;

            disposed = true;
        }
    }
}
