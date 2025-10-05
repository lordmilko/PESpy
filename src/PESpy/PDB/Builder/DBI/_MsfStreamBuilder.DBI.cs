using System;
using ClrDebug;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    public partial class MsfStreamBuilder
    {
        public class DBI
        {
            internal bool Changed;

            private readonly PDBFileBuilder _pdbFileBuilder;

            internal DBI(PDBFileBuilder pdbFileBuilder)
            {
                _pdbFileBuilder = pdbFileBuilder;

                _verSignature = NewDBIHdr.hdrSignature;
                _verHdr = DBIImpv.DBIImpvV70;
                _snGSSyms = SN.Nil;
                _snPSSyms = SN.Nil;
                _snSymRecs = SN.Nil;
            }

            internal DBI(MsfStream.DBI dbi, PDBFileBuilder pdbFileBuilder)
            {
                _pdbFileBuilder = pdbFileBuilder;

                //NewDBIHdr
                var newDbiHdr = (NewDBIHdr) dbi.DbiHdr;

                _verSignature = newDbiHdr.verSignature;
                _verHdr = newDbiHdr.verHdr;
                _age = newDbiHdr.age;
                _snGSSyms = newDbiHdr.snGSSyms;
                _usVerAll = newDbiHdr.usVerAll;
                _snPSSyms = newDbiHdr.snPSSyms;
                _usVerPdbDllBuild = newDbiHdr.usVerPdbDllBuild;
                _snSymRecs = newDbiHdr.snSymRecs;
                _usVerPdbDllRBld = newDbiHdr.usVerPdbDllRBld;
                _cbGpModi = newDbiHdr.cbGpModi;
                _cbSC = newDbiHdr.cbSC;
                _cbSecMap = newDbiHdr.cbSecMap;
                _cbFileInfo = newDbiHdr.cbFileInfo;
                _cbTSMap = newDbiHdr.cbTSMap;
                _iMFC = newDbiHdr.iMFC;
                _cbDbgHdr = newDbiHdr.cbDbgHdr;
                _cbECInfo = newDbiHdr.cbECInfo;
                _flags = newDbiHdr.flags;
                _wMachine = newDbiHdr.wMachine;
                _rgulReserved = newDbiHdr.rgulReserved;

                var modules = dbi.Modules;

                if (modules != null)
                    _modules = new ModiBuilderList(modules);

                var sectionContribs = dbi.SectionContribs;

                if (sectionContribs != null)
                    _sectionContribs = new SectionContribsBuilder(sectionContribs);

                var sectionMap = dbi.SectionMap;

                if (sectionMap != null)
                    _sectionMap = new OMFSegMapBuilder(sectionMap);

                var fileInfo = dbi.FileInfo;

                if (fileInfo != null)
                    _fileInfo = new FileInfoBuilder(fileInfo);

                //Type Server

                var nameTableEC = dbi.NameTableEC;

                if (nameTableEC != null)
                    _nameTableEC = new NMTBuilder(nameTableEC);

                var dbgHdr = dbi.DbgHdr;

                if (dbgHdr != null)
                    _dbgHdr = new DbgDataHdrBuilder(dbgHdr);
            }

            internal void Init()
            {
                //DBI1::fInit seems to clear the DBI when you're creating it.
                //It deletes and recreates the stream. It also deletes all sub-streams of the DBI
                _pdbFileBuilder.StreamTable.DeleteStream(SN.DBI);
                _pdbFileBuilder.Commit(PDBCommitFlags.None);
                _pdbFileBuilder.StreamTable[SN.DBI].ByteCount = 0;

#if PDB1_COMPATIBILITY
                //mspdbcore.dll has implemented logic not in microsoft-pdb wherein the NewDBIHdr is immediately inserted
                //back into the file, and it is committed
                pdbFileBuilder.Commit(PDBCommitFlags.DBI);
#endif

                //Creating DBI also creates TPI, IPI, Publics and Globals
                _pdbFileBuilder.AcquireGSI();
                _pdbFileBuilder.AcquirePSGSI();

                _pdbFileBuilder.AcquireTPI();
                _pdbFileBuilder.AcquireIPI();

                //When DBI1::clearDBI calls PDB1::OpenStreamEx to see if the /LinkInfo stream exists, this causes it to be cleared
                //At the end of DBI1::fInit it calls fInitializeTMCacheInfo which adds the /TMCache stream
            }

            #region NewDBIHdr
            #region verSignature

            private int _verSignature;

            /// <summary>
            /// Gets or sets <see cref="NewDBIHdr.verSignature"/>.
            /// </summary>
            public int verSignature
            {
                get => _verSignature;
                set => SetValue(ref _verSignature, value, ref Changed);
            }

            #endregion
            #region verHdr

            private DBIImpv _verHdr;

            /// <summary>
            /// Gets or sets <see cref="NewDBIHdr.verHdr"/>.
            /// </summary>
            public DBIImpv verHdr
            {
                get => _verHdr;
                set => SetValue(ref _verHdr, value, ref Changed);
            }

            #endregion
            #region age

            private int _age;

            /// <summary>
            /// Gets or sets <see cref="NewDBIHdr.age"/>.
            /// </summary>
            public int age
            {
                get => _age;
                set => SetValue(ref _age, value, ref Changed);
            }

            #endregion
            #region snGSSyms

            private SN _snGSSyms;

            /// <summary>
            /// Gets or sets <see cref="NewDBIHdr.snGSSyms"/>.
            /// </summary>
            public SN snGSSyms
            {
                get => _snGSSyms;
                set => SetValue(ref _snGSSyms, value, ref Changed);
            }

            #endregion
            #region usVerAll

            private DbiHdrVersion _usVerAll;

            /// <summary>
            /// Gets or sets <see cref="NewDBIHdr.usVerAll"/>.
            /// </summary>
            public DbiHdrVersion usVerAll
            {
                get => _usVerAll;
                set => SetValue(ref _usVerAll, value, ref Changed);
            }

            #endregion
            #region snPSSyms

            private SN _snPSSyms;

            /// <summary>
            /// Gets or sets <see cref="NewDBIHdr.snPSSyms"/>.
            /// </summary>
            public SN snPSSyms
            {
                get => _snPSSyms;
                set => SetValue(ref _snPSSyms, value, ref Changed);
            }

            #endregion
            #region usVerPdbDllBuild

            private ushort _usVerPdbDllBuild;

            /// <summary>
            /// Gets or sets <see cref="NewDBIHdr.usVerPdbDllBuild"/>.
            /// </summary>
            public ushort usVerPdbDllBuild
            {
                get => _usVerPdbDllBuild;
                set => SetValue(ref _usVerPdbDllBuild, value, ref Changed);
            }

            #endregion
            #region snSymRecs

            private SN _snSymRecs;

            /// <summary>
            /// Gets or sets <see cref="NewDBIHdr.snSymRecs"/>.
            /// </summary>
            public SN snSymRecs
            {
                get => _snSymRecs;
                set => SetValue(ref _snSymRecs, value, ref Changed);
            }

            #endregion
            #region usVerPdbDllRBld

            private ushort _usVerPdbDllRBld;

            /// <summary>
            /// Gets or sets <see cref="NewDBIHdr.usVerPdbDllRBld"/>.
            /// </summary>
            public ushort usVerPdbDllRBld
            {
                get => _usVerPdbDllRBld;
                set => SetValue(ref _usVerPdbDllRBld, value, ref Changed);
            }

            #endregion
            #region cbGpModi

            private int _cbGpModi;

            /// <summary>
            /// Gets or sets <see cref="NewDBIHdr.cbGpModi"/>.<para/>
            /// This value is automatically updated by <see cref="PDBFileBuilder"/> during serialization.
            /// </summary>
            public int cbGpModi
            {
                get => _cbGpModi;
                set => SetValue(ref _cbGpModi, value, ref Changed);
            }

            #endregion
            #region cbSC

            private int _cbSC;

            /// <summary>
            /// Gets or sets <see cref="NewDBIHdr.cbSC"/>.<para/>
            /// This value is automatically updated by <see cref="PDBFileBuilder"/> during serialization.
            /// </summary>
            public int cbSC
            {
                get => _cbSC;
                set => SetValue(ref _cbSC, value, ref Changed);
            }

            #endregion
            #region cbSecMap

            private int _cbSecMap;

            /// <summary>
            /// Gets or sets <see cref="NewDBIHdr.cbSecMap"/>.<para/>
            /// This value is automatically updated by <see cref="PDBFileBuilder"/> during serialization.
            /// </summary>
            public int cbSecMap
            {
                get => _cbSecMap;
                set => SetValue(ref _cbSecMap, value, ref Changed);
            }

            #endregion
            #region cbFileInfo

            private int _cbFileInfo;

            /// <summary>
            /// Gets or sets <see cref="NewDBIHdr.cbFileInfo"/>.<para/>
            /// This value is automatically updated by <see cref="PDBFileBuilder"/> during serialization.
            /// </summary>
            public int cbFileInfo
            {
                get => _cbFileInfo;
                set => SetValue(ref _cbFileInfo, value, ref Changed);
            }

            #endregion
            #region cbTSMap

            private int _cbTSMap;

            /// <summary>
            /// Gets or sets <see cref="NewDBIHdr.cbTSMap"/>.<para/>
            /// This value is automatically updated by <see cref="PDBFileBuilder"/> during serialization.
            /// </summary>
            public int cbTSMap
            {
                get => _cbTSMap;
                set => SetValue(ref _cbTSMap, value, ref Changed);
            }

            #endregion
            #region iMFC

            private int _iMFC;

            /// <summary>
            /// Gets or sets <see cref="NewDBIHdr.iMFC"/>.
            /// </summary>
            public int iMFC
            {
                get => _iMFC;
                set => SetValue(ref _iMFC, value, ref Changed);
            }

            #endregion
            #region cbDbgHdr

            private int _cbDbgHdr;

            /// <summary>
            /// Gets or sets <see cref="NewDBIHdr.cbDbgHdr"/>.<para/>
            /// This value is automatically updated by <see cref="PDBFileBuilder"/> during serialization.
            /// </summary>
            public int cbDbgHdr
            {
                get => _cbDbgHdr;
                set => SetValue(ref _cbDbgHdr, value, ref Changed);
            }

            #endregion
            #region cbECInfo

            private int _cbECInfo;

            /// <summary>
            /// Gets or sets <see cref="NewDBIHdr.cbECInfo"/>.<para/>
            /// This value is automatically updated by <see cref="PDBFileBuilder"/> during serialization.
            /// </summary>
            public int cbECInfo
            {
                get => _cbECInfo;
                set => SetValue(ref _cbECInfo, value, ref Changed);
            }

            #endregion
            #region flags

            private DbiHdrFlags _flags;

            /// <summary>
            /// Gets or sets <see cref="NewDBIHdr.flags"/>.
            /// </summary>
            public DbiHdrFlags flags
            {
                get => _flags;
                set => SetValue(ref _flags, value, ref Changed);
            }

            #endregion
            #region wMachine

            private IMAGE_FILE_MACHINE _wMachine;

            /// <summary>
            /// Gets or sets <see cref="NewDBIHdr.wMachine"/>.
            /// </summary>
            public IMAGE_FILE_MACHINE wMachine
            {
                get => _wMachine;
                set => SetValue(ref _wMachine, value, ref Changed);
            }

            #endregion
            #region rgulReserved

            private int _rgulReserved;

            /// <summary>
            /// Gets or sets <see cref="NewDBIHdr.rgulReserved"/>.
            /// </summary>
            public int rgulReserved
            {
                get => _rgulReserved;
                set => SetValue(ref _rgulReserved, value, ref Changed);
            }

            #endregion
            #endregion
            #region Modules

            private ModiBuilderList? _modules;

            public ModiBuilderList? Modules
            {
                get => _modules;
                set => SetValue(ref _modules, value, ref Changed);
            }

            #endregion
            #region Section Contribs

            private SectionContribsBuilder? _sectionContribs;

            public SectionContribsBuilder? SectionContribs
            {
                get => _sectionContribs;
                set => SetValue(ref _sectionContribs, value, ref Changed);
            }

            #endregion
            #region Section Map

            private OMFSegMapBuilder? _sectionMap;

            public OMFSegMapBuilder? SectionMap
            {
                get => _sectionMap;
                set => SetValue(ref _sectionMap, value, ref Changed);
            }

            #endregion
            #region File Info

            private FileInfoBuilder? _fileInfo;

            public FileInfoBuilder? FileInfo
            {
                get => _fileInfo;
                set => SetValue(ref _fileInfo, value, ref Changed);
            }

            #endregion

            //Type Server

            #region Edit and Continue Info

            private NMTBuilder? _nameTableEC;

            public NMTBuilder? NameTableEC
            {
                get => _nameTableEC;
                set => SetValue(ref _nameTableEC, value, ref Changed);
            }

            #endregion
            #region Optional Debug Header

            private DbgDataHdrBuilder? _dbgHdr;

            public DbgDataHdrBuilder? DbgHdr
            {
                get => _dbgHdr;
                set => SetValue(ref _dbgHdr, value, ref Changed);
            }

            #endregion

            internal void Measure()
            {
                cbGpModi = Modules?.Measure() ?? 0;
                cbSC = SectionContribs?.Measure() ?? 0;
                cbSecMap = SectionMap?.Measure() ?? 0;
                cbFileInfo = FileInfo?.Measure() ?? 0;
                //Type Server Map
                cbECInfo = NameTableEC?.Measure() ?? 0;
                cbDbgHdr = DbgHdr?.Measure() ?? 0;

                var size = NewDBIHdr.StructSize + cbGpModi + cbSC + cbSecMap + cbFileInfo + cbECInfo + cbDbgHdr;

                //The PDB stream is saved by "replacing" it, which means that the previous stream gets deleted
                _pdbFileBuilder.StreamTable.DeleteStream(SN.DBI);

                _pdbFileBuilder.AllocPages(SN.DBI, size);
            }

            internal void Serialize()
            {
                if (!Changed)
                    return;

                var chunk = _pdbFileBuilder.SlicePaged(SN.DBI);

                _ = new NewDBIHdr(chunk)
                {
                    verSignature = verSignature,
                    verHdr = verHdr,
                    age = age,
                    snGSSyms = snGSSyms,
                    usVerAll = usVerAll,
                    snPSSyms = snPSSyms,
                    usVerPdbDllBuild = usVerPdbDllBuild,
                    snSymRecs = snSymRecs,
                    usVerPdbDllRBld = usVerPdbDllRBld,
                    cbGpModi = cbGpModi,
                    cbSC = cbSC,
                    cbSecMap = cbSecMap,
                    cbFileInfo = cbFileInfo,
                    cbTSMap = cbTSMap,
                    iMFC = iMFC,
                    cbDbgHdr = cbDbgHdr,
                    cbECInfo = cbECInfo,
                    flags = flags,
                    wMachine = wMachine,
                    rgulReserved = rgulReserved
                };

                var offset = NewDBIHdr.StructSize;

                if (Modules != null)
                {
                    Modules.Serialize(chunk.Slice(offset));
                    offset += cbGpModi;
                }

                if (SectionContribs != null)
                {
                    SectionContribs.Serialize(chunk.Slice(offset));
                    offset += cbSC;
                }

                if (SectionMap != null)
                {
                    SectionMap.Serialize(chunk.Slice(offset));
                    offset += cbSecMap;
                }

                if (FileInfo != null)
                {
                    FileInfo.Serialize(chunk.Slice(offset));
                    offset += cbFileInfo;
                }

                //Type Server Map
                if (NameTableEC != null)
                {
                    NameTableEC.Serialize(chunk.Slice(offset));
                    offset += cbECInfo;
                }

                if (DbgHdr != null)
                {
                    DbgHdr.Serialize(chunk.Slice(offset));
                    offset += cbDbgHdr;
                }

                Changed = false;
            }
        }
    }
}
