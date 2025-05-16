using System;
using System.Collections.Generic;
using System.IO;
using ClrDebug.PDB;
using PESpy.PDB;
using PESpy.View;
using SN = PESpy.PDB.SN;

namespace PESpy
{
    /// <summary>
    /// Represents a CodeView Program Database (PDB) file.
    /// </summary>
    public abstract unsafe class PDBFile : IFile, IViewable, IDisposable
    {
        public static PDBFile FromFile(string path, bool writable = false)
        {
            //File.OpenWrite opens with FileMode.OpenOrCreate. The file _must_ already exist if we are opening it with an MMF
            using var fs = writable ? File.Open(path, FileMode.Open, FileAccess.ReadWrite) : File.OpenRead(path);

            //V2 magic is 44 bytes and a BIGMSF_HDR is close to 60
            if (fs.Length < 44)
                throw new BadImageFormatException("File is not large enough to contain a PDB header");

            var mmf = new MemoryMappedFileHolder(fs);

            try
            {
                var magic = new FixedAnsiString(mmf.Address, 32);

                if (magic == BigMsfHdr.BigHdrMagic)
                    return new PDB7File(fs.Name, mmf);

                magic = new FixedAnsiString(mmf.Address, 44);

                if (magic == MsfHdr.HdrMagic)
                    return new PDB2File(fs.Name, mmf);

                if (magic == OHDR.OHdrMagic)
                    return new PDB1File(fs.Name, mmf);

                throw new BadImageFormatException("File did not contain a PDB magic signature");
            }
            catch
            {
                mmf.Close();

                throw;
            }
        }

        public static PDBFile Create(string fileName, int pageSize = 1024) => new PDB7File(fileName, pageSize);

        private bool disposed;

        public PDBFileKind PDBKind { get; }

        /// <inheritdoc/>
        public string? Name { get; private set; }

        /// <inheritdoc/>
        public string? FileName { get; private set; }

        /// <inheritdoc/>
        public FileKind Kind => FileKind.PDB;

        #region MSF

        //Fields that are specific to MSF (PDB2/PDB7) files. PDB1 does not use MSF

        protected FPM fpm0;

        /// <summary>
        /// Gets the first Free Page Map (FPM 0) which describes which pages are free vs in use in the PDB.<para/>
        /// In a <see cref="PDB7File"/> this is the active FPM if <see cref="BigMsfHdr.FpmPageNo"/> == 1.<para/>
        /// In a <see cref="PDB2File"/> the primary and secondary page numbers depend on the page size used in the PDB.<para/>
        /// This value is not valid if this is a <see cref="PDB1File"/>.
        /// </summary>
        public ref readonly FPM FPM0 => ref fpm0;

        protected FPM fpm1;

        /// <summary>
        /// Gets the second Free Page Map (FPM 1) which describes which pages are free vs in use in the PDB.<para/>
        /// This is the active FPM if <see cref="BigMsfHdr.FpmPageNo"/> == 2.<para/>
        /// In a <see cref="PDB2File"/> the primary and secondary page numbers depend on the page size used in the PDB.<para/>
        /// This value is not valid if this is a <see cref="PDB1File"/>.
        /// </summary>
        public ref readonly FPM FPM1 => ref fpm1;

        /// <summary>
        /// Gets the active Free Page Map, based on the FPM listed in <see cref="MsfHdr.FpmPageNo"/> or <see cref="BigMsfHdr.FpmPageNo"/>.<para/>
        /// This value is not valid if this is a <see cref="PDB1File"/>.
        /// </summary>
        public ref readonly FPM ActiveFPM
        {
            get
            {
                if (ActiveFpmPageNo == 1)
                    return ref fpm0;

                return ref fpm1;
            }
        }

        /// <summary>
        /// Gets the number of pages contained in the PDB.<para/>
        /// This value is not valid if this is a <see cref="PDB1File"/>.
        /// </summary>
        protected internal abstract int NumPages { get; }

        /// <summary>
        /// Gets the size of each page in the PDB.<para/>
        /// This value is not valid if this is a <see cref="PDB1File"/>.
        /// </summary>
        protected internal abstract int PageSize { get; }

        /// <summary>
        /// Gets the number of the page that contains the active FPM.<para/>
        /// This value is not valid if this is a <see cref="PDB1File"/>.
        /// </summary>
        protected internal abstract int ActiveFpmPageNo { get; }

        /// <summary>
        /// Gets the stream table that describes which pages belong to which streams in the PDB.<para/>
        /// This value is not valid if this is a <see cref="PDB1File"/>.
        /// </summary>
        public IStreamTable StreamTable { get; protected set; }

        #region Streams
        #region snSt (0)

        //Whenever the PDB is committed, a copy of the previous stream table pointed to by the BIGMSF_HDR is copied into Stream 0 (snSt)
        private IStreamTable? previousStreamTable;

