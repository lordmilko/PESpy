using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using ClrDebug;
using ClrDebug.PDB;
using static ClrDebug.PDB.SYM_ENUM_e;

namespace PESpy.PDB
{

    [DebuggerTypeProxy(typeof(SymTypeProxy))]
    [DebuggerDisplay("{SymTypeProxy.DebuggerDisplay(this),nq}")]
    public readonly unsafe struct SymType : IEquatable<SymType>
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly SYMTYPE* value;

        public ushort reclen => value->reclen;
        public SYM_ENUM_e rectyp => value->rectyp;

        public SymType(SYMTYPE* value)
        {
            this.value = value;
        }

        public override int GetHashCode()
        {
            return ((IntPtr) value).GetHashCode();
        }

        public override bool Equals(object? obj)
        {
            if (obj == null)
                return value == default;

            if (obj is SymType s)
                return value == s.value;

            return false;
        }

        public bool Equals(SymType other) => value == other.value;

        public override string ToString()
        {
            if (value == default)
                return "<null>";
            
            return SymTypeProxy.GetString(this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int GetSymbolLength(SYMTYPE* symType)
        {
            //Huge gotcha incoming: certain legacy symbols (S_DATAREF_ST, S_PROCREF_ST and S_LPROCREF_ST) may have a hidden
            //name after them, not included in their lengths. You need to account for this when calculating how big the symbol is!
            //See DBI1::fReadSymRec for details (note that the comments in this function were written before these enum values were
            //renamed to be ST)
            switch (symType->rectyp)
            {
                case S_DATAREF_ST:
                case S_PROCREF_ST:
                case S_LPROCREF_ST:
                    /* Complicating things even further, this length doesn't appear to always necessarily apply. I've found that in
                     * NB11 (whether the data is in the original PE File or if it's been split out to a DBG File) there _isn't_ any
                     * name at the end of the symbol. And in fact, by pretending that there is, we're effectively _skipping over_
                     * the symbol that comes after us! */
                    var accessor = SymbolMemoryTracker.GetAccessor((long) symType);

                    if (accessor is NB05SymbolAccessor a && a.CodeViewSig == CodeViewSig.NB11)
                        goto default;

                    var baseLength = symType->reclen + sizeof(ushort);
                    var strLen = *(((byte*) symType) + baseLength);

                    //The length occupies 1 byte, and then the actual bytes after it occupy even more bytes.
                    //We must align this total length to 32-bits
                    var alignedStringArea = ((strLen + 1) + 3) & ~3;

                    return baseLength + alignedStringArea;

                default:
                    return symType->reclen + sizeof(ushort);
            }
        }

        internal static FixedUtf8String ReadString<T>(T* symType, byte* start) where T : unmanaged
        {
            //We are length prefixed if we're a PDB with impv <= PDBImpvVC98 or are an OBJ file < C13
            var isLengthPrefixedData = SymbolMemoryTracker.IsLengthPrefixedData((long) symType);

            /* PDB files have two different ways of encoding strings
             * - ST, which means the string is length prefixed
             * - SZ, which means that the string is a null-terminated UTF8 string
             *
             * The rules for determining whether a given string is SZ or ST is as follows:
             *
             * 1. if the PDB impv > impvVC98, SZ is used everywhere
             * 2. if the PDB impv <= impvVC98, it's ST if the string type is < S_ST_MAX. If it's >= S_ST_MAX, it's SZ
             * 
             * PDB1::fIsSZPDB() performs this check between PDBStream.impv and impvVC98
             *
             * Confusingly, dumppdb.cpp says that UTF8 applies when the PDB interface version >= PDBImpvVC70. In between VC98
             * and VC70 is VC70Dep, so I think dumppdb is just ignoring VC70Dep, which means > impvVC98 and >= PDBImpvVC70 are saying
             * the same thing */

            var raw = (SYMTYPE*) symType;

            if (isLengthPrefixedData && raw->rectyp < SYM_ENUM_e.S_ST_MAX)
            {
                byte length = *start;

                var pdbString = new FixedUtf8String(start + 1, length);

                return pdbString;
            }
            else
            {
                var utf8 = new Utf8String(start);

                return new FixedUtf8String(start, utf8.Length);
            }
        }

        internal static int? GetRelativeVirtualAddress<T>(T* symType, ushort seg, int off) where T : unmanaged
        {
            //DataSym32 items may have a section number of 0, e.g. IID_IClassFactory in mscordbi. These also don't have an offset,
            //and so therefore don't have an RVA
            if (seg == 0)
                return null;

            var accessor = SymbolMemoryTracker.GetAccessor((long) symType);

            return accessor?.GetRelativeVirtualAddress(seg, off);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int? GetRelativeVirtualAddressFromSectionHeaders(
            ImageSectionHeader[]? sectionHeaders,
            ushort seg,
            int off)
        {
            //The section number we're given is 1 based
            if (sectionHeaders == null || seg > sectionHeaders.Length)
                return 0;

            ref var sectionHeader = ref sectionHeaders[seg - 1];

            return sectionHeader.VirtualAddress + off;
        }

        internal static SymType GetSymbol<T>(T* symType, ushort imod, int ibSym) where T : unmanaged
        {
            //To get the symbol that this ref points to, lookup the module indicated by imod (which is 1 based) and then get the symbol at ibSym bytes into the module's address space

            var accessor = SymbolMemoryTracker.GetAccessor((long) symType);

            if (accessor == null)
                return default;

            return accessor.GetModuleSymbol(imod, ibSym);
        }

        internal static IModi? GetPDBModuleFromSectionAddress<T>(T* symType, ushort seg, int off) where T : unmanaged
        {
            //DBI1::QueryImodFromAddrHelper does a binary search on the section contribs to the contrib that contains the listed section and offset.

            var dbi = ((PDBFile?) SymbolMemoryTracker.GetAccessor((long) symType))?.DBI;

            if (dbi == null)
                return default;

            var sectionContribs = dbi.SectionContribs;

            if (sectionContribs == null)
                return default;

            var modules = dbi.Modules;

            if (modules == null)
                return default;

            var sectionHeaders = dbi.SectionHdr;

            if (sectionHeaders == null || seg > sectionHeaders.Length)
                return default;

            //Getting the section is easy; the hard part is identifying the module
            if (!sectionContribs.TryGetSection(seg, off, out var sc))
                return default;

            //Module numbers are 1 based
            if (sc.imod > modules.Length)
                return default;

            //microsoft-pdb calls ximodForIMod which does +1 to this value. an ximod is an "external" imod,
            //which is 1 based, which means that the actual module indices on the raw SC items are 0 based
            var module = modules[sc.imod];

            return module;
        }

        //Note: can only be used when a symbol actually came from a PDB File, and not an OBJ file or NB05 record
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool TryPDBGetSectionContrib(SYMTYPE* symType, ushort seg, int off, out SC40 sc)
        {
            var pdbFile = ((PDBFile?) SymbolMemoryTracker.GetAccessor((long) symType));

            return TryPDBGetSectionContribInternal(pdbFile, seg, off, out sc);
        }

        internal static bool TryGetSectionCharacteristics(SYMTYPE* symType, ushort seg, int off, out IMAGE_SCN characteristics)
        {
            var accessor = SymbolMemoryTracker.GetAccessor((long) symType);

            if (accessor is PDBFile pdbFile)
            {
                if (TryPDBGetSectionContribInternal((PDBFile) accessor, seg, off, out var sc))
                {
                    characteristics = sc.dwCharacteristics;
                    return true;
                }
            }
            else if (accessor is DOSNB09SymbolAccessor d)
            {
                return d.TryGetSectionCharacteristics(seg, off, out characteristics);
            }
            else
            {
                var sectionHeaders = accessor?.GetSectionHeaders();

                if (sectionHeaders != null)
                {
                    if (seg <= sectionHeaders.Length)
                    {
                        ref var sectionHeader = ref sectionHeaders[seg - 1];
                        characteristics = sectionHeader.Characteristics;
                        return true;
                    }
                }
            }

            characteristics = default;
            return false;
        }

        private static bool TryPDBGetSectionContribInternal(PDBFile? pdbFile, ushort seg, int off, out SC40 sc)
        {
            var dbi = pdbFile?.DBI;

            sc = default!;

            if (dbi == null)
                return false;

            var sectionContribs = dbi.SectionContribs;

            if (sectionContribs == null)
                return false;

            //Getting the section is easy; the hard part is identifying the module
            if (!sectionContribs.TryGetSection(seg, off, out sc))
                return false;

            return true;
        }

        public static implicit operator SymType(SYMTYPE* value) => new SymType(value);
        public static implicit operator SYMTYPE*(SymType value) => value.value;

        public static implicit operator AlignSym(SymType symType) => new AlignSym((ALIGNSYM*) symType.value);
        public static implicit operator AnnotationSym(SymType symType) => new AnnotationSym((ANNOTATIONSYM*) symType.value);
        public static implicit operator ArmSwitchTable(SymType symType) => new ArmSwitchTable((ARMSWITCHTABLE*) symType.value);
        public static implicit operator AttrManyRegSym2(SymType symType) => new AttrManyRegSym2((ATTRMANYREGSYM2*) symType.value);
        public static implicit operator AttrRegRel(SymType symType) => new AttrRegRel((ATTRREGREL*) symType.value);
        public static implicit operator AttrRegSym(SymType symType) => new AttrRegSym((ATTRREGSYM*) symType.value);
        public static implicit operator AttrSlotSym(SymType symType) => new AttrSlotSym((ATTRSLOTSYM*) symType.value);
        public static implicit operator BlockSym16(SymType symType) => new BlockSym16((BLOCKSYM16*) symType.value);
        public static implicit operator BlockSym32(SymType symType) => new BlockSym32((BLOCKSYM32*) symType.value);
        public static implicit operator BPRelSym16(SymType symType) => new BPRelSym16((BPRELSYM16*) symType.value);
        public static implicit operator BPRelSym32(SymType symType) => new BPRelSym32((BPRELSYM32*) symType.value);
        public static implicit operator BPRelSym3216t(SymType symType) => new BPRelSym3216t((BPRELSYM32_16t*) symType.value);
        public static implicit operator BuildInfoSym(SymType symType) => new BuildInfoSym((BUILDINFOSYM*) symType.value);
        public static implicit operator CallSiteInfo(SymType symType) => new CallSiteInfo((CALLSITEINFO*) symType.value);
        public static implicit operator CExMSym16(SymType symType) => new CExMSym16((CEXMSYM16*) symType.value);
        public static implicit operator CExMSym32(SymType symType) => new CExMSym32((CEXMSYM32*) symType.value);
        public static implicit operator CFlagSym(SymType symType) => new CFlagSym((CFLAGSYM*) symType.value);
        public static implicit operator CoffGroupSym(SymType symType) => new CoffGroupSym((COFFGROUPSYM*) symType.value);
        public static implicit operator CompileSym(SymType symType) => new CompileSym((COMPILESYM*) symType.value);
        public static implicit operator CompileSym3(SymType symType) => new CompileSym3((COMPILESYM3*) symType.value);
        public static implicit operator ConstSym(SymType symType) => new ConstSym((CONSTSYM*) symType.value);
        public static implicit operator ConstSym16t(SymType symType) => new ConstSym16t((CONSTSYM_16t*) symType.value);
        public static implicit operator DataSym16(SymType symType) => new DataSym16((DATASYM16*) symType.value);
        public static implicit operator DataSym32(SymType symType) => new DataSym32((DATASYM32*) symType.value);
        public static implicit operator DataSym3216t(SymType symType) => new DataSym3216t((DATASYM32_16t*) symType.value);
        public static implicit operator DataSymHLSL(SymType symType) => new DataSymHLSL((DATASYMHLSL*) symType.value);
        public static implicit operator DataSymHLSL32(SymType symType) => new DataSymHLSL32((DATASYMHLSL32*) symType.value);
        public static implicit operator DataSymHLSL32Ex(SymType symType) => new DataSymHLSL32Ex((DATASYMHLSL32_EX*) symType.value);
        public static implicit operator DefRangeSym(SymType symType) => new DefRangeSym((DEFRANGESYM*) symType.value);
        public static implicit operator DefRangeSymFramePointerRel(SymType symType) => new DefRangeSymFramePointerRel((DEFRANGESYMFRAMEPOINTERREL*) symType.value);
        public static implicit operator DefRangeSymFramePointerRelFullScope(SymType symType) => new DefRangeSymFramePointerRelFullScope((DEFRANGESYMFRAMEPOINTERREL_FULL_SCOPE *) symType.value);
        public static implicit operator DefRangeSymHLSL(SymType symType) => new DefRangeSymHLSL((DEFRANGESYMHLSL*) symType.value);
        public static implicit operator DefRangeSymRegister(SymType symType) => new DefRangeSymRegister((DEFRANGESYMREGISTER*) symType.value);
        public static implicit operator DefRangeSymRegisterRel(SymType symType) => new DefRangeSymRegisterRel((DEFRANGESYMREGISTERREL*) symType.value);
        public static implicit operator DefRangeSymSubField(SymType symType) => new DefRangeSymSubField((DEFRANGESYMSUBFIELD*) symType.value);
        public static implicit operator DefRangeSymSubfieldRegister(SymType symType) => new DefRangeSymSubfieldRegister((DEFRANGESYMSUBFIELDREGISTER*) symType.value);
        public static implicit operator DiscardedSym(SymType symType) => new DiscardedSym((DISCARDEDSYM*) symType.value);
        public static implicit operator DPCSymTagMap(SymType symType) => new DPCSymTagMap((DPCSYMTAGMAP*) symType.value);
        public static implicit operator EntryThisSym(SymType symType) => new EntryThisSym((ENTRYTHISSYM*) symType.value);
        public static implicit operator EnvBlockSym(SymType symType) => new EnvBlockSym((ENVBLOCKSYM*) symType.value);
        public static implicit operator ExportSym(SymType symType) => new ExportSym((EXPORTSYM*) symType.value);
        public static implicit operator FileStaticSym(SymType symType) => new FileStaticSym((FILESTATICSYM*) symType.value);
        public static implicit operator FrameCookie(SymType symType) => new FrameCookie((FRAMECOOKIE*) symType.value);
        public static implicit operator FrameProcSym(SymType symType) => new FrameProcSym((FRAMEPROCSYM*) symType.value);
        public static implicit operator FrameRelSym(SymType symType) => new FrameRelSym((FRAMERELSYM*) symType.value);
        public static implicit operator FunctionList(SymType symType) => new FunctionList((FUNCTIONLIST*) symType.value);
        public static implicit operator HeapAllocSite(SymType symType) => new HeapAllocSite((HEAPALLOCSITE*) symType.value);
        public static implicit operator InlineSiteSym(SymType symType) => new InlineSiteSym((INLINESITESYM*) symType.value);
        public static implicit operator InlineSiteSym2(SymType symType) => new InlineSiteSym2((INLINESITESYM2*) symType.value);
        public static implicit operator LabelSym16(SymType symType) => new LabelSym16((LABELSYM16*) symType.value);
        public static implicit operator LabelSym32(SymType symType) => new LabelSym32((LABELSYM32*) symType.value);
        public static implicit operator LocalDPCGroupSharedSym(SymType symType) => new LocalDPCGroupSharedSym((LOCALDPCGROUPSHAREDSYM*) symType.value);
        public static implicit operator LocalSym(SymType symType) => new LocalSym((LOCALSYM*) symType.value);
        public static implicit operator ManProcSym(SymType symType) => new ManProcSym((MANPROCSYM*) symType.value);
        public static implicit operator ManTypRef(SymType symType) => new ManTypRef((MANTYPREF*) symType.value);
        public static implicit operator ManyRegSym(SymType symType) => new ManyRegSym((MANYREGSYM*) symType.value);
        public static implicit operator ManyRegSym16t(SymType symType) => new ManyRegSym16t((MANYREGSYM_16t*) symType.value);
        public static implicit operator ManyRegSym2(SymType symType) => new ManyRegSym2((MANYREGSYM2*) symType.value);
        public static implicit operator ModTypeRef(SymType symType) => new ModTypeRef((MODTYPEREF*) symType.value);
        public static implicit operator ObjNameSym(SymType symType) => new ObjNameSym((OBJNAMESYM*) symType.value);
        public static implicit operator OemSymbol(SymType symType) => new OemSymbol((OEMSYMBOL*) symType.value);
        public static implicit operator PdbMap(SymType symType) => new PdbMap((PDBMAP*) symType.value);
        public static implicit operator PogoInfo(SymType symType) => new PogoInfo((POGOINFO*) symType.value);
        public static implicit operator ProcSym16(SymType symType) => new ProcSym16((PROCSYM16*) symType.value);
        public static implicit operator ProcSym32(SymType symType) => new ProcSym32((PROCSYM32*) symType.value);
        public static implicit operator ProcSym3216t(SymType symType) => new ProcSym3216t((PROCSYM32_16t*) symType.value);
        public static implicit operator ProcSymIA64(SymType symType) => new ProcSymIA64((PROCSYMIA64*) symType.value);
        public static implicit operator ProcSymMips(SymType symType) => new ProcSymMips((PROCSYMMIPS*) symType.value);
        public static implicit operator ProcSymMips16t(SymType symType) => new ProcSymMips16t((PROCSYMMIPS_16t*) symType.value);
        public static implicit operator PubSym32(SymType symType) => new PubSym32((PUBSYM32*) symType.value);
        public static implicit operator RefMiniPdb(SymType symType) => new RefMiniPdb((REFMINIPDB*) symType.value);
        public static implicit operator RefSym(SymType symType) => new RefSym((REFSYM*) symType.value);
        public static implicit operator RefSym2(SymType symType) => new RefSym2((REFSYM2*) symType.value);
        public static implicit operator RegRel16(SymType symType) => new RegRel16((REGREL16*) symType.value);
        public static implicit operator RegRel32(SymType symType) => new RegRel32((REGREL32*) symType.value);
        public static implicit operator RegRel3216t(SymType symType) => new RegRel3216t((REGREL32_16t*) symType.value);
        public static implicit operator RegSym(SymType symType) => new RegSym((REGSYM*) symType.value);
        public static implicit operator RegSym16t(SymType symType) => new RegSym16t((REGSYM_16t*) symType.value);
        public static implicit operator ReturnSym(SymType symType) => new ReturnSym((RETURNSYM*) symType.value);
        public static implicit operator SearchSym(SymType symType) => new SearchSym((SEARCHSYM*) symType.value);
        public static implicit operator SectionSym(SymType symType) => new SectionSym((SECTIONSYM*) symType.value);
        public static implicit operator SepCodeSym(SymType symType) => new SepCodeSym((SEPCODESYM*) symType.value);
        public static implicit operator SLink32(SymType symType) => new SLink32((SLINK32*) symType.value);
        public static implicit operator SlotSym32(SymType symType) => new SlotSym32((SLOTSYM32*) symType.value);
        public static implicit operator ThunkSym16(SymType symType) => new ThunkSym16((THUNKSYM16*) symType.value);
        public static implicit operator ThunkSym32(SymType symType) => new ThunkSym32((THUNKSYM32*) symType.value);
        public static implicit operator TrampolineSym(SymType symType) => new TrampolineSym((TRAMPOLINESYM*) symType.value);
        public static implicit operator UdtSym(SymType symType) => new UdtSym((UDTSYM*) symType.value);
        public static implicit operator UdtSym16t(SymType symType) => new UdtSym16t((UDTSYM_16t*) symType.value);
        public static implicit operator UNameSpace(SymType symType) => new UNameSpace((UNAMESPACE*) symType.value);
    }
}
