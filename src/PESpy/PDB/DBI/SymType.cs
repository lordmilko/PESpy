using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using ClrDebug;
using ClrDebug.DIA;
using ClrDebug.PDB;
using static ClrDebug.PDB.SYM_ENUM_e;

namespace PESpy.PDB
{
    public static class SymTypeExtensions
    {
        //This method _does_ traverse ref symbols
        public static unsafe bool TryGetRVA(in this SymType symType, out int rva)
        {
            if (TryGetOffSeg(symType, out var off, out var seg))
            {
                var rawRva = SymType.GetRelativeVirtualAddress((SYMTYPE*) symType, seg, off);

                //Data symbols can have a section index of 0, indicating they don't physically exist
                if (rawRva != null)
                {
                    rva = rawRva.Value;
                    return true;
                }
            }

            rva = default;
            return false;
        }

        public static bool TryGetName(in this SymType symType, out FixedUtf8String name)
        {
            switch (symType.rectyp)
            {
                case S_MANREGREL_ST:
                case S_MANREGREL:
                case S_ATTR_REGREL:
                    name = ((AttrRegRel) symType).name;
                    return true;

                case S_MANREGISTER_ST:
                case S_MANREGISTER:
                case S_ATTR_REGISTER:
                    name = ((AttrRegSym) symType).name;
                    return true;

                case S_MANSLOT_ST:
                case S_MANSLOT:
                    name = ((AttrSlotSym) symType).name;
                    return true;

                case S_BLOCK16:
                case S_WITH16:
                    name = ((BlockSym16) symType).name;
                    return true;

                case S_BLOCK32_ST:
                case S_WITH32_ST:
                case S_BLOCK32:
                case S_WITH32:
                    name = ((BlockSym32) symType).name;
                    return true;

                case S_BPREL16:
                    name = ((BPRelSym16) symType).name;
                    return true;

                case S_BPREL32_ST:
                case S_BPREL32:
                    name = ((BPRelSym32) symType).name;
                    return true;

                case S_BPREL32_16t:
                    name = ((BPRelSym3216t) symType).name;
                    return true;

                //case S_COMPILE:
                //    name = ((CFlagSym) symType).name;
                //    return true;

                case S_COFFGROUP:
                    name = ((CoffGroupSym) symType).name;
                    return true;

                //case S_COMPILE2_ST:
                //case S_COMPILE2:
                //    name = ((CompileSym) symType).name;
                //    return true;

                //case S_COMPILE3:
                //    name = ((CompileSym3) symType).name;
                //    return true;

                case S_CONSTANT_ST:
                case S_CONSTANT:
                case S_MANCONSTANT:
                    name = ((ConstSym) symType).name;
                    return true;

                case S_CONSTANT_16t:
                    name = ((ConstSym16t) symType).name;
                    return true;

                case S_LDATA16:
                case S_GDATA16:
                case S_PUB16:
                    name = ((DataSym16) symType).name;
                    return true;

                case S_LDATA32_ST:
                case S_GDATA32_ST:
                case S_LTHREAD32_ST:
                case S_GTHREAD32_ST:
                case S_LMANDATA_ST:
                case S_GMANDATA_ST:
                case S_LDATA32:
                case S_GDATA32:
                case S_LTHREAD32:
                case S_GTHREAD32:
                case S_LMANDATA:
                case S_GMANDATA:
                    name = ((DataSym32) symType).name;
                    return true;

                case S_LDATA32_16t:
                case S_GDATA32_16t:
                case S_PUB32_16t:
                case S_LTHREAD32_16t:
                case S_GTHREAD32_16t:
                    name = ((DataSym3216t) symType).name;
                    return true;

                case S_GDATA_HLSL:
                case S_LDATA_HLSL:
                    name = ((DataSymHLSL) symType).name;
                    return true;

                //DataSymHLSL32
                //DataSymHLSL32Ex

                case S_EXPORT:
                    name = ((ExportSym) symType).name;
                    return true;

                case S_FILESTATIC:
                    name = ((FileStaticSym) symType).name;
                    return true;

                case S_MANFRAMEREL_ST:
                case S_MANFRAMEREL:
                case S_ATTR_FRAMEREL:
                    name = ((FrameRelSym) symType).name;
                    return true;

                case S_LABEL16:
                    name = ((LabelSym16) symType).name;
                    return true;

                case S_LABEL32_ST:
                case S_LABEL32:
                    name = ((LabelSym32) symType).name;
                    return true;

                case S_LOCAL_DPC_GROUPSHARED:
                    name = ((LocalDPCGroupSharedSym) symType).name;
                    return true;

                case S_LOCAL:
                    name = ((LocalSym) symType).name;
                    return true;

                case S_GMANPROC_ST:
                case S_LMANPROC_ST:
                case S_GMANPROC:
                case S_LMANPROC:
                    name = ((ManProcSym) symType).name;
                    return true;

                case S_OBJNAME_ST:
                case S_OBJNAME:
                    name = ((ObjNameSym) symType).name;
                    return true;

                case S_PDBMAP:
                    name = ((PdbMap) symType).name;
                    return true;

                case S_LPROC16:
                case S_GPROC16:
                    name = ((ProcSym16) symType).name;
                    return true;

                case S_LPROC32_ST:
                case S_GPROC32_ST:
                case S_LPROC32:
                case S_GPROC32:
                case S_LPROC32_ID:
                case S_GPROC32_ID:
                case S_LPROC32_DPC:
                case S_LPROC32_DPC_ID:
                    name = ((ProcSym32) symType).name;
                    return true;

                case S_LPROC32_16t:
                case S_GPROC32_16t:
                    name = ((ProcSym3216t) symType).name;
                    return true;

                case S_LPROCIA64_ST:
                case S_GPROCIA64_ST:
                case S_LPROCIA64:
                case S_GPROCIA64:
                case S_LPROCIA64_ID:
                case S_GPROCIA64_ID:
                    name = ((ProcSymIA64) symType).name;
                    return true;

                case S_LPROCMIPS_ST:
                case S_GPROCMIPS_ST:
                case S_LPROCMIPS:
                case S_GPROCMIPS:
                case S_LPROCMIPS_ID:
                case S_GPROCMIPS_ID:
                    name = ((ProcSymMips) symType).name;
                    return true;

                case S_LPROCMIPS_16t:
                case S_GPROCMIPS_16t:
                    name = ((ProcSymMips16t) symType).name;
                    return true;

                case S_PUB32_ST:
                case S_PUB32:
                    name = ((PubSym32) symType).name;
                    return true;

                case S_REF_MINIPDB:
                    name = ((RefMiniPdb) symType).name;
                    return true;

                case S_PROCREF_ST:
                case S_DATAREF_ST:
                case S_LPROCREF_ST:
                    name = ((RefSym) symType).name;
                    return true;

                case S_PROCREF:
                case S_DATAREF:
                case S_LPROCREF:
                case S_ANNOTATIONREF:
                case S_TOKENREF:
                    name = ((RefSym2) symType).name;
                    return true;

                case S_REGREL16:
                    name = ((RegRel16) symType).name;
                    return true;

                case S_REGREL32_ST:
                case S_REGREL32:
                    name = ((RegRel32) symType).name;
                    return true;

                case S_REGREL32_16t:
                    name = ((RegRel3216t) symType).name;
                    return true;

                case S_REGISTER_ST:
                case S_REGISTER:
                    name = ((RegSym) symType).name;
                    return true;

                case S_REGISTER_16t:
                    name = ((RegSym16t) symType).name;
                    return true;

                case S_SECTION:
                    name = ((SectionSym) symType).name;
                    return true;

                case S_LOCALSLOT_ST:
                case S_PARAMSLOT_ST:
                case S_LOCALSLOT:
                case S_PARAMSLOT:
                    name = ((SlotSym32) symType).name;
                    return true;

                case S_THUNK32_ST:
                case S_THUNK32:
                    name = ((ThunkSym32) symType).name;
                    return true;

                case S_UDT_ST:
                case S_COBOLUDT_ST:
                case S_UDT:
                case S_COBOLUDT:
                    name = ((UdtSym) symType).name;
                    return true;

                case S_UDT_16t:
                case S_COBOLUDT_16t:
                    name = ((UdtSym16t) symType).name;
                    return true;

                case S_UNAMESPACE_ST:
                case S_UNAMESPACE:
                    name = ((UNameSpace) symType).name;
                    return true;

                default:
                    name = default;
                    return false;
            }
        }

