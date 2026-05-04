using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;
using System.Threading;
using ClrDebug;
using ClrDebug.PDB;
using PESpy.PDB;
using PESpy.PDB.DIA;
using PESpy.View;
using PESpy.View.Builder;
using SN = PESpy.PDB.SN;

namespace PESpy
{
    internal abstract class PDBFileDebugView
    {
        private PDBFile pdbFile;

        public PDBFileKind PDBKind => pdbFile.PDBKind;

        public string Name => pdbFile.Name;

        public string FileName => pdbFile.FileName;

        public FileKind Kind => pdbFile.Kind;

        public long Length => pdbFile.Length;

        public FPM FPM0 => pdbFile.FPM0;

        public FPM FPM1 => pdbFile.FPM1;

        public FPM ActiveFPM => pdbFile.ActiveFPM;

        public int NumPages => pdbFile.NumPages;

        public int PageSize => pdbFile.PageSize;

        public int ActiveFpmPageNo => pdbFile.ActiveFpmPageNo;

        public IStreamTable StreamTable => pdbFile.StreamTable;

        public IStreamTable? PreviousStreamTable => pdbFile.PreviousStreamTable;

        public MsfStream.PDB? PDB => pdbFile.PDB;

        public MsfStream.TPI? TPI => pdbFile.TPI;

        public MsfStream.DBI? DBI => pdbFile.DBI;

        public MsfStream.TPI? IPI => pdbFile.IPI;

        public NMT? NameMap => pdbFile.NameMap;

        public MsfStream.SrcHeaders? SrcHeaders => pdbFile.SrcHeaders;

        public FixedAnsiString SrcSrv => pdbFile.SrcSrv;

        public SourceLinkList SourceLink => pdbFile.SourceLink;

        public MsfStream.GSI? GSI => pdbFile.GSI;

        public MsfStream.PSGSI? PSGSI => pdbFile.PSGSI;

        internal PDBFileDebugView(PDBFile pdbFile)
        {
            this.pdbFile = pdbFile;
        }
    }

    /// <summary>
    /// Represents a CodeView Program Database (PDB) file.
    /// </summary>
    public abstract unsafe class PDBFile : IFile, IViewable, ICodeViewAccessor, IDisposable
    {
        /// <summary>
        /// Reads a <see cref="PDBFile"/> from a file on disk.
        /// </summary>
        /// <param name="path">The path to the file to read.</param>
        /// <param name="writable">Whether the file should be opened as writable. Changes can be persisted by calling <see cref="Save"/></param>
        /// <returns>A <see cref="PDBFile"/> that provides access to the contents of the specified file.</returns>
        public static PDBFile FromFile(string path, bool writable = false)
        {
            //File.OpenWrite opens with FileMode.OpenOrCreate. The file _must_ already exist if we are opening it with an MMF
            using var fs = writable ? File.Open(path, FileMode.Open, FileAccess.ReadWrite) : File.OpenRead(path);

            return FromStream(fs);
        }

        public static PDBFile FromStream(FileStream fileStream)
        {
            //V2 magic is 44 bytes and a BIGMSF_HDR is close to 60
            if (fileStream.Length < 44)
                throw new BadImageFormatException("File is not large enough to contain a PDB header");

            var mmf = new MemoryMappedFileHolder(fileStream);

            try
            {
                var magic = new FixedAnsiString(mmf.Address, 32);

                if (magic == BigMsfHdr.BigHdrMagic)
                    return new PDB7File(fileStream.Name, mmf);

                magic = new FixedAnsiString(mmf.Address, 44);

                if (magic == MsfHdr.HdrMagic)
                    return new PDB2File(fileStream.Name, mmf);

                if (magic == OHDR.OHdrMagic)
                    return new PDB1File(fileStream.Name, mmf);

                if (*(uint*) mmf.Address == StorageSignature.STORAGE_MAGIC_SIG)
                    throw new InvalidOperationException("Portable PDB files cannot be opened using this method. Use PortablePDBFile.FromFile() instead");

                if (Detector.TryDetectFile(fileStream.Name, mmf, mmf.Length, out var kind, out _, out _))
                    throw new InvalidOperationException($"Expected a PDB file however a {kind} was provided");

                throw new BadImageFormatException("File did not contain a PDB magic signature");
            }
            catch
            {
                mmf.Dispose();

                throw;
            }
        }