        //This value is not available when writing, as snSt will return the in-memory version of the stream table,
        //which will match the current version of the stream table (which will upset the view since they haev the same offset).
        //Only the on-disk snSt will be the previous stream table
        public IStreamTable? PreviousStreamTable
        {
            get
            {
                if (previousStreamTable == null && !globalBlock.writable)
                {
                    if (TryGetStreamChunk(SN.ST, out var chunk))
                        previousStreamTable = CreateStreamTable(chunk, PageSize);
                }

                return previousStreamTable;
            }
        }

        internal abstract IStreamTable CreateStreamTable(in MemoryChunk chunk, int pageSize);

        #endregion
        #region snPDB (1)

        private MsfStream.PDB? pdb;

        /// <summary>
        /// Provides access to the contents of the snPDB (1) stream which provides basic header information about the PDB
        /// as well as the stream name table.<para/>
        /// This value is not valid if this is a <see cref="PDB1File"/>.
        /// </summary>
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

        /// <summary>
        /// Provides access to the contents of the snTpi (2) stream which contains records for types used in the PDB.<para/>
        /// This value is not valid if this is a <see cref="PDB1File"/>.
        /// </summary>
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

        /// <summary>
        /// Provides access to the contents of the snDbi (3) stream which contains modules, symbols, section contributions, and most other types
        /// of symbolic information found in the PDB.<para/>
        /// This value is not valid if this is a <see cref="PDB1File"/>.
        /// </summary>
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
        #region snIpi (4)

        private MsfStream.TPI? ipi;

        /// <summary>
        /// Provides access to the contents of the snIpi (4) stream which contains records for ID types used in the PDB.<para/>
        /// This value is not valid if this is a <see cref="PDB1File"/>.
        /// </summary>
        public MsfStream.TPI? IPI //dumppdb.cpp uses the header "IDs" for IPI, which tells us what the I stands for
        {
            get
            {
                if (ipi == null && PDB?.HasIPI == true)
                {
                    if (TryGetStreamChunk(SN.IPI, out var chunk))
                        ipi = new MsfStream.TPI(chunk);
                }

                return ipi;
            }
        }

        #endregion

        public TypType GetTypTypeFromIndex(CV_typ_t typeIndex)
        {
            if (typeIndex.CV_IS_PRIMITIVE())
                throw new ArgumentException($"Cannot resolve TypType for {nameof(CV_typ_t)} {typeIndex}: type is a primitive type");

            var tpi = TPI;

            if (tpi == null)
                throw new InvalidOperationException("Attempted to resolve a type index when no TPI stream was present");

            //In impv70+ there is a table in tpihash that we can use to do a binary search on to get the offset of the type index

            if (TryGetOffsetFromTpiHash(typeIndex, out var offset))
                return tpi.Types.GetTypeFromOffset(offset);

            throw new NotImplementedException("Retrieving a TypType when the type index is not in the TI to Offset list is not implemented");
        }

        private NativeSpan<TI_OFF> tiToOffList;
        private bool hasTriedTiToOffList;

        private bool TryGetOffsetFromTpiHash(CV_typ_t typeIndex, out int offset)
        {
            //It seems that not all type indices in a PDB may be in this list
            offset = default;

            if (tiToOffList.Length == 0)
            {
                if (hasTriedTiToOffList)
                    return false;

                //Caller should have validated we have a TPI
                if (TPI!.Hdr is HDR h)
                {
                    var tpiHash = h.tpihash;

                    if (TryGetStreamChunk(tpiHash.sn, out var chunk))
                    {
                        var val = chunk.Slice(tpiHash.offcbTiOff.off);

                        hasTriedTiToOffList = true;
                        tiToOffList = val.PeekNativeSpan<TI_OFF>(0, tpiHash.offcbTiOff.cb / 8);
                    }
                }

                hasTriedTiToOffList = true;
            }

            var localList = tiToOffList;

            //Binary search for offset
            int low = 0;
            int high = localList.Length - 1;

            while (low <= high)
            {
                int mid = low + (high - low) / 2;

                var item = localList[mid];

                if (item.ti == typeIndex)
                {
                    offset = item.off;
                    return true;
                }
                else if (item.ti < typeIndex)
                    low = mid + 1;
                else
                    high = mid - 1;
            }

            return false;
        }

        #region /names

        private NMT? nameMap;

        public NMT? NameMap
        {
            get
            {
                /* Note that there a confusing illusion that can occur with /names (and likely other data) when looking
                 * at a view of the PDB. Consider the following page layout:
                 * 100: NewDbiHdr, C:\Windows\sys
                 * 102: /names(2) stem32\notepad.exe
                 * 103: /names(1) C:\Windows\sys
                 * 
                 * This creates the illusion that /names is starting on page 100, right after the NewDbiHdr. This is not the case.
                 * As you can see, the actual start of the string has been written in /names(1) on page 103. Page 100 previously
                 * was being used to store /names(1), but got repurposed to store NewDbiHdr instead */

                if (nameMap == null && TryGetStreamChunk("/names", out var chunk))
                    nameMap = new NMT(chunk);

                return nameMap;
            }
        }

        #endregion
        #region Globals

        private MsfStream.GSI? gsi;

