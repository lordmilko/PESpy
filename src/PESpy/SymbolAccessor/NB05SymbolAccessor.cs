using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using ClrDebug.OMF;
using ClrDebug.PDB;
using PESpy.PDB;
using PESpy.PDB.DIA;
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
    internal class NB05SymbolAccessor : ICodeViewAccessor, ISymbolAccessor, ISectionContribs
    {
        protected IFile _file;
        internal NB05Data data; //Set after construction
        internal CV_SIGNATURE CvSignature;

        internal CodeViewSig CodeViewSig => data.Signature;

        public bool HasLengthPrefixedStrings { get; set; }

        //Data that is synthesized from the list of dir entries
        internal OMFDirEntry[][] _moduleEntries;
        internal OMFDirEntry[] _globalEntries;
        private SC40[]? _sectionContribs;

        internal readonly NB05SymCache _symCache;

        public NB05SymbolAccessor(IFile file)
        {
            _file = file;
            _symCache = new NB05SymCache(this);
        }

        #region ICodeViewAccessor

        public ImageSectionHeader[]? GetSectionHeaders() => _file.GetSectionHeaders();

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetSymbolByRVA(int rva, out SymType symType, out int displacement) =>
            TryGetSymbolByRVA(rva, out symType, out displacement, out _);

        public bool TryGetSymbolByRVA(int rva, out SymType symType, out int displacement, out IMOD imod)
        {
            symType = default;
            displacement = 0;

            ImageSectionHeader.GetSectionAndOffset(GetSectionHeaders(), rva, out var sectionNumber, out var relativeOffset, out _);

            return TryGetSymbolBySectionAndOffset(sectionNumber, relativeOffset, out symType, out displacement, out imod);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetSymbolBySectionAndOffset(
            ISECT sectionNumber,
            int relativeOffset,
            out SymType symType,
            out int displacement) =>
            TryGetSymbolBySectionAndOffset(sectionNumber, relativeOffset, out symType, out displacement, out _);

        public unsafe bool TryGetNameFromAddress(int targetAddress, out SymString name, out int displacement)
        {
            if (TryGetSymbolByRVA(targetAddress, out var symType, out displacement) && symType.TryGetName(out name, this))
                return true;

            name = default;
            return false;
        }

        public bool TryGetAddressFromName(FixedUtf8String name, out int targetAddress)
        {
            throw new NotImplementedException();
        }

        public unsafe bool TryGetLengthFromAddress(int targetAddress, ISectionDataAccessor sectionDataAccessor, out int length)
        {
            if (TryGetSymbolByRVA(targetAddress, out var symType, out _))
                return symType.TryGetLength(out length, this);

            length = default;
            return false;
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
                    _globalEntries = group.ToArray();
                }
                else
                {
                    i++;
                    Debug.Assert(group.Key == i);
                    modules.Add(group.ToArray());
                }
            }

            _moduleEntries = modules.ToArray();

            EnsureSectionContribMap();
        }

        private void EnsureSectionContribMap()
        {
            /* In PDB files, the DBI has a global section contribs subsection that lists all of the contributions for each of the modules,
             * CodeView OMF data has no such subsection, so it's on us to build up the section contribs map ourselves. While there may be
             * a "preferred" order that all of the sstModule entries are written first, this is not necessarily guaranteed, so we'll run through
             * all subsections and construct fake SC entries as we go */

            using var results = new PooledList<SC40>();

            var dirEntries = data.DirEntries;

            var sectionHeaders = GetSectionHeaders();

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

                        results.Add(new SC40
                        {
                            isect = item.Seg,
                            off = item.Off,
                            cb = item.cbSeg,
                            imod = (IMOD) (dirEntry.iMod - 1), //These indices seem to be 1-based
                            dwCharacteristics = sectionHeaders[item.Seg - 1].Characteristics
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

            _sectionContribs = results.ToArray();
        }

        protected bool TryGetModuleEntry(ushort imod, SST kind, out OMFDirEntry dirEntry)
        {
            EnsureSynthesizedData();

            var entries = _moduleEntries[imod - 1];

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

        public bool TryGetSymbolBySectionAndOffset(
            ISECT sectionNumber,
            int relativeOffset,
            out SymType symType,
            out int displacement,
            out IMOD imod)
        {
            //Note that interestingly, unlike in PDBs, sstGlobalSym _does_ appear to have
            //an address map, since it's technically also backed by OMFHashedSymbols as well

            EnsureSynthesizedData();

            var trav = new NB05AddrTrav(_symCache, this);

            if (trav.FInit(sectionNumber, relativeOffset, out var result))
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

        int ISectionContribs.Length => _sectionContribs.Length;

        SC40 ISectionContribs.this[int index] => _sectionContribs[index];

        bool ISectionContribs.TryGetSection(ISECT seg, int off, out SC40 sc) =>
            ((ISectionContribs) this).TryGetSection(seg, off, out _, out sc);

        unsafe bool ISectionContribs.TryGetSection(ISECT seg, int off, out int index, out SC40 sc)
        {
            fixed (SC40* pSectionContribs = _sectionContribs)
            {
                if (SectionContribsV40.TryGetSection(new NativeSpan<SC40>(pSectionContribs, _sectionContribs.Length), seg, off, out index, out sc))
                    return true;
            }

            return false;
        }

        bool ICodeViewAccessor.TryGetSectionContrib(SymType symType, ISECT sectionNumber, int relativeOffset, out SC40 sc) =>
            ((ISectionContribs) this).TryGetSection(sectionNumber, relativeOffset, out sc);

        //ISectionContribs causes us to have this; on balance, we do want ISectionContribs to be an IValue
        int IValue.Offset => throw new NotSupportedException();

        void IViewable.WriteGlobals(ViewWriter writer) => throw new NotSupportedException();

        IView? IViewable.WriteStruct(ViewWriter writer) => throw new NotSupportedException();

        int IViewable.NumChildren() => throw new NotSupportedException();

        void IViewable.WriteChild(int index, ref StructWriter structWriter) => throw new NotSupportedException();
    }
}
