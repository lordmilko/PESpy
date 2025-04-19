using System;
using System.Diagnostics;
using System.Text;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    class SymTypeProxy
    {
        private SymType symType;

        public SymTypeProxy(SymType symType)
        {
            this.symType = symType;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public object Value => GetValue(symType);

        internal static object GetValue(in SymType symType)
        {
            switch (symType.rectyp)
            {
                case SYM_ENUM_e.S_COMPILE:
                    return (CFlagSym) symType;

                case SYM_ENUM_e.S_REGISTER_16t:
                    return (RegSym16t) symType;

                case SYM_ENUM_e.S_CONSTANT_16t:
                    return (ConstSym16t) symType;

                case SYM_ENUM_e.S_UDT_16t:
                case SYM_ENUM_e.S_COBOLUDT_16t:
                    return (UdtSym16t) symType;

                case SYM_ENUM_e.S_SSEARCH:
                    return (SearchSym) symType;

                case SYM_ENUM_e.S_END:
                case SYM_ENUM_e.S_SKIP:
                case SYM_ENUM_e.S_CVRESERVE:
                case SYM_ENUM_e.S_ENDARG:
                case SYM_ENUM_e.S_VFTABLE16:
                case SYM_ENUM_e.S_VFTABLE32_16t:
                case SYM_ENUM_e.S_VFTABLE32:
                case SYM_ENUM_e.S_RESERVED1:
                case SYM_ENUM_e.S_RESERVED2:
                case SYM_ENUM_e.S_RESERVED3:
                case SYM_ENUM_e.S_RESERVED4:
                case SYM_ENUM_e.S_LOCAL_2005:
                case SYM_ENUM_e.S_DEFRANGE_2005:
                case SYM_ENUM_e.S_DEFRANGE2_2005:
                case SYM_ENUM_e.S_INLINESITE_END:
                case SYM_ENUM_e.S_PROC_ID_END:
                    return (SymType) symType;

                case SYM_ENUM_e.S_OBJNAME_ST:
                case SYM_ENUM_e.S_OBJNAME:
                    return (ObjNameSym) symType;

                case SYM_ENUM_e.S_MANYREG_16t:
                    return (ManyRegSym16t) symType;

                case SYM_ENUM_e.S_RETURN:
                    return (ReturnSym) symType;

                case SYM_ENUM_e.S_ENTRYTHIS:
                    return (EntryThisSym) symType;

                case SYM_ENUM_e.S_BPREL16:
                    return (BPRelSym16) symType;

                case SYM_ENUM_e.S_LDATA16:
                case SYM_ENUM_e.S_GDATA16:
                case SYM_ENUM_e.S_PUB16:
                    return (DataSym16) symType;

                case SYM_ENUM_e.S_LPROC16:
                case SYM_ENUM_e.S_GPROC16:
                    return (ProcSym16) symType;

                case SYM_ENUM_e.S_THUNK16:
                    return (ThunkSym16) symType;

                case SYM_ENUM_e.S_BLOCK16:
                case SYM_ENUM_e.S_WITH16:
                    return (BlockSym16) symType;

                case SYM_ENUM_e.S_LABEL16:
                    return (LabelSym16) symType;

                case SYM_ENUM_e.S_CEXMODEL16:
                    return (CExMSym16) symType;

                case SYM_ENUM_e.S_REGREL16:
                    return (RegRel16) symType;

                case SYM_ENUM_e.S_BPREL32_16t:
                    return (BPRelSym3216t) symType;

                case SYM_ENUM_e.S_LDATA32_16t:
                case SYM_ENUM_e.S_GDATA32_16t:
                case SYM_ENUM_e.S_PUB32_16t:
                case SYM_ENUM_e.S_LTHREAD32_16t:
                case SYM_ENUM_e.S_GTHREAD32_16t:
                    return (DataSym3216t) symType;

                case SYM_ENUM_e.S_LPROC32_16t:
                case SYM_ENUM_e.S_GPROC32_16t:
                    return (ProcSym3216t) symType;

                case SYM_ENUM_e.S_THUNK32_ST:
                case SYM_ENUM_e.S_THUNK32:
                    return (ThunkSym32) symType;

                case SYM_ENUM_e.S_BLOCK32_ST:
                case SYM_ENUM_e.S_WITH32_ST:
                case SYM_ENUM_e.S_BLOCK32:
                case SYM_ENUM_e.S_WITH32:
                    return (BlockSym32) symType;

                case SYM_ENUM_e.S_LABEL32_ST:
                case SYM_ENUM_e.S_LABEL32:
                    return (LabelSym32) symType;

                case SYM_ENUM_e.S_CEXMODEL32:
                    return (CExMSym32) symType;

                case SYM_ENUM_e.S_REGREL32_16t:
                    return (RegRel3216t) symType;

                case SYM_ENUM_e.S_SLINK32:
                    return (SLink32) symType;

                case SYM_ENUM_e.S_LPROCMIPS_16t:
                case SYM_ENUM_e.S_GPROCMIPS_16t:
                    return (ProcSymMips16t) symType;

                case SYM_ENUM_e.S_PROCREF_ST:
                case SYM_ENUM_e.S_DATAREF_ST:
                case SYM_ENUM_e.S_LPROCREF_ST:
                    return (RefSym) symType;

                case SYM_ENUM_e.S_ALIGN:
                    return (AlignSym) symType;

                case SYM_ENUM_e.S_OEM:
                    return (OemSymbol) symType;

                case SYM_ENUM_e.S_REGISTER_ST:
                case SYM_ENUM_e.S_REGISTER:
                    return (RegSym) symType;

                case SYM_ENUM_e.S_CONSTANT_ST:
                case SYM_ENUM_e.S_CONSTANT:
                case SYM_ENUM_e.S_MANCONSTANT:
                    return (ConstSym) symType;

                case SYM_ENUM_e.S_UDT_ST:
                case SYM_ENUM_e.S_COBOLUDT_ST:
                case SYM_ENUM_e.S_UDT:
                case SYM_ENUM_e.S_COBOLUDT:
                    return (UdtSym) symType;

                case SYM_ENUM_e.S_MANYREG_ST:
                case SYM_ENUM_e.S_MANYREG:
                    return (ManyRegSym) symType;

                case SYM_ENUM_e.S_BPREL32_ST:
                case SYM_ENUM_e.S_BPREL32:
                    return (BPRelSym32) symType;

                case SYM_ENUM_e.S_LDATA32_ST:
                case SYM_ENUM_e.S_GDATA32_ST:
                case SYM_ENUM_e.S_LTHREAD32_ST:
                case SYM_ENUM_e.S_GTHREAD32_ST:
                case SYM_ENUM_e.S_LMANDATA_ST:
                case SYM_ENUM_e.S_GMANDATA_ST:
                case SYM_ENUM_e.S_LDATA32:
                case SYM_ENUM_e.S_GDATA32:
                case SYM_ENUM_e.S_LTHREAD32:
                case SYM_ENUM_e.S_GTHREAD32:
                case SYM_ENUM_e.S_LMANDATA:
                case SYM_ENUM_e.S_GMANDATA:
                    return (DataSym32) symType;

                case SYM_ENUM_e.S_PUB32_ST:
                case SYM_ENUM_e.S_PUB32:
                    return (PubSym32) symType;

                case SYM_ENUM_e.S_LPROC32_ST:
                case SYM_ENUM_e.S_GPROC32_ST:
                case SYM_ENUM_e.S_LPROC32:
                case SYM_ENUM_e.S_GPROC32:
                case SYM_ENUM_e.S_LPROC32_ID:
                case SYM_ENUM_e.S_GPROC32_ID:
                case SYM_ENUM_e.S_LPROC32_DPC:
                case SYM_ENUM_e.S_LPROC32_DPC_ID:
                    return (ProcSym32) symType;

                case SYM_ENUM_e.S_REGREL32_ST:
                case SYM_ENUM_e.S_REGREL32:
                    return (RegRel32) symType;

                case SYM_ENUM_e.S_LPROCMIPS_ST:
                case SYM_ENUM_e.S_GPROCMIPS_ST:
                case SYM_ENUM_e.S_LPROCMIPS:
                case SYM_ENUM_e.S_GPROCMIPS:
                case SYM_ENUM_e.S_LPROCMIPS_ID:
                case SYM_ENUM_e.S_GPROCMIPS_ID:
                    return (ProcSymMips) symType;

                case SYM_ENUM_e.S_FRAMEPROC:
                    return (FrameProcSym) symType;

                case SYM_ENUM_e.S_COMPILE2_ST:
                case SYM_ENUM_e.S_COMPILE2:
                    return (CompileSym) symType;

                case SYM_ENUM_e.S_MANYREG2_ST:
                case SYM_ENUM_e.S_MANYREG2:
                    return (ManyRegSym2) symType;

                case SYM_ENUM_e.S_LPROCIA64_ST:
                case SYM_ENUM_e.S_GPROCIA64_ST:
                case SYM_ENUM_e.S_LPROCIA64:
                case SYM_ENUM_e.S_GPROCIA64:
                case SYM_ENUM_e.S_LPROCIA64_ID:
                case SYM_ENUM_e.S_GPROCIA64_ID:
                    return (ProcSymIA64) symType;

                case SYM_ENUM_e.S_LOCALSLOT_ST:
                case SYM_ENUM_e.S_PARAMSLOT_ST:
                case SYM_ENUM_e.S_LOCALSLOT:
                case SYM_ENUM_e.S_PARAMSLOT:
                    return (SlotSym32) symType;

                case SYM_ENUM_e.S_ANNOTATION:
                    return (AnnotationSym) symType;

                case SYM_ENUM_e.S_GMANPROC_ST:
                case SYM_ENUM_e.S_LMANPROC_ST:
                case SYM_ENUM_e.S_GMANPROC:
                case SYM_ENUM_e.S_LMANPROC:
                    return (ManProcSym) symType;

                case SYM_ENUM_e.S_MANFRAMEREL_ST:
                case SYM_ENUM_e.S_MANFRAMEREL:
                case SYM_ENUM_e.S_ATTR_FRAMEREL:
                    return (FrameRelSym) symType;

                case SYM_ENUM_e.S_MANREGISTER_ST:
                case SYM_ENUM_e.S_MANREGISTER:
                case SYM_ENUM_e.S_ATTR_REGISTER:
                    return (AttrRegSym) symType;

                case SYM_ENUM_e.S_MANSLOT_ST:
                case SYM_ENUM_e.S_MANSLOT:
                    return (AttrSlotSym) symType;

                case SYM_ENUM_e.S_MANMANYREG_ST:
                case SYM_ENUM_e.S_MANMANYREG:
                    throw new NotImplementedException(); //todo

                case SYM_ENUM_e.S_MANREGREL_ST:
                case SYM_ENUM_e.S_MANREGREL:
                case SYM_ENUM_e.S_ATTR_REGREL:
                    return (AttrRegRel) symType;

                case SYM_ENUM_e.S_MANMANYREG2_ST:
                case SYM_ENUM_e.S_MANMANYREG2:
                    throw new NotImplementedException(); //todo

                case SYM_ENUM_e.S_MANTYPREF:
                    return (ManTypRef) symType;

                case SYM_ENUM_e.S_UNAMESPACE_ST:
                case SYM_ENUM_e.S_UNAMESPACE:
                    return (UNameSpace) symType;

                case SYM_ENUM_e.S_PROCREF:
                case SYM_ENUM_e.S_DATAREF:
                case SYM_ENUM_e.S_LPROCREF:
                case SYM_ENUM_e.S_ANNOTATIONREF:
                case SYM_ENUM_e.S_TOKENREF:
                    return (RefSym2) symType;

                case SYM_ENUM_e.S_TRAMPOLINE:
                    return (TrampolineSym) symType;

                case SYM_ENUM_e.S_ATTR_MANYREG:
                    return (AttrManyRegSym2) symType;

                case SYM_ENUM_e.S_SEPCODE:
                    return (SepCodeSym) symType;

                case SYM_ENUM_e.S_SECTION:
                    return (SectionSym) symType;

                case SYM_ENUM_e.S_COFFGROUP:
                    return (CoffGroupSym) symType;

                case SYM_ENUM_e.S_EXPORT:
                    return (ExportSym) symType;

                case SYM_ENUM_e.S_CALLSITEINFO:
                    return (CallSiteInfo) symType;

                case SYM_ENUM_e.S_FRAMECOOKIE:
                    return (FrameCookie) symType;

                case SYM_ENUM_e.S_DISCARDED:
                    return (DiscardedSym) symType;

                case SYM_ENUM_e.S_COMPILE3:
                    return (CompileSym3) symType;

                case SYM_ENUM_e.S_ENVBLOCK:
                    return (EnvBlockSym) symType;

                case SYM_ENUM_e.S_LOCAL:
                    return (LocalSym) symType;

                case SYM_ENUM_e.S_DEFRANGE:
                    return (DefRangeSym) symType;

                case SYM_ENUM_e.S_DEFRANGE_REGISTER:
                    return (DefRangeSymRegister) symType;

                case SYM_ENUM_e.S_DEFRANGE_FRAMEPOINTER_REL:
                    return (DefRangeSymFramePointerRel) symType;

                case SYM_ENUM_e.S_DEFRANGE_FRAMEPOINTER_REL_FULL_SCOPE:
                    return (DefRangeSymFramePointerRelFullScope) symType;

                case SYM_ENUM_e.S_DEFRANGE_SUBFIELD:
                    return (DefRangeSymSubField) symType;

                case SYM_ENUM_e.S_DEFRANGE_SUBFIELD_REGISTER:
                    return (DefRangeSymSubfieldRegister) symType;

                case SYM_ENUM_e.S_DEFRANGE_REGISTER_REL:
                    return (DefRangeSymRegisterRel) symType;

                case SYM_ENUM_e.S_BUILDINFO:
                    return (BuildInfoSym) symType;

                case SYM_ENUM_e.S_INLINESITE:
                    return (InlineSiteSym) symType;

                case SYM_ENUM_e.S_DEFRANGE_HLSL:
                case SYM_ENUM_e.S_DEFRANGE_DPC_PTR_TAG:
                    return (DefRangeSymHLSL) symType;

                case SYM_ENUM_e.S_GDATA_HLSL:
                case SYM_ENUM_e.S_LDATA_HLSL:
                    return (DataSymHLSL) symType;

                case SYM_ENUM_e.S_FILESTATIC:
                    return (FileStaticSym) symType;

                case SYM_ENUM_e.S_LOCAL_DPC_GROUPSHARED:
                    return (LocalDPCGroupSharedSym) symType;

                case SYM_ENUM_e.S_DPC_SYM_TAG_MAP:
                    return (DPCSymTagMap) symType;

                case SYM_ENUM_e.S_ARMSWITCHTABLE:
                    return (ArmSwitchTable) symType;

                case SYM_ENUM_e.S_CALLEES:
                case SYM_ENUM_e.S_CALLERS:
                    return (FunctionList) symType;

                case SYM_ENUM_e.S_POGODATA:
                    return (PogoInfo) symType;

                case SYM_ENUM_e.S_INLINESITE2:
                    return (InlineSiteSym2) symType;

                case SYM_ENUM_e.S_HEAPALLOCSITE:
                    return (HeapAllocSite) symType;

                case SYM_ENUM_e.S_MOD_TYPEREF:
                    return (ModTypeRef) symType;

                case SYM_ENUM_e.S_REF_MINIPDB:
                    return (RefMiniPdb) symType;

                case SYM_ENUM_e.S_PDBMAP:
                    return (PdbMap) symType;

                case SYM_ENUM_e.S_GDATA_HLSL32:
                case SYM_ENUM_e.S_LDATA_HLSL32:
                    return (DataSymHLSL32) symType;

                case SYM_ENUM_e.S_GDATA_HLSL32_EX:
                case SYM_ENUM_e.S_LDATA_HLSL32_EX:
                    return (DataSymHLSL32Ex) symType;

                //Known unsupported types

                case SYM_ENUM_e.S_FRAMEREG:
                case SYM_ENUM_e.S_REF_MINIPDB2:
                case SYM_ENUM_e.S_INLINEES:
                case SYM_ENUM_e.S_HOTPATCHFUNC:
                case SYM_ENUM_e.S_BPREL32_INDIR:
                case SYM_ENUM_e.S_REGREL32_INDIR:
                case SYM_ENUM_e.S_GPROC32EX:
                case SYM_ENUM_e.S_LPROC32EX:
                case SYM_ENUM_e.S_GPROC32EX_ID:
                case SYM_ENUM_e.S_LPROC32EX_ID:
                case SYM_ENUM_e.S_STATICLOCAL:
                case SYM_ENUM_e.S_DEFRANGE_REGISTER_REL_INDIR:
                case SYM_ENUM_e.S_BPREL32_ENCTMP:
                case SYM_ENUM_e.S_REGREL32_ENCTMP:
                case SYM_ENUM_e.S_BPREL32_INDIR_ENCTMP:
                case SYM_ENUM_e.S_REGREL32_INDIR_ENCTMP:
                    return (SymType) symType;

                default:
                    Debug.Assert(false);
                    return (SymType) symType;
            }
        }

        public static string GetString(SymType symType)
        {
            var underlying = GetValue(symType);

            //SymType.ToString() calls back into GetString again, so we need to special case bail out
            //if there wasn't a more specialized SymType implementation
            if (underlying is SymType s)
                return s.rectyp.ToString();

            return underlying.ToString();
        }

        public static string DebuggerDisplay(SymType symType)
        {
            var builder = new StringBuilder();
            builder.Append("[").Append(symType.rectyp).Append("]");

            var value = GetValue(symType);

            var defaultStr = symType.rectyp.ToString();

            var str = value.ToString();

            if (defaultStr != str)
                builder.Append(" ").Append(str);

            return builder.ToString();
        }
    }
}
