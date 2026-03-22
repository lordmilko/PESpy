using System;
using System.Diagnostics;
using System.Linq;
using ClrDebug.OMF;
using ClrDebug.PDB;
using PESpy.PDB;
using PESpy.View;

namespace PESpy
{
    /* Contrary to https://web.archive.org/web/20160909082838/http://pierrelib.pagesperso-orange.fr/exec_formats/MS_Symbol_Type_v1.0.pdf
     * my observation is that in NB05, symbols are ordered
     * 1. Everything in module 1 (including the sstModule)
     * 2. Everything in module 2 (including the sstModule)
     *
     * etc
     *
     * By contrast, in later codeView versions the ordering is
     *
     * 1. All of the sstModule entries
     * 2. Everything else for module 1
     * 3. Everything else for module 2
     *
     * etc
     */

    //In NB05, sections are ordered
    //https://web.archive.org/web/20160909082838/http://pierrelib.pagesperso-orange.fr/exec_formats/MS_Symbol_Type_v1.0.pdf
    internal class NB05SymbolAccessor : ICodeViewAccessor, ISymbolAccessor
    {
        protected IFile file;
        internal NB05Data data; //Set after construction
        internal CV_SIGNATURE CvSignature;

        internal CodeViewSig CodeViewSig => data.Signature;

        public bool HasLengthPrefixedStrings { get; set; }

        //Data that is synthesized from the list of dir entries
        private OMFDirEntry[][] moduleEntries;
        private OMFDirEntry[] globalEntries;
        private SC20[]? sectionContribs;

        public NB05SymbolAccessor(IFile file)
        {
            this.file = file;
        }

        #region ICodeViewAccessor

        public ImageSectionHeader[]? GetSectionHeaders() => file.GetSectionHeaders();

        public virtual SymType GetModuleSymbol(ushort imod, int ibSym)
        {
            /* The ordering of module sections should be
             * 1. sstTypes
             * 2. sstPublics
             * 3. sstSymbols
             * 4. sstSrcModule
             */

            throw new NotImplementedException();
        }

        public virtual TypType GetTypTypeFromIndex(CV_typ_t typeIndex)
        {
            throw new NotImplementedException();
        }

        public TypType GetTypTypeFromIndex(CV_ItemId typeIndex)
        {
            throw new NotImplementedException();
        }

        public virtual int? GetRelativeVirtualAddress(ushort seg, int off) =>
            SymType.GetRelativeVirtualAddressFromSectionHeaders(GetSectionHeaders(), seg, off);

        #endregion
        #region ISymbolAccessor

        public SymbolAccessorKind Kind => SymbolAccessorKind.CodeView;

        public unsafe bool TryGetNameFromAddress(int targetAddress, out SymString name, out int displacement)
        {
            //In the long run, I think we would just be better off if we grouped all of the directory entries per module. That way,
            //given a module index we can go straight to the records that are associated with it

            EnsureSynthesizedData();

            name = default;
            displacement = default;

            var sectionHeaders = GetSectionHeaders();

            if (!ImageSectionHeader.TryGetSectionAndOffset(sectionHeaders, targetAddress, out var sectionNumber, out var relativeOffset))
                return false;
            fixed (SC20* pSectionContribs = sectionContribs)
            {
                if (SectionContribsV40.TryGetSection(new NativeSpan<SC20>(pSectionContribs, sectionContribs.Length), sectionNumber, relativeOffset, out var sc))
                {
                    if (TryGetSymTypeFromModule(sc, sectionNumber, relativeOffset, out symType, out displacement))
                    {
                        if (symType.TryGetName(out name, this))
                        {
                            return true;
                        }
                        else
                        {
                            throw new NotImplementedException();
                        }
                    }
                }
            }

            //Contrary to how we operate with PDBFile, we try publics last. We're not trying to "upgrade" symbols here, we're just trying to find
            //something with a given name

            if (TryGetPubSym(sectionNumber, relativeOffset, out symType))
            {
                name = symType.GetName(this);
                return true;
            }

            return false;
        }

        private bool TryGetPubSym(ISECT sectionNumber, int relativeOffset, out SymType symType)
        {
            //First, try and get a public symbol. There's no guarantee that all linked object files included their private symbols in them,
            //so it's entirely possible that publics will be the best we get
            for (var i = 0; i < globalEntries.Length; i++)
            {
                ref var entry = ref globalEntries[i];

                switch (entry.SubSection)
                {
                    case SST.sstGlobalPub:
                        if (TryGetHashedSymbol((OMFHashedSymbols) entry.Data, sectionNumber, relativeOffset, out symType))
                        {
                            return true;
                        }

                        break;

                    case SST.sstPublic:
                    case SST.sstPublicSym:
                        throw new NotImplementedException();
                }
            }

            symType = default;
            return false;
        }