        public static bool TryGetOffSeg(in this SymType symType, out int off, out ushort seg)
        {
            //The following symbol kinds have a "seg" member which indicates they may store an RVA

            switch (symType.rectyp)
            {
                case S_ANNOTATION:
                {
                    var sym = ((AnnotationSym) symType);
                    off = sym.off;
                    seg = sym.seg;
                    return true;
                }

                case S_BLOCK16:
                case S_WITH16:
                {
                    var sym = ((BlockSym16) symType);
                    off = sym.off;
                    seg = sym.seg;
                    return true;
                }

                case S_BLOCK32_ST:
                case S_WITH32_ST:
                case S_BLOCK32:
                case S_WITH32:
                {
                    var sym = ((BlockSym32) symType);
                    off = sym.off;
                    seg = sym.seg;
                    return true;
                }

                case S_CEXMODEL16:
                {
                    var sym = ((CExMSym16) symType);
                    off = sym.off;
                    seg = sym.seg;
                    return true;
                }

                case S_CEXMODEL32:
                {
                    var sym = ((CExMSym32) symType);
                    off = sym.off;
                    seg = sym.seg;
                    return true;
                }

                case S_COFFGROUP:
                {
                    var sym = ((CoffGroupSym) symType);
                    off = sym.off;
                    seg = sym.seg;
                    return true;
                }

                case S_LDATA16:
                case S_GDATA16:
                case S_PUB16:
                {
                    var sym = ((DataSym16) symType);
                    off = sym.off;
                    seg = sym.seg;
                    return true;
                }

                case S_LDATA32_ST:
                case S_GDATA32_ST:
                case S_LTHREAD32_ST:
                case S_GTHREAD32_ST:
                case S_LMANDATA_ST:
                case S_GMANDATA_ST:
                case S_LDATA32:
                case S_GDATA32:
                case S_LTHREAD32:
                case S_GTHREAD32:
                case S_LMANDATA:
                case S_GMANDATA:
                {
                    var sym = ((DataSym32) symType);
                    off = sym.off;
                    seg = sym.seg;
                    return true;
                }

                case S_LDATA32_16t:
                case S_GDATA32_16t:
                case S_PUB32_16t:
                case S_LTHREAD32_16t:
                case S_GTHREAD32_16t:
                {
                    var sym = ((DataSym3216t) symType);
                    off = sym.off;
                    seg = sym.seg;
                    return true;
                }

                case S_LABEL16:
                {
                    var sym = ((LabelSym16) symType);
                    off = sym.off;
                    seg = sym.seg;
                    return true;
                }

                case S_LABEL32_ST:
                case S_LABEL32:
                {
                    var sym = ((LabelSym32) symType);
                    off = sym.off;
                    seg = sym.seg;
                    return true;
                }

                case S_GMANPROC_ST:
                case S_LMANPROC_ST:
                case S_GMANPROC:
                case S_LMANPROC:
                {
                    var sym = ((ManProcSym) symType);
                    off = sym.off;
                    seg = sym.seg;
                    return true;
                }

                case S_LPROC16:
                case S_GPROC16:
                {
                    var sym = ((ProcSym16) symType);
                    off = sym.off;
                    seg = sym.seg;
                    return true;
                }

                case S_LPROC32_ST:
                case S_GPROC32_ST:
                case S_LPROC32:
                case S_GPROC32:
                case S_LPROC32_ID:
                case S_GPROC32_ID:
                case S_LPROC32_DPC:
                case S_LPROC32_DPC_ID:
                {
                    var sym = ((ProcSym32) symType);
                    off = sym.off;
                    seg = sym.seg;
                    return true;
                }

                case S_LPROC32_16t:
                case S_GPROC32_16t:
                {
                    var sym = ((ProcSym3216t) symType);
                    off = sym.off;
                    seg = sym.seg;
                    return true;
                }

                case S_LPROCIA64_ST:
                case S_GPROCIA64_ST:
                case S_LPROCIA64:
                case S_GPROCIA64:
                case S_LPROCIA64_ID:
                case S_GPROCIA64_ID:
                {
                    var sym = ((ProcSymIA64) symType);
                    off = sym.off;
                    seg = sym.seg;
                    return true;
                }

                case S_LPROCMIPS_ST:
                case S_GPROCMIPS_ST:
                case S_LPROCMIPS:
                case S_GPROCMIPS:
                case S_LPROCMIPS_ID:
                case S_GPROCMIPS_ID:
                {
                    var sym = ((ProcSymMips) symType);
                    off = sym.off;
                    seg = sym.seg;
                    return true;
                }

                case S_LPROCMIPS_16t:
                case S_GPROCMIPS_16t:
                {
                    var sym = ((ProcSymMips16t) symType);
                    off = sym.off;
                    seg = sym.seg;
                    return true;
                }

                case S_PUB32_ST:
                case S_PUB32:
                {
                    var sym = ((PubSym32) symType);
                    off = sym.off;
                    seg = sym.seg;
                    return true;
                }

                //case S_SSEARCH: //SSEARCH has a seg but no off

                case S_THUNK16:
                {
                    var sym = ((ThunkSym16) symType);
                    off = sym.off;
                    seg = sym.seg;
                    return true;
                }

                case S_THUNK32_ST:
                case S_THUNK32:
                {
                    var sym = ((ThunkSym32) symType);
                    off = sym.off;
                    seg = sym.seg;
                    return true;
                }

                //ref symbols don't have a seg, but they may point to something that does!

                case S_PROCREF_ST:
                case S_DATAREF_ST:
                case S_LPROCREF_ST:
                    return ((RefSym) symType).Symbol.TryGetOffSeg(out off, out seg);

                case S_PROCREF:
                case S_DATAREF:
                case S_LPROCREF:
                case S_ANNOTATIONREF:
                case S_TOKENREF:
                    return ((RefSym2) symType).Symbol.TryGetOffSeg(out off, out seg);

                default:
                    off = default;
                    seg = default;
                    return false;
            }
        }