        public MsfStream.GSI? GSI
        {
            get
            {
                if (gsi == null)
                {
                    var dbi = DBI;

                    if (dbi != null)
                    {
                        if (TryGetStreamChunk(dbi.DbiHdr.snGSSyms, out var chunk))
                            gsi = new MsfStream.GSI(chunk);
                    }
                }

                return gsi;
            }
        }

        #endregion
        #region Publics

        private MsfStream.PSGSI? psgsi;

        public MsfStream.PSGSI? PSGSI
        {
            get
            {
                if (psgsi == null)
                {
                    var dbi = DBI;

                    if (dbi != null)
                    {
                        if (TryGetStreamChunk(dbi.DbiHdr.snPSSyms, out var chunk))
                            psgsi = new MsfStream.PSGSI(chunk);
                    }
                }

                return psgsi;
            }
        }

        #endregion
        #endregion
        #endregion

        private MemoryMappedFileHolder mmf;
        internal PDBGlobalMemoryBlock globalBlock;

        //Open an existing file
        internal PDBFile(string fileName, in MemoryMappedFileHolder mmf, PDBFileKind pdbKind)
        {
            this.mmf = mmf;
            PDBKind = pdbKind;
            StreamTable = null!;

            FileName = fileName;
            Name = Path.GetFileName(fileName);

            globalBlock = new PDBGlobalMemoryBlock(mmf.Address, (int) mmf.Length, mmf.Writable, ownsMemory: false, this);

            //In PDB2 and PDB7 this will read the MSF Headers. In PDB1 it will read the whole file (which just contains type information)
            ReadHeaders();
        }

        //Create a new file
        internal PDBFile(string fileName, PDBFileKind kind)
        {
            FileName = fileName;
            Name = Path.GetFileName(fileName);

            PDBKind = kind;
            globalBlock = null!;
            StreamTable = null!;
        }

        ~PDBFile()
        {
            Dispose(false);
        }

        protected abstract void ReadHeaders();

        internal bool TryGetStreamChunk(SN sn, out MemoryChunk chunk)
        {
            if (sn == SN.Nil || StreamTable == null)
            {
                chunk = default;
                return false;
            }

            if (StreamTable.StreamPages.Count > sn)
            {
                var si = StreamTable.StreamInfos[sn];

                if (si.PageList.Count > 0)
                {
                    chunk = globalBlock.SlicePaged(si);
                    return true;
                }
            }

            chunk = default;
            return false;
        }

        internal bool TryGetStreamChunk(string name, out MemoryChunk chunk)
        {
            chunk = default;

            if (StreamTable == null)
                return false;

            var pdb = PDB;

            if (pdb == null)
                return false;

            if (pdb.StreamNameTable.NameToStreamNumberMap.TryGetValue(name, out var sn))
                return TryGetStreamChunk(sn, out chunk);

            return false;
        }

        //Enumerates symbols from all symbol sources
        public IEnumerable<SymType> EnumerateSymbols()
        {
            var dbi = DBI;

            if (dbi == null)
                yield break;

            var symbols = dbi.Symbols;

            if (symbols != null)
            {
                //It seems that publics can be located in symrecs as well
                foreach (var item in symbols)
                    yield return item;
            }

            var modules = dbi.Modules;

            if (modules != null)
            {
                foreach (var module in modules)
                {
                    var moduleSymbols = module.Symbols;

                    if (moduleSymbols != null)
                    {
                        foreach (var item in moduleSymbols.Symbols)
                            yield return item;
                    }
                }
            }

            var globals = GSI;

            if (globals != null)
            {
                foreach (var item in globals.Symbols)
                    yield return item;
            }

            var publics = PSGSI;

            if (publics != null)
            {
                foreach (var item in publics.Symbols)
                    yield return item;
            }
        }

        void IViewable.WriteView(ViewWriter writer) => WriteView(writer);

        protected abstract void WriteView(ViewWriter writer);

        protected void WriteMsfStreamViews(ViewWriter writer)
        {
            writer.WriteGlobal(StreamTable);

            writer.WriteGlobal(PreviousStreamTable); //snSt
            writer.WriteGlobal(PDB); //snPDB
            writer.WriteGlobal(TPI); //snTpi
            writer.WriteGlobal(DBI); //snDbi

            //IPI will return null if its not supported
            writer.WriteGlobal(IPI); //snIpi

            writer.WriteGlobal(GSI);
            writer.WriteGlobal(PSGSI);

            writer.WriteGlobal(NameMap);

            //Any Free pages are automatically detected during merging
        }

        public FileView GetView()
        {
            byte* address;
            int length;

            if (globalBlock.writable)
            {
                //Resize the global block and consolidate all pages into it
                globalBlock.ConsolidatePages();

                address = globalBlock.LocalPointer;
                length = globalBlock.Length;
            }
            else
            {
                address = mmf.Address;
                length = (int) mmf.Length;
            }

            var writer = new PDBViewWriter(this, address, length);
            ((IViewable) this).WriteView(writer);

            return (FileView) writer.Finalize();
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
                GC.SuppressFinalize(this);

            globalBlock.Dispose();
            mmf.Close();

            disposed = true;
        }
    }
}