        /// <summary>
        /// Locates a file on the symbol server and opens it as a <see cref="PDBFile"/>.<para/>
        /// If a <see cref="SymStoreKey"/> of type <see cref="SymStoreKeyKind.PE"/> is specified, this method
        /// will attempt to locate the PDB that is associated with that file.
        /// </summary>
        /// <param name="symStoreKey">The <see cref="SymStoreKey"/> describing the file that should be located and opened.</param>
        /// <returns>A <see cref="PDBFile"/> that provides access to the contents of the specified file.</returns>
        /// <exception cref="ArgumentException">The specified <see cref="SymStoreKey"/> cannot be opened as a <see cref="PDBFile"/>.</exception>
        public static PDBFile FromKey(SymStoreKey symStoreKey)
        {
            switch (symStoreKey.Kind)
            {
                case SymStoreKeyKind.PDB:
                    var path = Locator.Locate(symStoreKey);

                    return FromFile(path);

                case SymStoreKeyKind.PE:
                    var modulePath = Locator.Locate(symStoreKey);
                    var pdbPath = Locator.LocatePDB(modulePath);

                    return FromFile(pdbPath);

                default:
                    throw new ArgumentException($"{nameof(SymStoreKey)} '{symStoreKey}' of type '{symStoreKey.Kind}' cannot be opened as a {nameof(PDBFile)}");
            }
        }

        private bool disposed;

        public PDBFileKind PDBKind { get; }

        /// <inheritdoc/>
        public string Name { get; private set; }

        /// <inheritdoc/>
        public string FileName { get; private set; }

        /// <inheritdoc/>
        public FileKind Kind => FileKind.PDB;

        public long Length => globalBlock.Length;

        /* Note that we explicitly do not have a SymStoreKey property. You may think that you can take
         * The GUID and Age from the PDBStream70 and synthesize the original path to this file on the symbol
         * server, however this is not the case! Files uploaded to the Microsoft Symbol Store regularly have
         * wildly different ages from what you would think their ages should be in their path. Even assuming that
         * the age will usually be 1 is wrong. A URL can indicate the age was 7, but the PDBStream70 says the age
         * was 9. */

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
                    //The previous stream table can sometimes contain junk data when it's been partially overwritten, which may lead it to have
                    //a very high "number of streams" that takes it out of bounds
                    if (TryGetStreamChunk(SN.ST, out var chunk))
                    {
                        //Do an initial sanity check: check whether skipping over NumStreams + StreamSizes would take us out of bounds
                        var numStreams = chunk.PeekInt32(0);

                        int offset;

                        if (this is PDB7File)
                            offset = sizeof(int) + (numStreams * sizeof(int));
                        else
                            offset = sizeof(int) + (numStreams * SI_PERSIST.StructSize);

                        if (offset < 0 || offset > chunk.Remaining)
                            return null; //The stream contains garbage

                        previousStreamTable = CreateStreamTable(chunk, PageSize);
                    }
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

        /* tpihash.sn comes from bufMapHash
         *
         * TPI1::AddNewTypeRecord appends items to it. You pass in some bytes. A REC is created around the bytes,
         * and then TPI1::hashPrec hashes the PREC. For versions below impv80, a 16-bit HASH is added. Otherwise,
         * it's a 32-bit LHASH. A TI -> OFF tuple is then recorded for every 8kb of data written
         * to the main type record buffer
         */

        public virtual TypType GetTypTypeFromIndex(CV_typ_t typeIndex) => GetTypTypeFromIndex(typeIndex, TPI, "TPI");

        public virtual TypType GetTypTypeFromIndex(CV_ItemId typeIndex) => GetTypTypeFromIndex((int) (uint) typeIndex, IPI, "IPI");

        private TypType GetTypTypeFromIndex(CV_typ_t typeIndex, MsfStream.TPI? stream, string streamName)
        {
            if (typeIndex.CV_IS_PRIMITIVE())
                throw new ArgumentException($"Cannot resolve TypType for {nameof(CV_typ_t)} {typeIndex}: type is a primitive type");

            if (stream == null)
                throw new InvalidOperationException($"Attempted to resolve a type index when no {streamName} stream was present");

            return stream.GetTypTypeFromIndex(typeIndex);
        }

        public virtual bool TryGetTypTypeFromIndex(CV_typ_t typeIndex, out TypType typType)
        {
            typType = default;

            if (typeIndex.CV_IS_PRIMITIVE())
                return false;

            var tpi = TPI;

            if (tpi == null)
                return false;

            return tpi.TryGetTypTypeFromIndex(typeIndex, out typType);
        }

        public virtual bool TryGetTypTypeFromIndex(CV_ItemId typeIndex, out TypType typType)
        {
            typType = default;

            var ipi = IPI;

            if (ipi == null)
                return false;

            return ipi.TryGetTypTypeFromIndex((CV_typ_t) (int) (uint) typeIndex, out typType);
        }

        #region /names