        private bool TryGetSymTypeFromModule(SC20 sc, ISECT sectionNumber, int relativeOffset, out SymType symType, out int displacement)
        {
            var scEnd = sc.off + sc.cb;

            var entries = moduleEntries[sc.imod - 1];

            for (var i = 0; i < entries.Length; i++)
            {
                ref var entry = ref entries[i];

                OMFModuleSymbols symbols;

                switch (entry.SubSection)
                {
                    case SST.sstModule:
                    case SST.sstSrcModule:
                        continue;

                    case SST.sstAlignSym:
                        symbols = (OMFModuleSymbols) entry.Data;
                        break;

                    default:
                        throw new NotImplementedException();
                }

                //Same logic as PDBFile: get the "closest" symbol. I think symbols may be listed in ascending order, which means if we go beyond the range
                //of the section contrib, we've gone too far
                if (PDBFile.TryGetBestModuleSymbol(symbols.List, sectionNumber, relativeOffset, sc.off, scEnd, this, out symType, out displacement))
                {
                    return true;
                }
            }

            symType = default;
            displacement = default;
            return false;
        }

        public bool TryGetAddressFromName(FixedUtf8String name, out int targetAddress)
        {
            throw new NotImplementedException();
        }

        private bool TryGetHashedSymbol(OMFHashedSymbols hashedSymbols, ushort sectionNumber, int relativeOffset, out SymType symType)
        {
            var addressHashTable = hashedSymbols.AddressHashTable as IAddrHash32;

            if (addressHashTable == null)
                throw new NotImplementedException(); //Not good; linear search the symbols instead then

            if (addressHashTable.TryGetSymbolOffset(sectionNumber, relativeOffset, out var symbolOffset))
            {
                symType = hashedSymbols.GetSymbolFromOffset(symbolOffset);

                return true;
            }

            symType = default;
            return false;
        }

        public unsafe bool TryGetLengthFromAddress(int targetAddress, ISectionDataAccessor sectionDataAccessor, out int length)
        {
            length = default;

            var sectionHeaders = GetSectionHeaders();

            if (!ImageSectionHeader.TryGetSectionAndOffset(sectionHeaders, targetAddress, out var sectionNumber, out var relativeOffset))
                return false;

            SymType symType;

            fixed (SC20* pSectionContribs = sectionContribs)
            {
                if (SectionContribsV40.TryGetSection(new NativeSpan<SC20>(pSectionContribs, sectionContribs.Length), sectionNumber, relativeOffset, out var sc))
                {
                    if (TryGetSymTypeFromModule(sc, sectionNumber, relativeOffset, out symType, out var displacement))
                    {
                        if (symType.TryGetLength(out length))
                        {
                            length -= displacement;
                            return true;
                        }
                        else
                        {
                            throw new NotImplementedException();
                        }
                    }

                    if (TryGetPubSym(sectionNumber, relativeOffset, out symType))
                    {
                        length = sc.cb;
                        return true;
                    }
                }
            }

            throw new System.NotImplementedException();
        }

        public void Dispose()
        {
            //These symbols are part of the parent file; we have nothing to do
        }

        #endregion

        private void EnsureSynthesizedData()
        {
            var groups = data.DirEntries.GroupBy(e => e.iMod);

            using var modules = new PooledList<OMFDirEntry[]>();

            var i = 0;

            foreach (var group in groups)
            {
                if (group.Key == ushort.MaxValue)
                {
                    //Globals
                    globalEntries = group.ToArray();
                }
                else
                {
                    i++;
                    Debug.Assert(group.Key == i);
                    modules.Add(group.ToArray());
                }
            }

            moduleEntries = modules.ToArray();

            EnsureSectionContribMap();
        }

        private void EnsureSectionContribMap()
        {
            /* In PDB files, the DBI has a global section contribs subsection that lists all of the contributions for each of the modules,
             * CodeView OMF data has no such subsection, so it's on us to build up the section contribs map ourselves. While there may be
             * a "preferred" order that all of the sstModule entries are written first, this is not necessarily guaranteed, so we'll run through
             * all subsections and construct fake SC entries as we go */


            using var results = new PooledList<SC20>();

            var dirEntries = data.DirEntries;

            for (var i = 0; i < dirEntries.Length ; i++)
            {
                ref var dirEntry = ref dirEntries[i];

                if (dirEntry.SubSection == SST.sstModule)
                {
                    var module = (OMFModule) dirEntry.Data;

                    var segInfo = module.SegInfo;

                    for (var j = 0; j < segInfo.Length; j++)
                    {
                        ref var item = ref segInfo[j];

                        results.Add(new SC20
                        {
                            isect = item.Seg,
                            off = item.Off,
                            cb = item.cbSeg,
                            imod = dirEntry.iMod,
                        });
                    }
                }
            }

            results.Sort((a, b) =>
            {
                var diff = a.isect.CompareTo(b.isect);

                if (diff != 0)
                    return diff;

                return a.off.CompareTo(b.off);
            });

            sectionContribs = results.ToArray();
        }

        protected bool TryGetModuleEntry(ushort imod, SST kind, out OMFDirEntry dirEntry)
        {
            EnsureSynthesizedData();

            var entries = moduleEntries[imod - 1];

            for (var i = 0; i < entries.Length; i++)
            {
                ref var entry = ref entries[i];

                if (entry.SubSection == kind)
                {
                    dirEntry = entry;
                    return true;
                }
            }

            dirEntry = default;
            return false;
        }

        public bool TryGetSymbolBySectionAndOffset(ISECT sectionNumber, int relativeOffset, out SymType symType, out int displacement)
        {
            throw new NotImplementedException();
        }
    }
}