        /// <summary>
        /// Gets whether this symbol represents code.<para/>
        /// A symbol is code if any of the following is true:<para/>
        /// - The symbol is a PROC<para/>
        /// - The symbol is a THUNK
        /// - The symbol is a public with flags set to indicate it is code<para/>
        /// - The symbol is a public in a section with <see cref="IMAGE_SCN.CNT_CODE"/> set
        /// </summary>
        /// <param name="symType">The symbol to inspect</param>
        /// <returns>True if the specified symbol is code. Otherwise, false</returns>
        public static unsafe bool IsCode(in this SymType symType)
        {
            /* From mapping DIA symbols to PDB symbols by RVA, the following symbol kinds have been observed to have code or be functions:
             *
             * S_PUB32
             * S_GPROC32
             * S_LPROC32
             * S_THUNK32
             * S_GDATA32 (at the same address there was also a S_PUB32. It wasn't code or function, but the S_GDATA32 item was in .text which had CNT_CODE)
             * S_LABEL32 (at the same address there was also a S_PUB32)
             * S_COFFGROUP (at the same address there was also an S_PUB32 and an S_THUNK32)
             *
             * So I would say that anything that is a PROC or THUNK could be code
             *
             * In the case of publics, if they report that they're code, it's all good. Otherwise, msdia140!setPubSymFlags calls
             * SymCache::iModFromAddr which calls DBI1::QueryModFromAddr2. This method returns the IMAGE_SCN characteristics of the
             * section contrib that contained the section + offset. DIA then right shifts this 5 to check if IMAGE_SCN_CNT_CODE is set (0x20) */

            if (symType.IsProc() || symType.IsThunk())
                return true;

            ushort seg;
            int off;

            switch (symType.rectyp)
            {
                case S_PUB16:
                {
                    var value = (DataSym16) symType;
                    seg = value.seg;
                    off = value.off;
                    break;
                }

                case S_PUB32:
                case S_PUB32_ST:
                {
                    var value = (PubSym32) symType;

                    //todo: i dont know which flags should be used for saying "its code". e.g. could you have function but not code?
                    //similarly, what if its managed/msil?
                    if (value.pubsymflags.fCode || value.pubsymflags.fFunction)
                        return true;

                    seg = value.seg;
                    off = value.off;
                    break;
                }

                case S_PUB32_16t:
                {
                    var value = (DataSym3216t) symType;
                    seg = value.seg;
                    off = value.off;
                    break;
                }

                default:
                    return false;
            }

            if (SymType.TryGetSectionContrib(symType, seg, off, out var sc))
            {
                if ((sc.dwCharacteristics & ClrDebug.IMAGE_SCN.CNT_CODE) != 0)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Gets whether a <see cref="SymType"/> contains a "proc" symbol, representing a procedure.<para/>
        /// This method does not traverse ref symbols.
        /// </summary>
        /// <param name="symType">The symbol to inspect.</param>
        /// <returns>True if the symbol is some type of procedure. Otherwise, false.</returns>
        public static bool IsProc(in this SymType symType)
        {
            switch (symType.rectyp)
            {
                //ProcSym16
                case S_LPROC16:
                case S_GPROC16:

                //ProcSym3216t
                case S_LPROC32_16t:
                case S_GPROC32_16t:

                //ProcSymMips16t
                case S_LPROCMIPS_16t:
                case S_GPROCMIPS_16t:

                //ProcSym32
                case S_LPROC32_ST:
                case S_GPROC32_ST:
                case S_LPROC32:
                case S_GPROC32:
                case S_LPROC32_ID:
                case S_GPROC32_ID:
                case S_LPROC32_DPC:
                case S_LPROC32_DPC_ID:

                //ProcSymMips
                case S_LPROCMIPS_ST:
                case S_GPROCMIPS_ST:
                case S_LPROCMIPS:
                case S_GPROCMIPS:
                case S_LPROCMIPS_ID:
                case S_GPROCMIPS_ID:

                //Not sure if FRAMEPROC should be included

                //ProcSymIA64
                case S_LPROCIA64_ST:
                case S_GPROCIA64_ST:
                case S_LPROCIA64:
                case S_GPROCIA64:
                case S_LPROCIA64_ID:
                case S_GPROCIA64_ID:

                //ManProcSym
                case S_GMANPROC_ST:
                case S_LMANPROC_ST:
                case S_GMANPROC:
                case S_LMANPROC:

                //Unsupported
                case S_GPROC32EX:
                case S_LPROC32EX:
                case S_GPROC32EX_ID:
                case S_LPROC32EX_ID:
                    return true;

                //All non-ST and 16-bit items map to SymTagFunction. Add any new items
                //to SymTagFunction as well

                default:
                    return false;
            }
        }

        /// <summary>
        /// Gets whether a <see cref="SymType"/> contains a "thunk" symbol.<para/>
        /// This method does not traverse ref symbols.
        /// </summary>
        /// <param name="symType">The symbol to inspect.</param>
        /// <returns>True if the symbol is some type of thunk. Otherwise, false.</returns>
        public static bool IsThunk(in this SymType symType)
        {
            switch (symType.rectyp)
            {
                //ThunkSym16
                case S_THUNK16:

                //ThunkSym32
                case S_THUNK32_ST:
                case S_THUNK32:
                    return true;

                default:
                    return false;
            }
        }

        public static bool IsRef(in this SymType symType)
        {
            switch (symType.rectyp)
            {
                //RefSym
                case S_PROCREF_ST:
                case S_DATAREF_ST:
                case S_LPROCREF_ST:

                //RefSym2
                case S_PROCREF:
                case S_DATAREF:
                case S_LPROCREF:
                case S_ANNOTATIONREF:
                case S_TOKENREF:
                    return true;

                default:
                    return false;
            }
        }

        public static SymTagEnum GetSymTagEnum(in this SymType symType)
        {
            /* When msdia140 dispatches symbols, they are sent to either a SymbolDataSimpleImpl<> or
             * SymbolDataGeneralImpl<> type. SimpleImpl types are instantiated using the SYM_ENUM_e and SymTagEnum,
             * whereas GeneralImpl types take a particular structure and SYM_ENUM_e and then either conditionally return
             * different SymTagEnum values based on the contents of the passed in structure, or in some cases return a hardcoded
             * value. In the second slot of each impl's vtable, is a method symTag(), however due to COMDAT folding not all symTag methods may be visible
             * in msdia140's symbols. By disassembling the methods pointed to by each impl type, we can see the possible
             * SymTagEnum values that may be used for the given symbol type
             *
             * The following table describes each impl type, the types described by its generic signature, and what its symTag method does
             *
             * | Type                                     | SYM_ENUM_e (Sig)         | SymTagEnum (Sig)         | Struct (Sig)  | symTag() |
             * |------------------------------------------|--------------------------|--------------------------|---------------|----------|
             * Compiland (2)
             * =========================
             *
             * CompilandDetails (3)
             * =========================
             * | SymbolDataSimpleImpl<1,3>                 | S_COMPILE               | SymTagCompilandDetails   |               | SymTagCompilandDetails
             * | SymbolDataGeneralImpl<COMPILESYM3,4412>   | S_COMPILE3              |                          | COMPILESYM3   | SymTagCompilandDetails
             * | SymbolDataGeneralImpl<COMPILESYM,4374>    | S_COMPILE2              |                          | COMPILESYM    | SymTagCompilandDetails / SymTagCompilandEnv
             *
             * CompilandEnv (4)
             * =========================
             * | SymbolDataSimpleImpl<4353,4>              | S_OBJNAME               | SymTagCompilandEnv       |               | SymTagCompilandEnv
             * | SymbolDataGeneralImpl<BUILDINFOSYM,4428>  | S_BUILDINFO             |                          | BUILDINFOSYM  | SymTagCompilandEnv
             * | SymbolDataGeneralImpl<ENVBLOCKSYM,4413>   | S_ENVBLOCK              |                          | ENVBLOCKSYM   | SymTagCompilandEnv
             *
             * Function (5)
             * =========================
             * | SymbolDataGeneralImpl<PROCSYM32,4367>     | S_LPROC32               |                          | PROCSYM32     | SymTagFunction / SymTagFuncDebugStart / SymTagFuncDebugEnd
             * | SymbolDataGeneralImpl<PROCSYM32,4368>     | S_GPROC32               |                          | PROCSYM32     | SymTagFunction / SymTagFuncDebugStart / SymTagFuncDebugEnd
             * | SymbolDataGeneralImpl<PROCSYMMIPS,4372>   | S_LPROCMIPS             |                          | PROCSYMMIPS   | SymTagFunction / SymTagFuncDebugStart / SymTagFuncDebugEnd
             * | SymbolDataGeneralImpl<PROCSYMMIPS,4373>   | S_GPROCMIPS             |                          | PROCSYMMIPS   | SymTagFunction / SymTagFuncDebugStart / SymTagFuncDebugEnd
             * | SymbolDataGeneralImpl<PROCSYMIA64,4376>   | S_LPROCIA64             |                          | PROCSYMIA64   | SymTagFunction / SymTagFuncDebugStart / SymTagFuncDebugEnd
             * | SymbolDataGeneralImpl<PROCSYMIA64,4377>   | S_GPROCIA64             |                          | PROCSYMIA64   | SymTagFunction / SymTagFuncDebugStart / SymTagFuncDebugEnd
             * | SymbolDataGeneralImpl<MANPROCSYM,4394>    | S_GMANPROC              |                          | MANPROCSYM    | SymTagFunction / SymTagFuncDebugStart / SymTagFuncDebugEnd
             * | SymbolDataGeneralImpl<MANPROCSYM,4395>    | S_LMANPROC              |                          | MANPROCSYM    | SymTagFunction / SymTagFuncDebugStart / SymTagFuncDebugEnd
             * | SymbolDataGeneralImpl<PROCSYM32,4422>     | S_LPROC32_ID            |                          | PROCSYM32     | SymTagFunction / SymTagFuncDebugStart / SymTagFuncDebugEnd
             * | SymbolDataGeneralImpl<PROCSYM32,4423>     | S_GPROC32_ID            |                          | PROCSYM32     | SymTagFunction / SymTagFuncDebugStart / SymTagFuncDebugEnd
             * | SymbolDataGeneralImpl<PROCSYMMIPS,4424>   | S_LPROCMIPS_ID          |                          | PROCSYMMIPS   | SymTagFunction / SymTagFuncDebugStart / SymTagFuncDebugEnd
             * | SymbolDataGeneralImpl<PROCSYMMIPS,4425>   | S_GPROCMIPS_ID          |                          | PROCSYMMIPS   | SymTagFunction / SymTagFuncDebugStart / SymTagFuncDebugEnd
             * | SymbolDataGeneralImpl<PROCSYMIA64,4426>   | S_LPROCIA64_ID          |                          | PROCSYMIA64   | SymTagFunction / SymTagFuncDebugStart / SymTagFuncDebugEnd
             * | SymbolDataGeneralImpl<PROCSYMIA64,4427>   | S_GPROCIA64_ID          |                          | PROCSYMIA64   | SymTagFunction / SymTagFuncDebugStart / SymTagFuncDebugEnd
             * | SymbolDataGeneralImpl<PROCSYM32,4437>     | S_LPROC32_DPC           |                          | PROCSYM32     | SymTagFunction / SymTagFuncDebugStart / SymTagFuncDebugEnd
             * | SymbolDataGeneralImpl<PROCSYM32,4438>     | S_LPROC32_DPC_ID        |                          | PROCSYM32     | SymTagFunction / SymTagFuncDebugStart / SymTagFuncDebugEnd
             * | SymbolDataGeneralImpl<PROCSYM32EX,4466>   | S_GPROC32EX             |                          | PROCSYM32EX   | SymTagFunction / SymTagFuncDebugStart / SymTagFuncDebugEnd
             * | SymbolDataGeneralImpl<PROCSYM32EX,4467>   | S_LPROC32EX             |                          | PROCSYM32EX   | SymTagFunction / SymTagFuncDebugStart / SymTagFuncDebugEnd
             * | SymbolDataGeneralImpl<PROCSYM32EX,4468>   | S_GPROC32EX_ID          |                          | PROCSYM32EX   | SymTagFunction / SymTagFuncDebugStart / SymTagFuncDebugEnd
             * | SymbolDataGeneralImpl<PROCSYM32EX,4469>   | S_LPROC32EX_ID          |                          | PROCSYM32EX   | SymTagFunction / SymTagFuncDebugStart / SymTagFuncDebugEnd
             *
             * Block (6)
             * =========================
             * | SymbolDataSimpleImpl<4355,6>              | S_BLOCK32               | SymTagBlock              |               | SymTagBlock
             * | SymbolDataSimpleImpl<4402,6>              | S_SEPCODE               | SymTagBlock              |               | SymTagBlock
             *
             * Data (7)
             * =========================
             * | SymbolDataSimpleImpl<4358,7>              | S_REGISTER              | SymTagData               |               | SymTagData
             * | SymbolDataSimpleImpl<4359,7>              | S_CONSTANT              | SymTagData               |               | SymTagData
             * | SymbolDataSimpleImpl<4362,7>              | S_MANYREG               | SymTagData               |               | SymTagData
             * | SymbolDataSimpleImpl<4363,7>              | S_BPREL32               | SymTagData               |               | SymTagData
             * | SymbolDataSimpleImpl<4364,7>              | S_LDATA32               | SymTagData               |               | SymTagData
             * | SymbolDataSimpleImpl<4365,7>              | S_GDATA32               | SymTagData               |               | SymTagData
             * | SymbolDataSimpleImpl<4369,7>              | S_REGREL32              | SymTagData               |               | SymTagData
             * | SymbolDataSimpleImpl<4370,7>              | S_LTHREAD32             | SymTagData               |               | SymTagData
             * | SymbolDataSimpleImpl<4371,7>              | S_GTHREAD32             | SymTagData               |               | SymTagData
             * | SymbolDataSimpleImpl<4375,7>              | S_MANYREG2              | SymTagData               |               | SymTagData
             * | SymbolDataSimpleImpl<4378,7>              | S_LOCALSLOT             | SymTagData               |               | SymTagData
             * | SymbolDataSimpleImpl<4379,7>              | S_PARAMSLOT             | SymTagData               |               | SymTagData
             * | SymbolDataSimpleImpl<4380,7>              | S_LMANDATA              | SymTagData               |               | SymTagData
             * | SymbolDataSimpleImpl<4381,7>              | S_GMANDATA              | SymTagData               |               | SymTagData
             * | SymbolDataSimpleImpl<4382,7>              | S_MANFRAMEREL           | SymTagData               |               | SymTagData
             * | SymbolDataSimpleImpl<4383,7>              | S_MANREGISTER           | SymTagData               |               | SymTagData
             * | SymbolDataSimpleImpl<4384,7>              | S_MANSLOT               | SymTagData               |               | SymTagData
             * | SymbolDataSimpleImpl<4385,7>              | S_MANMANYREG            | SymTagData               |               | SymTagData
             * | SymbolDataSimpleImpl<4386,7>              | S_MANREGREL             | SymTagData               |               | SymTagData
             * | SymbolDataSimpleImpl<4387,7>              | S_MANMANYREG2           | SymTagData               |               | SymTagData
             * | SymbolDataSimpleImpl<4397,7>              | S_MANCONSTANT           | SymTagData               |               | SymTagData
             * | SymbolDataSimpleImpl<4398,7>              | S_ATTR_FRAMEREL         | SymTagData               |               | SymTagData
             * | SymbolDataSimpleImpl<4399,7>              | S_ATTR_REGISTER         | SymTagData               |               | SymTagData
             * | SymbolDataSimpleImpl<4400,7>              | S_ATTR_REGREL           | SymTagData               |               | SymTagData
             * | SymbolDataSimpleImpl<4401,7>              | S_ATTR_MANYREG          | SymTagData               |               | SymTagData
             * | SymbolDataSimpleImpl<4414,7>              | S_LOCAL                 | SymTagData               |               | SymTagData
             * | SymbolDataSimpleImpl<4433,7>              | S_GDATA_HLSL            | SymTagData               |               | SymTagData
             * | SymbolDataSimpleImpl<4434,7>              | S_LDATA_HLSL            | SymTagData               |               | SymTagData
             * | SymbolDataSimpleImpl<4435,7>              | S_FILESTATIC            | SymTagData               |               | SymTagData
             * | SymbolDataSimpleImpl<4436,7>              | S_LOCAL_DPC_GROUPSHARED | SymTagData               |               | SymTagData
             * | SymbolDataSimpleImpl<4450,7>              | S_GDATA_HLSL32          | SymTagData               |               | SymTagData
             * | SymbolDataSimpleImpl<4451,7>              | S_LDATA_HLSL32          | SymTagData               |               | SymTagData
             * | SymbolDataSimpleImpl<4452,7>              | S_GDATA_HLSL32_EX       | SymTagData               |               | SymTagData
             * | SymbolDataSimpleImpl<4453,7>              | S_LDATA_HLSL32_EX       | SymTagData               |               | SymTagData
             * | SymbolDataSimpleImpl<4464,7>              | S_BPREL32_INDIR         | SymTagData               |               | SymTagData
             * | SymbolDataSimpleImpl<4465,7>              | S_REGREL32_INDIR        | SymTagData               |               | SymTagData
             * | SymbolDataSimpleImpl<4470,7>              | S_STATICLOCAL           | SymTagData               |               | SymTagData
             *
             * Annotation (8)
             * =========================
             * | SymbolDataGeneralImpl<ANNOTATIONSYM,4121> | S_ANNOTATION            |                          | ANNOTATIONSYM | SymTagData / SymTagAnnotation
             *
             * Label (9)
             * =========================
             * | SymbolDataSimpleImpl<4357,9>              | S_LABEL32               | SymTagLabel              |               | SymTagLabel
             *
             * PublicSymbol (10)
             * =========================
             * | SymbolDataSimpleImpl<4366,10>             | S_PUB32                 | SymTagPublicSymbol       |               | SymTagPublicSymbol
             *
             * UDT (11)
             * Enum (12)
             * FunctionType (13)
             * PointerType (14)
             * ArrayType (15)
             * BaseType (16)
             *
             * Typedef (17)
             * =========================
             * | SymbolDataSimpleImpl<4360,17>             | S_UDT                   | SymTagTypedef            |               | SymTagTypedef
             *
             * BaseClass (18)
             * Friend (19)
             * FunctionArgType (20)
             *
             * FuncDebugStart (21)
             * FuncDebugEnd (22)
             * =========================
             * All SYM_ENUM_e kinds may instead generate FuncDebugStart/FuncDebugEnd
             *
             * UsingNamespace (23)
             * =========================
             * | SymbolDataSimpleImpl<4388,23>             | S_UNAMESPACE            | SymTagUsingNamespace     |               | SymTagUsingNamespace
             *
             * VTableShape (24)
             * =========================
             *
             * VTable (25)
             *
             * Custom (26)
             * =========================
             * | SymbolDataSimpleImpl<1028,26>             | S_OEM                   | SymTagCustom             |               | SymTagCustom
             *
             * Thunk (27)
             * =========================
             * | SymbolDataSimpleImpl<4354,27>             | S_THUNK32               | SymTagThunk              |               | SymTagThunk             
             * | SymbolDataSimpleImpl<4396,27>             | S_TRAMPOLINE            | SymTagThunk              |               | SymTagThunk
             *
             * CustomType (28)
             * ManagedType (29)
             * Dimension (30)
             *
             * CallSite (31)
             * =========================
             * | SymbolDataSimpleImpl<4409,31>             | S_CALLSITEINFO          | SymTagCallSite           |               | SymTagCallSite
             *
             * InlineSite (32)
             * =========================
             * | SymbolDataSimpleImpl<4429,32>             | S_INLINESITE            | SymTagInlineSite         |               | SymTagInlineSite
             * | SymbolDataSimpleImpl<4445,32>             | S_INLINESITE2           | SymTagInlineSite         |               | SymTagInlineSite
             *
             * BaseInterface (33)
             * VectorType (34)
             * MatrixType (35)
             * HLSLType (36)
             *
             * Caller (37)
             * =========================
             * | SymbolDataGeneralImpl<FUNCTIONLIST,4443>  | S_CALLERS               |                          | FUNCTIONLIST  | SymTagCaller
             *
             * Callee (38)
             * =========================
             * | SymbolDataGeneralImpl<FUNCTIONLIST,4442>  | S_CALLEES               |                          | FUNCTIONLIST  | SymTagCallee
             *
             * Export (39)
             * =========================
             * | SymbolDataSimpleImpl<4408,39>             | S_EXPORT                | SymTagExport             |               | SymTagExport
             *
             * HeapAllocationSite (40)
             * =========================
             * | SymbolDataSimpleImpl<4446,40>             | S_HEAPALLOCSITE         | SymTagHeapAllocationSite |               | SymTagHeapAllocationSite
             *
             * CoffGroup (41)
             * =========================
             * | SymbolDataSimpleImpl<4407,41>             | S_COFFGROUP             | SymTagCoffGroup          |               | SymTagCoffGroup
             *
             * Inlinee (42)
             * TaggedUnionCase (43)
             */

            //I think getDataForProcSym by default is passed 0, which means to do SymTagFunction. COptDbgLocalTrav::get may pass in 1 instead, and then it also calls it again with 2,
            //which causes the function start and end symbols to be generated
            switch (symType.rectyp)
            {
                //EXE is a fake SymTagEnum from CTopLevelTrav

                //Compiland (2)

                #region CompilandDetails (3)

                case S_COMPILE:
                case S_COMPILE3:
                    return SymTagEnum.CompilandDetails;

                case S_COMPILE2:
                    throw new NotImplementedException(); //SymTagCompilandDetails / SymTagCompilandEnv

                #endregion
                #region CompilandEnv (4)

                case S_OBJNAME:
                case S_BUILDINFO:
                case S_ENVBLOCK:
                    return SymTagEnum.CompilandEnv;

                #endregion
                #region Function (5)

                case S_LPROC32:
                case S_GPROC32:
                case S_LPROCMIPS:
                case S_GPROCMIPS:
                case S_LPROCIA64:
                case S_GPROCIA64:
                case S_GMANPROC:
                case S_LMANPROC:
                case S_LPROC32_ID:
                case S_GPROC32_ID:
                case S_LPROCMIPS_ID:
                case S_GPROCMIPS_ID:
                case S_LPROCIA64_ID:
                case S_GPROCIA64_ID:
                case S_LPROC32_DPC:
                case S_LPROC32_DPC_ID:
                case S_GPROC32EX:
                case S_LPROC32EX:
                case S_GPROC32EX_ID:
                case S_LPROC32EX_ID:
                    return SymTagEnum.Function;

                #endregion
                #region Block (6)

                case S_BLOCK32:
                case S_SEPCODE:
                    return SymTagEnum.Block;

                #endregion
                #region Data (7)

                case S_REGISTER:
                case S_CONSTANT:
                case S_MANYREG:
                case S_BPREL32:
                case S_LDATA32:
                case S_GDATA32:
                case S_REGREL32:
                case S_LTHREAD32:
                case S_GTHREAD32:
                case S_MANYREG2:
                case S_LOCALSLOT:
                case S_PARAMSLOT:
                case S_LMANDATA:
                case S_GMANDATA:
                case S_MANFRAMEREL:
                case S_MANREGISTER:
                case S_MANSLOT:
                case S_MANMANYREG:
                case S_MANREGREL:
                case S_MANMANYREG2:
                case S_MANCONSTANT:
                case S_ATTR_FRAMEREL:
                case S_ATTR_REGISTER:
                case S_ATTR_REGREL:
                case S_ATTR_MANYREG:
                case S_LOCAL:
                case S_GDATA_HLSL:
                case S_LDATA_HLSL:
                case S_FILESTATIC:
                case S_LOCAL_DPC_GROUPSHARED:
                case S_GDATA_HLSL32:
                case S_LDATA_HLSL32:
                case S_GDATA_HLSL32_EX:
                case S_LDATA_HLSL32_EX:
                case S_BPREL32_INDIR:
                case S_REGREL32_INDIR:
                case S_STATICLOCAL:
                    return SymTagEnum.Data;

                #endregion
                #region Annotation (8)

                case S_ANNOTATION:
                    //This can either be SymTagData or SymTagAnnotation. I don't currently know how to tell when to use which
                    return SymTagEnum.Annotation;

                #endregion
                #region Label (9)

                case S_LABEL32:
                    return SymTagEnum.Label;

                #endregion
                #region PublicSymbol (10)

                case S_PUB32:
                    return SymTagEnum.PublicSymbol;

                #endregion

                //UDT (11)
                //Enum (12)
                //FunctionType (13)
                //PointerType (14)
                //ArrayType (15)
                //BaseType (16)

                #region Typedef (17)

                case S_UDT:
                    return SymTagEnum.Typedef;

                #endregion

                //BaseClass (18)
                //Friend (19)
                //FunctionArgType (20)
                //FuncDebugStart (21)
                //FuncDebugEnd (22)

                #region UsingNamespace (23)

                case S_UNAMESPACE:
                    return SymTagEnum.UsingNamespace;

                #endregion

                //VTableShape (24)
                //VTable (25)

                #region Custom (26)

                case S_OEM:
                    return SymTagEnum.Custom;

                #endregion
                #region Thunk (27)

                case S_THUNK32:
                case S_TRAMPOLINE:
                    return SymTagEnum.Thunk;

                #endregion

                //CustomType (28)
                //ManagedType (29)
                //Dimension (30)

                #region CallSite (31)

                case S_CALLSITEINFO:
                    return SymTagEnum.CallSite;

                #endregion
                #region InlineSite (32)

                case S_INLINESITE:
                case S_INLINESITE2:
                    return SymTagEnum.InlineSite;

                #endregion

                //BaseInterface (33)
                //VectorType (34)
                //MatrixType (35)
                //HLSLType (36)

                #region Caller (37)

                case S_CALLERS:
                    return SymTagEnum.Caller;

                #endregion
                #region Callee (38)

                case S_CALLEES:
                    return SymTagEnum.Callee;

                #endregion
                #region Export (39)

                case S_EXPORT:
                    return SymTagEnum.Export;

                #endregion
                #region HeapAllocationSite (40)

                case S_HEAPALLOCSITE:
                    return SymTagEnum.HeapAllocationSite;

                #endregion
                #region CoffGroup (41)

                case S_COFFGROUP:
                    return SymTagEnum.CoffGroup;

                #endregion

                //Inlinee (42)
                //TaggedUnionCase (43)

                default:
                    return SymTagEnum.Null;
            }
        }
    }

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

            var sectionHeaders = SymbolMemoryTracker.GetSectionHeaders((long) symType);

            //The section number we're given is 1 based
            if (sectionHeaders == null || seg > sectionHeaders.Length)
                return 0;

            ref var sectionHeader = ref sectionHeaders[seg - 1];

            return sectionHeader.VirtualAddress + off;
        }

        internal static SymType GetSymbol<T>(T* symType, short imod, int ibSym) where T : unmanaged
        {
            //To get the symbol that this ref points to, lookup the module indicated by imod (which is 1 based) and then get the symbol at ibSym bytes into the module's address space

            var modules = SymbolMemoryTracker.GetModules((long) symType);

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

        internal static IModi? GetModuleFromSectionAddress<T>(T* symType, ushort seg, int off) where T : unmanaged
        {
            //DBI1::QueryImodFromAddrHelper does a binary search on the section contribs to the contrib that contains the listed section and offset.

            var dbi = SymbolMemoryTracker.GetPDB((long) symType)?.DBI;

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

        internal static bool TryGetSectionContrib(SYMTYPE* symType, ushort seg, int off, out SC40 sc)
        {
            var dbi = SymbolMemoryTracker.GetPDB((long) symType)?.DBI;

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
