using System;
using System.Collections.Generic;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    public static partial class MsfStream
    {
        public class DBI : IValue, IViewable
        {
            public IDBIHdr DbiHdr { get; }

            #region Modules Substream

            private IModi[]? modules;

            public IModi[]? Modules
            {
                get
                {
                    //Module Substream
                    if (modules == null && DbiHdr.cbGpModi > 0)
                    {
                        var end = DbiHdr.cbGpModi;

                        using var results = new PooledList<IModi>();

                        var moduleChunk = chunk.Slice(DbiHdr.StructSize);

                        if (DbiHdr is NewDBIHdr n)
                        {
                            if (n.verHdr < DBIImpv.DBIImpvV60)
                            {
                                //MODI50
                                throw new NotImplementedException("Processing MODI50 and earlier is not implemented");
                            }
                            else
                            {
                                int totalRead = 0;

                                while (totalRead < end)
                                {
                                    var module = new Modi60(moduleChunk.Slice(totalRead), out var read);
                                    results.Add(module);
                                    totalRead += read;
                                }

                                Debug.Assert(totalRead == end);
                            }
                        }
                        else
                        {
                            var pdbFile = chunk.PDBFile();

                            if (pdbFile.PDB!.PDBHeader.ImplementationVersion == PDBIMPV.PDBImpvVC2)
                                throw new NotImplementedException("Reading VC2 modules is not implemented"); //NT 4 has special logic for handling V2 files

                            int totalRead = 0;

                            while (totalRead < end)
                            {
                                var module = new Modi(moduleChunk.Slice(totalRead), out var read);
                                results.Add(module);
                                totalRead += read;
                            }

                            Debug.Assert(totalRead == end);
                        }

                        modules = results.ToArray();
                    }

                    return modules;
                }
            }

            #endregion
            #region Section Contribs Substream

            private ISectionContribs? sectionContribs;

            public ISectionContribs? SectionContribs
            {
                get
                {
                    if (sectionContribs == null && DbiHdr.cbSC > 0)
                    {
                        var dataChunk = chunk.Slice(DbiHdr.StructSize + DbiHdr.cbGpModi);

                        var version = (DBISCImpv) dataChunk.PeekUInt32(0);

                        if (version == DBISCImpv.DBISCImpvV60)
                        {
                            var size = DbiHdr.cbSC - 4; //Skip over the version field

                            sectionContribs = new SectionContribsV60(dataChunk, size);
                        }
                        else if (version == DBISCImpv.DBISCImpv2)
                        {
                            var size = DbiHdr.cbSC - 4; //Skip over the version field

                            sectionContribs = new SectionContribs2(dataChunk, size);
                        }
                        else
                        {
                            //Visual C++ 4 does not have a version; we seem to just start reading straight into the Section Contribution data.
                            //This is confirmed by DBI1::getSecContribs

                            sectionContribs = new SectionContribsV40(dataChunk, DbiHdr.cbSC);
                        }
                    }

                    return sectionContribs;
                }
            }

            #endregion
            #region Section Map

            private OMFSegMap? sectionMap;

            public OMFSegMap? SectionMap
            {
                get
                {
                    if (sectionMap == null && DbiHdr.cbSecMap > 0)
                    {
                        var dataChunk = chunk.Slice(DbiHdr.StructSize + DbiHdr.cbGpModi + DbiHdr.cbSC);

                        sectionMap = new OMFSegMap(dataChunk);
                    }

                    return sectionMap;
                }
            }

            #endregion
            #region File Info

            private FileInfo? fileInfo;

            public FileInfo? FileInfo
            {
                get
                {
                    if (fileInfo == null)
                    {
                        var length = DbiHdr.cbFileInfo;

                        fileInfo = new FileInfo(chunk.Slice(DbiHdr.StructSize + DbiHdr.cbGpModi + DbiHdr.cbSC + DbiHdr.cbSecMap), length);
                    }

                    return fileInfo;
                }
            }

            #endregion
            #region Type Server Map

            private object? typeServerMap;

            public object? TypeServerMap
            {
                get
                {
                    if (typeServerMap == null && DbiHdr is NewDBIHdr newDbiHdr && newDbiHdr.cbTSMap > 0)
                    {
                        /* I believe this contains a collection of TSM structs. The only place
                         * TSM items are added seems to be in DBI1::AddTypeServer, which is #if 0'd
                         * out in microsoft-pdb. The method is not present in VS2015+. Furthermore,
                         * Mod1::GetTmts says that support for creating type servers was removed
                         * "a long time ago". It would be good to be able to show the data that
                         * existed here none-the-less, but for now we will just skip it */
                        var dataChunk = chunk.Slice(DbiHdr.StructSize + DbiHdr.cbGpModi + DbiHdr.cbSC + DbiHdr.cbSecMap + DbiHdr.cbFileInfo);
                        typeServerMap = null;
                    }

                    return typeServerMap;
                }
            }

            #endregion
            #region Edit and Continue Info

            private NMT? nameTableEC;

            //MODI has an ECInfo, so we don't call this ECInfo too to avoid confusion
            public NMT? NameTableEC
            {
                get
                {
                    if (nameTableEC == null && DbiHdr is NewDBIHdr newDbiHdr && newDbiHdr.cbECInfo > 0)
                        nameTableEC = new NMT(chunk.Slice(DbiHdr.StructSize + DbiHdr.cbGpModi + DbiHdr.cbSC + DbiHdr.cbSecMap + DbiHdr.cbFileInfo + newDbiHdr.cbTSMap));

                    return nameTableEC;
                }
            }

            #endregion
            #region Optional Debug Header

            private DbgDataHdr? dbgHdr;

            public DbgDataHdr? DbgHdr
            {
                get
                {
                    if (dbgHdr == null && DbiHdr is NewDBIHdr newDbiHdr && newDbiHdr.cbDbgHdr > 0)
                        dbgHdr = new DbgDataHdr(chunk.Slice(DbiHdr.StructSize + DbiHdr.cbGpModi + DbiHdr.cbSC + DbiHdr.cbSecMap + DbiHdr.cbFileInfo + newDbiHdr.cbTSMap + newDbiHdr.cbECInfo), newDbiHdr.cbDbgHdr);

                    return dbgHdr;
                }
            }

            //FPO
            //Exception
            //Fixup
            //OmapToSrc
            //OmapFromSrc

            #region SectionHdr

            private ImageSectionHeader[]? sectionHdr;

            public ImageSectionHeader[]? SectionHdr
            {
                get
                {
                    if (sectionHdr == null)
                    {
                        var dbgHdr = DbgHdr;

                        if (dbgHdr != null)
                        {
                            var pdbFile = chunk.PDBFile();

                            if (pdbFile.TryGetStreamChunk(dbgHdr.SectionHdr, out var valueChunk))
                            {
                                var numItems = valueChunk.Remaining / ImageSectionHeader.StructSize;

                                var items = new ImageSectionHeader[numItems];

                                for (var i = 0; i < numItems; i++)
                                    items[i] = new ImageSectionHeader(valueChunk.Slice(i * ImageSectionHeader.StructSize));

                                sectionHdr = items;
                            }
                        }
                    }

                    return sectionHdr;
                }
            }

            #endregion

            //TokenRidMap
            //XData
            //PData
            //NewFPO

            #region SectionHdrOrig

            private ImageSectionHeader[]? sectionHdrOrig;

            public ImageSectionHeader[]? SectionHdrOrig
            {
                get
                {
                    if (sectionHdrOrig == null)
                    {
                        var dbgHdr = DbgHdr;

                        if (dbgHdr != null)
                        {
                            var pdbFile = chunk.PDBFile();

                            if (pdbFile.TryGetStreamChunk(dbgHdr.SectionHdrOrig, out var valueChunk))
                            {
                                var numItems = valueChunk.Remaining / ImageSectionHeader.StructSize;

                                var items = new ImageSectionHeader[numItems];

                                for (var i = 0; i < numItems; i++)
                                    items[i] = new ImageSectionHeader(valueChunk.Slice(i * ImageSectionHeader.StructSize));

                                sectionHdrOrig = items;
                            }
                        }
                    }

                    return sectionHdrOrig;
                }
            }

            #endregion

            #endregion
            #region Symbols

            private SymTypeList? symbols;

            public unsafe SymTypeList? Symbols
            {
                get
                {
                    if (symbols == null)
                    {
                        var pdbFile = chunk.PDBFile();

                        if (pdbFile.TryGetStreamChunk(DbiHdr.snSymRecs, out var symRecChunk))
                        {
                            SymbolMemoryTracker.RegisterPDBSymbolMemory(symRecChunk);
                            Debug.Assert(symRecChunk.RelativeOffset == 0);
                            symbols = new SymTypeList(symRecChunk.Pointer, symRecChunk.Remaining);
                        }
                    }

                    return symbols;
                }
            }

            #endregion

            private readonly MemoryChunk chunk;

            public int Offset => chunk.AbsoluteOffset;

            internal DBI(in MemoryChunk chunk)
            {
                this.chunk = chunk;

                var sig = chunk.PeekInt16(0);

                /* The layout of the DBI is as follows
                 * - DbiHdr
                 * - Modules
                 * - Section Contribs
                 * - Section Map
                 * - File Info
                 * - Type Server Map
                 * - Edit and Continue Info
                 * - Optional Debug Header
                 *
                 * The only mandatory item is DbiHdr. Items >= Type Server Map are only present when NewDbiHdr is used */

                if (sig == NewDBIHdr.hdrSignature)
                    DbiHdr = new NewDBIHdr(chunk);
                else
                    DbiHdr = new DBIHdr(chunk);

                /* Note that there a confusing illusion that can occur with /names (and likely other data) when looking
                 * at a view of the PDB. Consider the following page layout:
                 * 100: NewDbiHdr, C:\Windows\sys
                 * 102: /names(2) stem32\notepad.exe
                 * 103: /names(1) C:\Windows\sys
                 * 
                 * This creates the illusion that /names is starting on page 100, right after the NewDbiHdr. This is not the case.
                 * As you can see, the actual start of the string has been written in /names(1) on page 103. Page 100 previously
                 * was being used to store /names(1), but got repurposed to store NewDbiHdr instead */

#if STRESS_TEST
                _ = Modules;
                _ = SectionContribs;
                _ = SectionMap;
                _ = FileInfo;
                _ = TypeServerMap;
                _ = NameTableEC;
                _ = DbgHdr;
                _ = Symbols;
#endif
            }

            void IViewable.WriteGlobals(ViewWriter writer)
            {
                writer.WriteGlobal(DbiHdr);
                writer.WriteGlobal(Modules);
                writer.WriteGlobal(SectionContribs);
                writer.WriteGlobal(SectionMap);
                writer.WriteGlobal(FileInfo);
                writer.WriteGlobal(NameTableEC);
                writer.WriteGlobal(DbgHdr);

                var pdbFile = chunk.PDBFile();

                var symbols = Symbols;

                if (symbols != null)
                {
                    if (pdbFile.TryGetStreamChunk(DbiHdr.snSymRecs, out var symRecChunk))
                    {
                        writer.WritePagedGlobal(symRecChunk.RelativeOffset, (PagedMemoryBlock) symRecChunk.block, symbols);
                    }
                }

                //FPO
                //Exception
                //Fixup
                //OmapToSrc
                //OmapFromSrc
                writer.WriteGlobal(SectionHdr);
                //TokenRidMap
                //XData
                //PData
                //NewFPO
                writer.WriteGlobal(SectionHdrOrig);
            }

            IView? IViewable.WriteStruct(ViewWriter writer) => null;

            IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter) => throw new NotSupportedException();
        }
    }
}