        private NMT? nameMap;

        /// <summary>
        /// Gets the contents of the /names stream which contains the name map.
        /// </summary>
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
        #region /src/headerblock

        //Gets the stream that contains the SrcHeaderBlock and the map of SrcHeaderOut
        //entries
        private MsfStream.SrcHeaders? srcHeaders;

        public MsfStream.SrcHeaders? SrcHeaders
        {
            get
            {
                if (srcHeaders == null)
                {
                    if (TryGetStreamChunk("/src/headerblock", out var chunk))
                        srcHeaders = new MsfStream.SrcHeaders(chunk, this);
                }

                return srcHeaders;
            }
        }

        #endregion
        #region srcsrv

        public FixedAnsiString SrcSrv
        {
            get
            {
                if (TryGetStreamChunk("srcsrv", out var chunk))
                    return chunk.PeekAnsiFixedLength(0, (int) chunk.Remaining);

                return default;
            }
        }

        #endregion
        #region sourcelink

        public SourceLinkList SourceLink
        {
            get
            {
                //https://github.com/microsoft/perfview/blob/main/src/TraceEvent/Symbols/NativeSymbolModule.cs#L1144
                //says that sourcelink data is either stored as a single "sourcelink" stream, or is stored as multiple
                //sourcelink$n streams, where n starts from 1. Note that even if there's more than 1 stream, the last
                //stream's length may be 0

                if (TryGetStreamInfo("sourcelink", out var sn, out var si) && si.ByteCount > 0)
                {
                    return new SourceLinkList(count: -1, this);
                }
                else
                {
                    var count = 0;

                    if (TryGetStreamInfo("sourcelink$1", out sn, out si) && si.ByteCount > 0)
                    {
                        count++;

                        while (true)
                        {
                            var name = count switch
                            {
                                1 => "sourcelink$2", //Having two streams is the common case, so avoid allocating here
                                _ => $"sourcelink${count + 1}"
                            };

                            if (TryGetStreamInfo(name, out sn, out si) && si.ByteCount > 0)
                                count++;
                            else
                                break;
                        }
                    }

                    return new SourceLinkList(count, this);
                }
            }
        }

        #endregion
        #region Globals

        private MsfStream.GSI? gsi;

        /// <summary>
        /// Gets the globals stream pointed to by the DBI stream.
        /// </summary>
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
                            gsi = new MsfStream.GSI(chunk, (int) chunk.Remaining);
                    }
                }

                return gsi;
            }
        }

        #endregion
        #region Publics

        private MsfStream.PSGSI? psgsi;

        /// <summary>
        /// Gets the publics stream pointed to by the DBI stream.
        /// </summary>
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
        private ImageSectionHeader[]? fallbackSectionHeaders;
        private ISymbolAccessor symbolAccessor; //Separate type so you can dispose the accessor without accidentally disposing the main file

        private readonly object c13SymbolMemoryLock = new object();
        private readonly HashSet<int> c13RegisteredSymbolMemory = new HashSet<int>();

        internal readonly PDBFileSymCache _symCache;

        //Open an existing file
        internal PDBFile(string fileName, in MemoryMappedFileHolder mmf, PDBFileKind pdbKind, string name)
        {
            this.mmf = mmf;
            PDBKind = pdbKind;
            StreamTable = null!;

            FileName = fileName;
            Name = name ?? Path.GetFileName(fileName);

            globalBlock = new PDBGlobalMemoryBlock(mmf.Address, mmf.Length, mmf.Writable, 0, this);
            _symCache = new PDBFileSymCache(this);

            try
            {
                //In PDB2 and PDB7 this will read the MSF Headers. In PDB1 it will read the whole file (which just contains type information)
                ReadHeaders();
            }
            catch
            {
                Dispose();
                throw;
            }
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

        public void SetFallbackSectionHeaders(ImageSectionHeader[] sectionHeaders) =>
            fallbackSectionHeaders = sectionHeaders;

        ~PDBFile()
        {
            Dispose(false);
        }

        //This does not perform a "Commit". We are just directly hacking the PDB
        public unsafe void Save()
        {
            //File.OpenWrite opens the file with FileAccess.Write, but we need ReadWrite to memory map it
            using var fs = File.Open(FileName, FileMode.OpenOrCreate, FileAccess.ReadWrite);

            //IIRC microsoft-pdb doesn't need to worry about zeroing stuff, because it expands the file on disk
            //and only writes data that has been modified in memory to it

            var pageSize = PageSize;

            fs.SetLength(NumPages * pageSize);

            using var mmf = new MemoryMappedFileHolder(fs, MemoryMappedFileAccess.ReadWrite);

            /* Now we need to copy a bunch of stuff:
             * 1. The first 3 pages of the global block (which may contain more than 3 pages
             *    if we consolidated everything into it for the purposes of showing a view
             * 2. Every page of every block
             */

            var headerLength = pageSize * 3;
            var dest = new Span<byte>(mmf.Address, headerLength);

            //Copy the Master Index, FPM 0 and FPM 1
            new Span<byte>(globalBlock.LocalPointer, headerLength).CopyTo(dest);

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

        protected abstract void ReadHeaders();

        public bool TryGetStreamData(string name, out NativeSpan<byte> span)
        {
            if (TryGetStreamChunk(name, out var chunk))
            {
                span = new NativeSpan<byte>(chunk.Pointer, checked((int) chunk.Remaining));
                return true;
            }

            span = default;
            return false;
        }

        internal bool TryGetStreamChunk(SN sn, out MemoryChunk chunk)
        {
            if (sn == SN.Nil || StreamTable == null)
            {
                chunk = default;
                return false;
            }

            if (StreamTable.StreamPages.Length > sn)
            {
                var si = StreamTable.StreamInfos[sn];

                if (si.PageList.Length > 0)
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

            var streamNameTable = PDB?.StreamNameTable;

            if (streamNameTable == null)
                return false;

            if (streamNameTable.NameToStreamNumberMap.TryGetValue(name, out var sn))
                return TryGetStreamChunk(sn, out chunk);

            return false;
        }

        public bool TryGetStreamInfo(SN sn, out SI si)
        {
            if (sn == SN.Nil || StreamTable == null)
            {
                si = default;
                return false;
            }

            if (StreamTable.StreamPages.Length > sn)
            {
                si = StreamTable.StreamInfos[sn];
                return true;
            }

            si = default;
            return false;
        }

        public bool TryGetStreamInfo(string name, out SN sn, out SI si)
        {
            sn = default;
            si = default;

            if (StreamTable == null)
                return false;

            var streamNameTable = PDB?.StreamNameTable;

            if (streamNameTable == null)
                return false;

            if (streamNameTable.NameToStreamNumberMap.TryGetValue(name, out sn))
            {
                if (sn != SN.Nil && sn < StreamTable.StreamInfos.Length)
                {
                    si = StreamTable.StreamInfos[sn];
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Enumerates symbols from all symbol sources
        /// </summary>
        /// <param name="topLevel">If <see langword="true"/>, excludes symbols nested inside top level block symbols.</param>
        /// <returns>An enumeration of all symbols.</returns>
        public IEnumerable<SymType> EnumerateSymbols(bool topLevel = true)
        {
            var dbi = DBI;

            if (dbi == null)
                yield break;

            /* Publics and Globals get their symbols from snSymRecs, so on that basis you might think to "get all non-module
             * symbols you should iterate snSymRecs". However, it appears that this is not the case; it seems that snSymRecs
             * can also contain "junk" symbols, such as REFSYM2 items that point to a symbol that doesn't exist. The REFSYM2
             * that you find isn't the _real_ REFSYM2 that you should be using, it's an older stale one. The real REFSYM2
             * is pointed to by globals */
            var gsiSymbols = GSI?.Symbols;

            if (gsiSymbols != null)
            {
                foreach (var item in gsiSymbols)
                    yield return item;
            }

            var psgsiSymbols = PSGSI?.Symbols;

            if (psgsiSymbols != null)
            {
                foreach (var item in psgsiSymbols)
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
                        if (!topLevel)
                        {
                            foreach (var item in moduleSymbols.List)
                                yield return item;
                        }
                        else
                        {
                            foreach (var item in moduleSymbols.List.GetTopLevel())
                                yield return item;
                        }
                    }
                }
            }

            //The symbols in GSI and PSGSI literally come from snSymRecs; they simply target specific items

            var thunkEntries = PSGSI?.ThunkEntries;

            if (thunkEntries != null && thunkEntries.Length > 0)
            {
                foreach (var thunkEntry in thunkEntries)
                {
                    if (thunkEntry.Thunk != default)
                        yield return thunkEntry.Thunk;
                }
            }
        }

        public ImageSectionHeader[]? GetSectionHeaders() => DBI?.SectionHdr ?? fallbackSectionHeaders;

        /// <summary>
        /// Attempts to get the symbol associated with the given RVA.<para/>
        /// If the PDB contains OmapToSrc information, this method will assume that <paramref name="rva"/>
        /// refers to a physical location in the <see cref="PEFile"/> in the post-OMAP address space, and will
        /// convert the address into a pre-OMAP (src) address prior to attempting symbol lookup. If <paramref name="rva"/>
        /// is already a src address, consider using <see cref="TryGetSymbolByRawRVA(int, out SymType, out int)"/> instead.
        /// </summary>
        /// <param name="rva">The address to resolve.</param>
        /// <param name="symType">The symbol associated with the given address.</param>
        /// <param name="displacement">The displacement of <paramref name="symType"/> relative to the given address.</param>
        /// <returns>True if a symbol was found, otherwise false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetSymbolByRVA(int rva, out SymType symType, out int displacement) =>
            TryGetSymbolByRVA(rva, out symType, out displacement, out _);

        public bool TryGetSymbolByRVA(int rva, out SymType symType, out int displacement, out IMOD imod)
        {
            symType = default;
            displacement = 0;

            GetOmapSectionAndOffset(ref rva, out var sectionNumber, out var relativeOffset, out _);

            return TryGetSymbolBySectionAndOffset(sectionNumber, relativeOffset, out symType, out displacement, out imod);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetSymbolByRawRVA(int rawRVA, out SymType symType, out int displacement) =>
            TryGetSymbolByRawRVA(rawRVA, out symType, out displacement, out _);

        public bool TryGetSymbolByRawRVA(int rawRVA, out SymType symType, out int displacement, out IMOD imod)
        {
            symType = default;
            displacement = 0;

            GetSectionAndOffset(rawRVA, out var sectionNumber, out var relativeOffset, out _);

            return TryGetSymbolBySectionAndOffset(sectionNumber, relativeOffset, out symType, out displacement, out imod);
        }

        //Caller must have converted the raw RVA + section number to OMAP
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetSymbolBySectionAndOffset(
            ISECT sectionNumber,
            int relativeOffset,
            out SymType symType,
            out int displacement) =>
            TryGetSymbolBySectionAndOffset(sectionNumber, relativeOffset, out symType, out displacement, out _);

        //Caller must have converted the raw RVA + section number to OMAP
        public bool TryGetSymbolBySectionAndOffset(
            ISECT sectionNumber,
            int relativeOffset,
            out SymType symType,
            out int displacement,
            out IMOD imod)
        {
            /* DIA contains many address traversers, many of which call into each other
             *
             *     CCompByAddrTrav
             *     CPubByAddrTrav
             *     CFuncByAddrTrav
             * CInlineFuncByAddrTrav (not sure what this does)
             *     CBlockByAddrTrav
             * CLabelByAddrTrav (not sure what this does)
             *     CGlobalDataByAddrTrav
             *     CAllDataByAddrTrav
             *     CDataByAddrTrav
             *     CAllSymsByAddrTrav
             * COMAPSymsByAddrTrav (not sure what this does)
             *     CModSymsByAddrTrav
             *
             * CAllDataByAddrTrav
             *     CGlobalDataByAddrTrav
             *     CDataByAddrTrav
             *
             * CPubByAddrTrav
             *     PSGSI::getEnumByAddr
             *
             * CDataByAddrTrav
             *     ModCache::dataByAddr
             *     CModSymsByAddrTrav
             *
             * CGlobalDataByAddrTrav
             *     GSI1::NextSym
             *
             * CModSymsByAddrTrav
             *     DBI::getEnumContrib
             *
             * CBlockByAddrTrav
             *     CModSymsByAddrTrav
             *     iterates over the symbols looking for block symbols
             *
             * CAllSymsByAddrTrav
             *     CPubByAddrTrav
             *     CBlockByAddrTrav - it seems like CModSymsByAddrTrav is a derived class maybe? And it starts by calling into CFuncByAddrTrav to get the parent symbol
             *     CDataByAddrTrav
             *     CGlobalDataByAddrTrav
             *
             * CCompByAddrTrav (only used when you do a search for SymTagCompiland)
             *     DBI::QueryModFromAddr
             *     the IMod is then retrieved from the Mod1, and thenfrom that the module data is somehow retrieved
             *
             * CFuncByAddrTrav
             *     CModSymsByAddrTrav
             *     ModCache::blockByAddr
             *     SymBuffer::isFunctionSym
             * 
             * We implement the logic of CAllSymsByAddrTrav, modified to cache any state on the PDBFile rather than on the msdia140!SymCache
             */

            if (PDBFileAddrTrav.TryCreate(_symCache, this, out var trav) && trav.FInit(sectionNumber, relativeOffset, out var result))
            {
                var bestOffSeg = result.offSegSym;
                var bestSeg = bestOffSeg.seg;
                var bestOff = bestOffSeg.off;
                imod = result.imod;
                symType = bestOffSeg.symType;

                //Note that it's important that we do this _before_ we start trying to resolve any block symbols, because a SepCode symbol
                //may resolve to a parent lambda with off/seg -1 which will cause us to return -1 even though "the sepcode itself was good"
                displacement = bestSeg == sectionNumber
                            ? relativeOffset - bestOff
                            : -1;

                return true;
            }

            symType = default;
            displacement = default;
            imod = default;
            return false;
        }

        //Section numbers are 1 based

        /// <summary>
        /// Gets the section number and offset into the section that maps to the specified RVA.<para/>
        /// If the RVA does not lie within the bounds of a section (i.e. it resides prior to the start of the first section,
        /// between the end and start of two sections, or past the bounds of the last section) this method will return <see langword="false"/>.<para/>
        /// This method does perform OMAP transformations; it merely detects the <see cref="ImageSectionHeader"/> that the given
        /// RVA lies within.
        /// </summary>
        /// <param name="rva">The RVA to resolve to a section number and offset</param>
        /// <param name="sectionNumber">The 1-based section number of the section that contains this RVA.</param>
        /// <param name="relativeOffset">The relative offset into the section that the RVA represents.</param>
        /// <returns>True if the RVA lies within the bounds of a section, otherwise false.</returns>
        public bool TryGetSectionAndOffset(int rva, out ISECT sectionNumber, out int relativeOffset)
        {
            var sectionHeaders = GetSectionHeaders();

            if (sectionHeaders != null)
                return ImageSectionHeader.TryGetSectionAndOffset(sectionHeaders, rva, out sectionNumber, out relativeOffset);

            sectionNumber = default;
            relativeOffset = default;
            return false;
        }

        /// <summary>
        /// Gets the best-effort section number and offset against the section that maps to the specified RVA.<para/>
        /// If the RVA lies prior to the start of the first section, this method will return the RVA itself as the relative offset
        /// against a non-existent section 0. If the RVA lies between the end and start of two sections or is past the end of the last section,
        /// the RVA will be reported as being relative to the last section that existed before it.
        /// </summary>
        /// <param name="rva">The RVA to resolve to a section number and offset</param>
        /// <param name="sectionNumber">The 1-based section number of the section that contains this RVA, or 0 if the RVA resides prior to the start of the first section.</param>
        /// <param name="relativeOffset">The relative offset past the start the section that the RVA represents.</param>
        /// <param name="isValid">Whether the specified RVA resides within the virtual bounds of the section it is listed as pertaining to.</param>
        public void GetSectionAndOffset(int rva, out ISECT sectionNumber, out int relativeOffset, out bool isValid) =>
            ImageSectionHeader.GetSectionAndOffset(GetSectionHeaders(), rva, out sectionNumber, out relativeOffset, out isValid);

        /// <summary>
        /// Converts the specified in OMAP address space to Src address space if needed, and then gets the best-effort
        /// section number and offset against the section that maps to that RVA.
        /// </summary>
        /// <param name="rva">The RVA in OMAP address space to resolve. If the PDB does not have OMAP information,
        /// the OMAP and Src address spaces are the same.</param>
        /// <param name="sectionNumber">The 1-based section number of the section that contains this RVA, or 0 if the RVA resides prior to the start of the first section.</param>
        /// <param name="relativeOffset">The relative offset past the start the section that the RVA represents.</param>
        /// <param name="isValid">Whether the specified RVA resides within the virtual bounds of the section it is listed as pertaining to.</param>
        public void GetOmapSectionAndOffset(ref int rva, out ISECT sectionNumber, out int relativeOffset, out bool isValid)
        {
            if (HasOmapToSrc)
            {
                //TryConvertOmapToSrc preserves the rva
                if (!omapToSrc.TryConvertOmapToSrc(rva, out rva))
                {
                    Debug.Assert(false); //Not sure what we should do here
                }
            }

            ImageSectionHeader.GetSectionAndOffset(GetSectionHeaders(), rva, out sectionNumber, out relativeOffset, out isValid);
        }

        public bool TryGetModuleBySectionAndOffset(
            ISECT sectionNumber,
            int relativeOffset,
            out IMOD imod,
            out IModi modi,
            out SC40 sc)
        {
            imod = IMOD.Nil;
            modi = default;
            sc = default;

            var modules = DBI?.Modules;

            if (modules == null)
                return false;

            if (TryGetModuleIndexBySectionAndOffset(sectionNumber, relativeOffset, out imod, out sc))
            {
                //Module numbers are 1 based
                if (imod > modules.Length)
                    return false;

                //microsoft-pdb calls ximodForIMod which does +1 to this value. an ximod is an "external" imod,
                //which is 1 based, which means that the actual module indices on the raw SC items are 0 based
                modi = modules[imod];
                return true;
            }

            modi = default;
            return false;
        }

        public bool TryGetModuleIndexBySectionAndOffset(ISECT sectionNumber, int relativeOffset, out IMOD imod, out SC40 sc)
        {
            //DBI1::QueryImodFromAddrHelper does a binary search on the section contribs to the contrib that contains the listed section and offset.

            var dbi = DBI;
            imod = IMOD.Nil;
            sc = default;

            if (dbi == null)
                return false;

            var sectionContribs = dbi.SectionContribs;

            if (sectionContribs == null)
                return false;

            var sectionHeaders = dbi.SectionHdr ?? fallbackSectionHeaders;

            if (sectionHeaders == null || sectionNumber > sectionHeaders.Length)
                return false;

            //Getting the section is easy; the hard part is identifying the module
            if (!sectionContribs.TryGetSection(sectionNumber, relativeOffset, out sc))
                return false;

            //It's up to the caller to validate that the imod is within range; we're just telling them
            //what the section contribs say
            imod = sc.imod;
            return true;
        }

        //Requires that the module have a segment and offset to help us locate the module via section contribs
        public bool TryGetModuleIndexBySymType(in SymType symType, out IMOD imod)
        {
            if (!symType.TryGetRawOffSeg(out var off, out var seg))
            {
                imod = IMOD.Nil;
                return false;
            }

            return TryGetModuleIndexBySectionAndOffset(seg, off, out imod, out _);
        }

        public bool TryGetModuleBySymType(in SymType symType, out IModi modi)
        {
            if (TryGetModuleIndexBySymType(symType, out var imod))
            {
                //We already know we have a DBI at this point
                var modules = DBI!.Modules;

                if (modules != null && imod < modules.Length)
                {
                    modi = modules[imod];
                    return true;
                }
            }

            modi = default;
            return false;
        }

        #region ISymbolAccessor

        IMAGE_FILE_MACHINE ICodeViewAccessor.MachineType
        {
            get
            {
                var dbi = DBI;

                if (dbi == null)
                    return IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_UNKNOWN;

                var hdr = dbi.DbiHdr;

                if (hdr is NewDBIHdr h)
                    return h.wMachine;

                //Assume that old PDBs are for x86?
                return IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_I386;
            }
        }

        //We cache these values here for faster lookup
        private int hasOmapFromSrc = -1;
        private NativeSpan<OMAP_DATA> omapFromSrc;
        bool ICodeViewAccessor.HasOmapFromSrc => HasOmapFromSrc;
        private bool HasOmapFromSrc
        {
            get
            {
                if (hasOmapFromSrc == -1)
                {
                    var dbi = DBI;
                    var dbgHdr = dbi?.DbgHdr;

                    if (dbgHdr != null && dbgHdr.OmapFromSrc != SN.Nil)
                    {
                        //Now let's check if we actually have OMAP data
                        omapFromSrc = dbi.OmapFromSrc ?? default;

                        if (omapFromSrc.Length > 0)
                            hasOmapFromSrc = 1;
                        else
                            hasOmapFromSrc = 0; //This is not good, we're not going to be able to translate addresses!
                    }
                    else
                        hasOmapFromSrc = 0;
                }

                return hasOmapFromSrc != 0;
            }
        }

        //We cache these values here for faster lookup
        private int hasOmapToSrc = -1;
        private NativeSpan<OMAP_DATA> omapToSrc;
        private bool HasOmapToSrc
        {
            get
            {
                if (hasOmapToSrc == -1)
                {
                    var dbi = DBI;
                    var dbgHdr = dbi?.DbgHdr;

                    if (dbgHdr != null && dbgHdr.OmapToSrc != SN.Nil)
                    {
                        //Now let's check if we actually have OMAP data
                        omapToSrc = dbi.OmapToSrc ?? default;

                        if (omapToSrc.Length > 0)
                            hasOmapToSrc = 1;
                        else
                            hasOmapToSrc = 0; //This is not good, we're not going to be able to translate addresses!
                    }
                    else
                        hasOmapToSrc = 0;
                }

                return hasOmapToSrc != 0;
            }
        }

        //Caller must have asked if we have OmapFromSrc data before calling this method
        NativeSpan<OMAP_DATA> ICodeViewAccessor.GetOmapFromSrc() => omapFromSrc;

        ImageSectionHeader[]? ICodeViewAccessor.GetSectionHeaders() => DBI?.SectionHdr ?? fallbackSectionHeaders;

        SymType ICodeViewAccessor.GetModuleSymbol(ushort imod, int ibSym)
        {
            var modules = DBI?.Modules;

            //Module indices are 1 based. So the last module is == modules.Length

            if (modules == null || imod > modules.Length)
                return default;

            var module = modules[imod - 1];

            var symbols = module.Symbols;

            if (symbols == null)
                return default;

            //This isn't super ideal (because it will force load _all_ symbols for the module) but I'm not sure what the best way of
            //storing a reference to the module's MemoryChunk is without needing to constantly try and lookup the symbol stream

            return symbols.GetSymbolFromOffset(ibSym);
        }

        bool ICodeViewAccessor.TryGetSectionContrib(SymType symType, ISECT sectionNumber, int relativeOffset, out SC40 sc) =>
            SymType.TryPDBGetSectionContrib(symType, sectionNumber, relativeOffset, this, out sc);

        private bool? hasLengthPrefixedStrings;

        bool ICodeViewAccessor.HasLengthPrefixedStrings
        {
            get
            {
                if (hasLengthPrefixedStrings.HasValue)
                    return hasLengthPrefixedStrings.Value;

                hasLengthPrefixedStrings = PDB!.PDBHeader.ImplementationVersion <= PDBIMPV.PDBImpvVC98;

                return hasLengthPrefixedStrings.Value;
            }
        }

        int? ICodeViewAccessor.GetRawRelativeVirtualAddress(ushort seg, int off) =>
            SymType.GetRelativeVirtualAddressFromSectionHeaders(((ICodeViewAccessor) this).GetSectionHeaders(), seg, off);

        int? ICodeViewAccessor.GetOmapRelativeVirtualAddress(ushort rawSeg, int rawOff)
        {
            var rawRVA = SymType.GetRelativeVirtualAddressFromSectionHeaders(((ICodeViewAccessor) this).GetSectionHeaders(), rawSeg, rawOff);

            if (rawRVA != null)
            {
                //Convert to OMAP-aware

                if (HasOmapFromSrc)
                {
                    if (omapFromSrc.TryConvertOmapFromSrc(rawRVA.Value, out var omapRva))
                    {
                        return omapRva;
                    }

                    Debug.Assert(false);
                    return null; //Not sure what we should do
                }
                else
                    return rawRVA;
            }
            else
                return null;
        }

        #endregion
        #region IViewable

        void IViewable.WriteGlobals(ViewWriter writer) => WriteGlobals(writer);

        IView? IViewable.WriteStruct(ViewWriter writer) => null;

        int IViewable.NumChildren() => throw new NotSupportedException();

        void IViewable.WriteChild(int index, ref StructWriter structWriter) => throw new NotSupportedException();

        protected abstract void WriteGlobals(ViewWriter writer);

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

        private FileAccessor? _viewAccessor;

        public FileView GetView(
            LocatorHttpPolicy httpPolicy = LocatorHttpPolicy.None,
            bool trackXRefs = false,
            CancellationToken cancellationToken = default)
        {
            if (_viewAccessor == null)
            {
                var accessor = FileAccessor.Create(this);
                FileAnalyzer.Analyze(accessor, httpPolicy: httpPolicy, trackXRefs: trackXRefs, cancellationToken: cancellationToken);
                _viewAccessor = accessor;
            }

            return _viewAccessor.GetFileView();
        }

        public FileView GetViewOld()
        {
            var writer = new ViewWriter(this);
            ((IViewable) this).WriteGlobals(writer);

            return (FileView) writer.Finalize();
        }

        public ISymbolAccessor GetSymbolAccessor(
            LocatorHttpPolicy httpPolicy = LocatorHttpPolicy.All,
            ILocatorProgress? progress = null,
            CancellationToken cancellationToken = default) => symbolAccessor ??= new PDBFileSymbolAccessor(this);

        internal unsafe ByteViewProvider CreateByteViewProvider(FileAccessor fileAccessor) => new LocalByteViewProvider(mmf.Address, mmf.Length, fileAccessor);

        #endregion

        internal void RegisterC13SymbolMemory(MemoryChunk dataChunk, ICodeViewModuleAccessor codeViewModuleAccessor)
        {
            lock (c13SymbolMemoryLock)
            {
                if (c13RegisteredSymbolMemory.Add((int) dataChunk.AbsoluteOffset))
                    SymbolMemoryTracker.RegisterPDBSymbolMemory(dataChunk, codeViewModuleAccessor);
            }
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

            _viewAccessor?.Dispose();

            psgsi?.Dispose();
            tpi?.Dispose();

            globalBlock.Dispose();
            mmf.Dispose();

            disposed = true;
        }

        public override string ToString()
        {
            if (Name != null)
                return Name.ToString();

            return base.ToString();
        }
    }
}
