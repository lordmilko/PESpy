using System;
using System.Diagnostics;
using ClrDebug;
using ClrDebug.DIA;
using ClrDebug.PDB;
using static ClrDebug.PDB.SYM_ENUM_e;

namespace PESpy.PDB
{
    public static partial class SymTypeExtensions
    {
        //todo: need an overload that also takes a codeviewaccessor
        public static int GetRVA(in this SymType symType) => GetRVA(symType, null);

        public static int GetRVA(in this SymType symType, ICodeViewAccessor? codeViewAccessor)
        {
            if (!symType.TryGetRVA(codeViewAccessor, out var rva))
                throw new InvalidOperationException($"Could not resolve an RVA for symbol '{symType}'");

            return rva;
        }

        //This method _does_ traverse ref symbols
        public static unsafe bool TryGetRVA(in this SymType symType, out int rva) =>
            TryGetRVA(symType, null, out rva);

        public static unsafe bool TryGetRVA(in this SymType symType, ICodeViewAccessor? codeViewAccessor, out int rva)
        {
            if (TryGetOffSeg(symType, out var off, out var seg))
            {
                var rawRva = SymType.GetRelativeVirtualAddress((SYMTYPE*) symType, seg, off, codeViewAccessor);

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

        public static unsafe bool TryGetRVA(in this SymType symType, ushort seg, ushort off, out int rva)
        {
            var rawRva = SymType.GetRelativeVirtualAddress((SYMTYPE*) symType, seg, off, null);

            //Data symbols can have a section index of 0, indicating they don't physically exist
            if (rawRva != null)
            {
                rva = rawRva.Value;
                return true;
            }

            rva = default;
            return false;
        }

        public static SymString GetName(in this SymType symType) => GetName(symType, null);

        public static SymString GetName(in this SymType symType, ICodeViewAccessor? codeViewAccessor)
        {
            if (!TryGetName(symType, codeViewAccessor, out var name))
                throw new NotImplementedException();

            return name;
        }

        public static bool TryGetName(in this SymType symType, out SymString name) =>
            TryGetName(symType, null, out name);

        public static bool TryGetName(in this SymType symType, ICodeViewAccessor? codeViewAccessor, out SymString name)
        {
            switch (symType.rectyp)
            {
                case S_MANREGREL_ST: //Not supported by DIA
                case S_MANREGREL:
                case S_ATTR_REGREL:
                    name = ((AttrRegRel) symType).GetName(codeViewAccessor);
                    return true;

                case S_MANREGISTER_ST: //Not supported by DIA
                case S_MANREGISTER:
                case S_ATTR_REGISTER:
                    name = ((AttrRegSym) symType).GetName(codeViewAccessor);
                    return true;

                case S_MANSLOT_ST: //Not supported by DIA
                case S_MANSLOT:
                    name = ((AttrSlotSym) symType).GetName(codeViewAccessor);
                    return true;

                case S_BLOCK16:
                case S_WITH16:
                    name = ((BlockSym16) symType).GetName(codeViewAccessor);
                    return true;

                case S_BLOCK32_ST: //Not supported by DIA
                case S_WITH32_ST: //Not supported by DIA
                case S_BLOCK32:
                case S_WITH32:
                    name = ((BlockSym32) symType).GetName(codeViewAccessor);
                    return true;

                case S_BPREL16:
                    name = ((BPRelSym16) symType).GetName(codeViewAccessor);
                    return true;

                case S_BPREL32_ST: //Not supported by DIA
                case S_BPREL32:
                    name = ((BPRelSym32) symType).GetName(codeViewAccessor);
                    return true;

                case S_BPREL32_16t: //Not supported by DIA
                    name = ((BPRelSym3216t) symType).GetName(codeViewAccessor);
                    return true;

                //case S_COMPILE:
                //    name = ((CFlagSym) symType).GetName(codeViewAccessor);
                //    return true;

                case S_COFFGROUP:
                    name = ((CoffGroupSym) symType).GetName(codeViewAccessor);
                    return true;

                //case S_COMPILE2_ST: //Not supported by DIA
                //case S_COMPILE2:
                //    name = ((CompileSym) symType).GetName(codeViewAccessor);
                //    return true;

                //case S_COMPILE3:
                //    name = ((CompileSym3) symType).GetName(codeViewAccessor);
                //    return true;

                case S_CONSTANT_ST: //Not supported by DIA
                case S_CONSTANT:
                case S_MANCONSTANT:
                    name = ((ConstSym) symType).GetName(codeViewAccessor);
                    return true;

                case S_CONSTANT_16t: //Not supported by DIA
                    name = ((ConstSym16t) symType).GetName(codeViewAccessor);
                    return true;

                case S_LDATA16:
                case S_GDATA16:
                case S_PUB16:
                    name = ((DataSym16) symType).GetName(codeViewAccessor);
                    return true;

                case S_LDATA32_ST: //Not supported by DIA
                case S_GDATA32_ST: //Not supported by DIA
                case S_LTHREAD32_ST: //Not supported by DIA
                case S_GTHREAD32_ST: //Not supported by DIA
                case S_LMANDATA_ST: //Not supported by DIA
                case S_GMANDATA_ST: //Not supported by DIA
                case S_LDATA32:
                case S_GDATA32:
                case S_LTHREAD32:
                case S_GTHREAD32:
                case S_LMANDATA:
                case S_GMANDATA:
                    name = ((DataSym32) symType).GetName(codeViewAccessor);
                    return true;

                case S_LDATA32_16t: //Not supported by DIA
                case S_GDATA32_16t: //Not supported by DIA
                case S_PUB32_16t: //Not supported by DIA
                case S_LTHREAD32_16t: //Not supported by DIA
                case S_GTHREAD32_16t: //Not supported by DIA
                    name = ((DataSym3216t) symType).GetName(codeViewAccessor);
                    return true;

                case S_GDATA_HLSL:
                case S_LDATA_HLSL:
                    name = ((DataSymHLSL) symType).GetName(codeViewAccessor);
                    return true;

                //DataSymHLSL32
                //DataSymHLSL32Ex

                case S_EXPORT:
                    name = ((ExportSym) symType).GetName(codeViewAccessor);
                    return true;

                case S_FILESTATIC:
                    name = ((FileStaticSym) symType).GetName(codeViewAccessor);
                    return true;

                case S_MANFRAMEREL_ST: //Not supported by DIA
                case S_MANFRAMEREL:
                case S_ATTR_FRAMEREL:
                    name = ((FrameRelSym) symType).GetName(codeViewAccessor);
                    return true;

                case S_LABEL16:
                    name = ((LabelSym16) symType).GetName(codeViewAccessor);
                    return true;

                case S_LABEL32_ST: //Not supported by DIA
                case S_LABEL32:
                    name = ((LabelSym32) symType).GetName(codeViewAccessor);
                    return true;

                case S_LOCAL_DPC_GROUPSHARED:
                    name = ((LocalDPCGroupSharedSym) symType).GetName(codeViewAccessor);
                    return true;

                case S_LOCAL:
                    name = ((LocalSym) symType).GetName(codeViewAccessor);
                    return true;

                case S_GMANPROC_ST: //Not supported by DIA
                case S_LMANPROC_ST: //Not supported by DIA
                case S_GMANPROC:
                case S_LMANPROC:
                    name = ((ManProcSym) symType).GetName(codeViewAccessor);
                    return true;

                case S_OBJNAME_ST: //Not supported by DIA
                case S_OBJNAME:
                    name = ((ObjNameSym) symType).GetName(codeViewAccessor);
                    return true;

                case S_PDBMAP:
                    name = ((PdbMap) symType).GetName(codeViewAccessor);
                    return true;

                case S_LPROC16:
                case S_GPROC16:
                    name = ((ProcSym16) symType).GetName(codeViewAccessor);
                    return true;

                case S_LPROC32_ST: //Not supported by DIA
                case S_GPROC32_ST: //Not supported by DIA
                case S_LPROC32:
                case S_GPROC32:
                case S_LPROC32_ID:
                case S_GPROC32_ID:
                case S_LPROC32_DPC:
                case S_LPROC32_DPC_ID:
                    name = ((ProcSym32) symType).GetName(codeViewAccessor);
                    return true;

                case S_LPROC32_16t: //Not supported by DIA
                case S_GPROC32_16t: //Not supported by DIA
                    name = ((ProcSym3216t) symType).GetName(codeViewAccessor);
                    return true;

                case S_LPROCIA64_ST: //Not supported by DIA
                case S_GPROCIA64_ST: //Not supported by DIA
                case S_LPROCIA64:
                case S_GPROCIA64:
                case S_LPROCIA64_ID:
                case S_GPROCIA64_ID:
                    name = ((ProcSymIA64) symType).GetName(codeViewAccessor);
                    return true;

                case S_LPROCMIPS_ST: //Not supported by DIA
                case S_GPROCMIPS_ST: //Not supported by DIA
                case S_LPROCMIPS:
                case S_GPROCMIPS:
                case S_LPROCMIPS_ID:
                case S_GPROCMIPS_ID:
                    name = ((ProcSymMips) symType).GetName(codeViewAccessor);
                    return true;

                case S_LPROCMIPS_16t: //Not supported by DIA
                case S_GPROCMIPS_16t: //Not supported by DIA
                    name = ((ProcSymMips16t) symType).GetName(codeViewAccessor);
                    return true;

                case S_PUB32_ST: //Not supported by DIA
                case S_PUB32:
                    name = ((PubSym32) symType).GetName(codeViewAccessor);
                    return true;

                case S_REF_MINIPDB:
                    name = ((RefMiniPdb) symType).GetName(codeViewAccessor);
                    return true;

                case S_PROCREF_ST: //Not supported by DIA
                case S_DATAREF_ST: //Not supported by DIA
                case S_LPROCREF_ST: //Not supported by DIA
                    name = ((RefSym) symType).GetName(codeViewAccessor);
                    return true;

                case S_PROCREF:
                case S_DATAREF:
                case S_LPROCREF:
                case S_ANNOTATIONREF:
                case S_TOKENREF:
                    name = ((RefSym2) symType).GetName(codeViewAccessor);
                    return true;

                case S_REGREL16:
                    name = ((RegRel16) symType).GetName(codeViewAccessor);
                    return true;

                case S_REGREL32_ST: //Not supported by DIA
                case S_REGREL32:
                    name = ((RegRel32) symType).GetName(codeViewAccessor);
                    return true;

                case S_REGREL32_16t: //Not supported by DIA
                    name = ((RegRel3216t) symType).GetName(codeViewAccessor);
                    return true;

                case S_REGISTER_ST: //Not supported by DIA
                case S_REGISTER:
                    name = ((RegSym) symType).GetName(codeViewAccessor);
                    return true;

                case S_REGISTER_16t: //Not supported by DIA
                    name = ((RegSym16t) symType).GetName(codeViewAccessor);
                    return true;

                case S_SECTION:
                    name = ((SectionSym) symType).GetName(codeViewAccessor);
                    return true;

                case S_LOCALSLOT_ST: //Not supported by DIA
                case S_PARAMSLOT_ST: //Not supported by DIA
                case S_LOCALSLOT:
                case S_PARAMSLOT:
                    name = ((SlotSym32) symType).GetName(codeViewAccessor);
                    return true;

                case S_THUNK32_ST: //Not supported by DIA
                case S_THUNK32:
                    name = ((ThunkSym32) symType).GetName(codeViewAccessor);
                    return true;

                case S_UDT_ST: //Not supported by DIA
                case S_COBOLUDT_ST: //Not supported by DIA
                case S_UDT:
                case S_COBOLUDT:
                    name = ((UdtSym) symType).GetName(codeViewAccessor);
                    return true;

                case S_UDT_16t: //Not supported by DIA
                case S_COBOLUDT_16t: //Not supported by DIA
                    name = ((UdtSym16t) symType).GetName(codeViewAccessor);
                    return true;

                case S_UNAMESPACE_ST: //Not supported by DIA
                case S_UNAMESPACE:
                    name = ((UNameSpace) symType).GetName(codeViewAccessor);
                    return true;

                default:
                    name = default;
                    return false;
            }
        }

        public static bool TryGetDemangledName(in this SymType symType, out string? demangledName, UNDNAME flags = UNDNAME.UNDNAME_COMPLETE)
        {
            switch (symType.rectyp)
            {
                case S_PUB16:
                {
                    var dataSym = (DataSym16) symType;

                    if (dataSym.name.StartsWith("?"))
                    {
                        if (Demangler.TryParseString(dataSym.name, flags, out demangledName))
                            return true;
                    }
                }
                break;

                case S_PUB32_16t: //Not supported by DIA
                    {
                    var dataSym = (DataSym3216t) symType;

                    if (dataSym.name.StartsWith("?"))
                    {
                        if (Demangler.TryParseString(dataSym.name, flags, out demangledName))
                            return true;
                    }
                }
                break;


                case S_PUB32_ST: //Not supported by DIA
                case S_PUB32:
                {
                    var pubSym = (PubSym32) symType;

                    if (pubSym.name.StartsWith("?"))
                    {
                        if (Demangler.TryParseString(pubSym.name, flags, out demangledName))
                            return true;
                    }
                }
                break;
            }

            demangledName = default;
            return false;
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

                case S_BLOCK32_ST: //Not supported by DIA
                case S_WITH32_ST: //Not supported by DIA
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

                case S_LDATA32_ST: //Not supported by DIA
                case S_GDATA32_ST: //Not supported by DIA
                case S_LTHREAD32_ST: //Not supported by DIA
                case S_GTHREAD32_ST: //Not supported by DIA
                case S_LMANDATA_ST: //Not supported by DIA
                case S_GMANDATA_ST: //Not supported by DIA
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

                case S_LDATA32_16t: //Not supported by DIA
                case S_GDATA32_16t: //Not supported by DIA
                case S_PUB32_16t: //Not supported by DIA
                case S_LTHREAD32_16t: //Not supported by DIA
                case S_GTHREAD32_16t: //Not supported by DIA
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

                case S_LABEL32_ST: //Not supported by DIA
                case S_LABEL32:
                {
                    var sym = ((LabelSym32) symType);
                    off = sym.off;
                    seg = sym.seg;
                    return true;
                }

                case S_GMANPROC_ST: //Not supported by DIA
                case S_LMANPROC_ST: //Not supported by DIA
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

                case S_LPROC32_ST: //Not supported by DIA
                case S_GPROC32_ST: //Not supported by DIA
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

                case S_LPROC32_16t: //Not supported by DIA
                case S_GPROC32_16t: //Not supported by DIA
                    {
                    var sym = ((ProcSym3216t) symType);
                    off = sym.off;
                    seg = sym.seg;
                    return true;
                }

                case S_LPROCIA64_ST: //Not supported by DIA
                case S_GPROCIA64_ST: //Not supported by DIA
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

                case S_LPROCMIPS_ST: //Not supported by DIA
                case S_GPROCMIPS_ST: //Not supported by DIA
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

                case S_LPROCMIPS_16t: //Not supported by DIA
                case S_GPROCMIPS_16t: //Not supported by DIA
                    {
                    var sym = ((ProcSymMips16t) symType);
                    off = sym.off;
                    seg = sym.seg;
                    return true;
                }

                case S_PUB32_ST: //Not supported by DIA
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

                case S_THUNK32_ST: //Not supported by DIA
                case S_THUNK32:
                {
                    var sym = ((ThunkSym32) symType);
                    off = sym.off;
                    seg = sym.seg;
                    return true;
                }

                //ref symbols don't have a seg, but they may point to something that does!

                case S_PROCREF_ST: //Not supported by DIA
                case S_DATAREF_ST: //Not supported by DIA
                case S_LPROCREF_ST: //Not supported by DIA
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
        public static unsafe bool IsCode(in this SymType symType) => IsCode(symType, null);

        public static unsafe bool IsCode(in this SymType symType, ICodeViewAccessor? codeViewAccessor)
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
                case S_PUB32_ST: //Not supported by DIA
                    {
                    var value = (PubSym32) symType;

                    //I don't know which flags should be used for saying "it's code". e.g. could you have a function but not code?
                    //Similarly, what if it's managed/MSIL?

                    if (value.pubsymflags.fCode)
                        return true;

                    if (value.pubsymflags.fFunction)
                    {
                        Debug.Assert(value.pubsymflags.fCode); //We assume that fFunction implies fCode
                        return true;
                    }

                    seg = value.seg;
                    off = value.off;
                    break;
                }

                case S_PUB32_16t: //Not supported by DIA
                    {
                    var value = (DataSym3216t) symType;
                    seg = value.seg;
                    off = value.off;
                    break;
                }

                default:
                    return false;
            }

            if (SymType.TryGetSectionCharacteristics(symType, seg, off, codeViewAccessor, out var characteristics))
            {
                if ((characteristics & IMAGE_SCN.CNT_CODE) != 0)
                    return true;
            }

            return false;
        }

        public static bool IsBlockSym(in this SymType symType)
        {
            //msdia140!isBlockSym
            switch (symType.rectyp)
            {
                //todo: all the 16-bit ones dia doesnt list

                case S_THUNK16: //Not supported by DIA
                case S_THUNK32_ST: //Not supported by DIA
                case S_THUNK32:

                case S_BLOCK16: //Not supported by DIA
                case S_BLOCK32_ST: //Not supported by DIA
                case S_BLOCK32:

                case S_WITH16: //Not supported by DIA
                case S_WITH32_ST: //Not supported by DIA
                case S_WITH32:

                case S_LPROC16: //Not supported by DIA
                case S_LPROC32_ST: //Not supported by DIA
                case S_LPROC32:

                case S_GPROC16: //Not supported by DIA
                case S_GPROC32_16t: //Not supported by DIA
                case S_GPROC32_ST: //Not supported by DIA
                case S_GPROC32:

                case S_LPROCMIPS_16t: //Not supported by DIA
                case S_LPROCMIPS_ST: //Not supported by DIA
                case S_LPROCMIPS:

                case S_GPROCMIPS_16t: //Not supported by DIA
                case S_GPROCMIPS_ST: //Not supported by DIA
                case S_GPROCMIPS:

                case S_LPROCIA64_ST://Not supported by DIA
                case S_LPROCIA64:

                case S_GPROCIA64:
                case S_GPROCIA64_ST: //Not supported by DIA

                case S_GMANPROC:
                case S_GMANPROC_ST: //Not supported by DIA

                case S_LMANPROC:
                case S_LMANPROC_ST: //Not supported by DIA

                //DIA has lots of logic all over the case for special casing S_TRAMPOLINE. S_TRAMPOLINE does not
                //have a corresponding S_END symbol however, and should not be considered a block
                case S_SEPCODE:
                case S_LPROC32_ID:
                case S_GPROC32_ID:
                case S_LPROCMIPS_ID:
                case S_GPROCMIPS_ID:
                case S_LPROCIA64_ID:
                case S_GPROCIA64_ID:
                case S_INLINESITE:
                case S_LPROC32_DPC:
                case S_LPROC32_DPC_ID:
                case S_INLINESITE2:

                //Not publically documented, but used by DIA
                case S_GPROC32EX:
                case S_LPROC32EX:
                case S_GPROC32EX_ID:
                case S_LPROC32EX_ID:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsDefRangeSym(in this SymType symType)
        {
            //msdia140!SymBuffer::isDefRangeSym
            switch (symType.rectyp)
            {
                case S_DEFRANGE:
                case S_DEFRANGE_SUBFIELD:
                case S_DEFRANGE_REGISTER:
                case S_DEFRANGE_FRAMEPOINTER_REL:
                case S_DEFRANGE_SUBFIELD_REGISTER:
                case S_DEFRANGE_FRAMEPOINTER_REL_FULL_SCOPE:
                case S_DEFRANGE_REGISTER_REL:
                case S_DEFRANGE_HLSL:
                case S_DEFRANGE_DPC_PTR_TAG:
                case S_DEFRANGE_REGISTER_REL_INDIR:
                case S_DEFRANGE_CONSTVAL_ON_ENTRY:
                case S_DEFRANGE_GLOBALSYM_ON_ENTRY:
                    return true;

                default:
                    return false;
            }
        }

        public static bool IsEnd(in this SymType symType)
        {
            switch (symType.rectyp)
            {
                case S_END:
                case S_ENDARG:
                case S_INLINESITE_END:
                case S_PROC_ID_END:
                    return true;

                default:
                    return false;
            }
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
                case S_LPROC32_16t: //Not supported by DIA
                case S_GPROC32_16t: //Not supported by DIA

                //ProcSymMips16t
                case S_LPROCMIPS_16t: //Not supported by DIA
                case S_GPROCMIPS_16t: //Not supported by DIA

                //ProcSym32
                case S_LPROC32_ST: //Not supported by DIA
                case S_GPROC32_ST: //Not supported by DIA
                case S_LPROC32:
                case S_GPROC32:
                case S_LPROC32_ID:
                case S_GPROC32_ID:
                case S_LPROC32_DPC:
                case S_LPROC32_DPC_ID:

                //ProcSymMips
                case S_LPROCMIPS_ST: //Not supported by DIA
                case S_GPROCMIPS_ST: //Not supported by DIA
                case S_LPROCMIPS:
                case S_GPROCMIPS:
                case S_LPROCMIPS_ID:
                case S_GPROCMIPS_ID:

                //Not sure if FRAMEPROC should be included

                //ProcSymIA64
                case S_LPROCIA64_ST: //Not supported by DIA
                case S_GPROCIA64_ST: //Not supported by DIA
                case S_LPROCIA64:
                case S_GPROCIA64:
                case S_LPROCIA64_ID:
                case S_GPROCIA64_ID:

                //ManProcSym
                case S_GMANPROC_ST: //Not supported by DIA
                case S_LMANPROC_ST: //Not supported by DIA
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
                case S_THUNK32_ST: //Not supported by DIA
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
                case S_PROCREF_ST: //Not supported by DIA
                case S_DATAREF_ST: //Not supported by DIA
                case S_LPROCREF_ST: //Not supported by DIA

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
             * Data (7)                                                                                                                        DataKind                LocationType
             * =========================                                                                                                       =========               ============
             * | SymbolDataSimpleImpl<4358,7>              | S_REGISTER              | SymTagData               |               | SymTagData | LocIsEnregistered     | DataIsLocal
             * | SymbolDataSimpleImpl<4359,7>              | S_CONSTANT              | SymTagData               |               | SymTagData | LocIsConstant         | DataIsConstant
             * | SymbolDataSimpleImpl<4362,7>              | S_MANYREG               | SymTagData               |               | SymTagData |                       |
             * | SymbolDataSimpleImpl<4363,7>              | S_BPREL32               | SymTagData               |               | SymTagData | LocIsRegRel           | DataIsLocal / DataIsParam (if typind > 0)
             * | SymbolDataSimpleImpl<4364,7>              | S_LDATA32               | SymTagData               |               | SymTagData | LocIsStatic           | DataIsStaticLocal / DataIsFileStatic
             * | SymbolDataSimpleImpl<4365,7>              | S_GDATA32               | SymTagData               |               | SymTagData | LocIsStatic           | DataIsGlobal
             * | SymbolDataSimpleImpl<4369,7>              | S_REGREL32              | SymTagData               |               | SymTagData | LocIsRegRel           | DataIsLocal
             * | SymbolDataSimpleImpl<4370,7>              | S_LTHREAD32             | SymTagData               |               | SymTagData | LocIsTLS              | DataIsStaticLocal / DataIsFileStatic
             * | SymbolDataSimpleImpl<4371,7>              | S_GTHREAD32             | SymTagData               |               | SymTagData | LocIsTLS              | DataIsGlobal
             * | SymbolDataSimpleImpl<4375,7>              | S_MANYREG2              | SymTagData               |               | SymTagData |                       |
             * | SymbolDataSimpleImpl<4378,7>              | S_LOCALSLOT             | SymTagData               |               | SymTagData | LocIsSlot             | DataIsLocal
             * | SymbolDataSimpleImpl<4379,7>              | S_PARAMSLOT             | SymTagData               |               | SymTagData | LocIsSlot             | DataIsParam
             * | SymbolDataSimpleImpl<4380,7>              | S_LMANDATA              | SymTagData               |               | SymTagData | LocIsStatic           | DataIsStaticLocal / DataIsFileStatic
             * | SymbolDataSimpleImpl<4381,7>              | S_GMANDATA              | SymTagData               |               | SymTagData | LocIsStatic           | DataIsGlobal
             * | SymbolDataSimpleImpl<4382,7>              | S_MANFRAMEREL           | SymTagData               |               | SymTagData | LocIsRegRel           | ?
             * | SymbolDataSimpleImpl<4383,7>              | S_MANREGISTER           | SymTagData               |               | SymTagData | LocIsEnregistered     | ?
             * | SymbolDataSimpleImpl<4384,7>              | S_MANSLOT               | SymTagData               |               | SymTagData | LocIsSlot             | ?
             * | SymbolDataSimpleImpl<4385,7>              | S_MANMANYREG            | SymTagData               |               | SymTagData |                       |
             * | SymbolDataSimpleImpl<4386,7>              | S_MANREGREL             | SymTagData               |               | SymTagData | LocIsRegRel           | ?
             * | SymbolDataSimpleImpl<4387,7>              | S_MANMANYREG2           | SymTagData               |               | SymTagData |                       |
             * | SymbolDataSimpleImpl<4397,7>              | S_MANCONSTANT           | SymTagData               |               | SymTagData | LocIsConstant         | DataIsConstant
             * | SymbolDataSimpleImpl<4398,7>              | S_ATTR_FRAMEREL         | SymTagData               |               | SymTagData | LocIsRegRel           | ?
             * | SymbolDataSimpleImpl<4399,7>              | S_ATTR_REGISTER         | SymTagData               |               | SymTagData | LocIsEnregistered     | ?
             * | SymbolDataSimpleImpl<4400,7>              | S_ATTR_REGREL           | SymTagData               |               | SymTagData | LocIsRegRel           | ?
             * | SymbolDataSimpleImpl<4401,7>              | S_ATTR_MANYREG          | SymTagData               |               | SymTagData |                       |
             * | SymbolDataSimpleImpl<4414,7>              | S_LOCAL                 | SymTagData               |               | SymTagData | ?                     | DataIsParam / DataIsLocal
             * | SymbolDataSimpleImpl<4433,7>              | S_GDATA_HLSL            | SymTagData               |               | SymTagData | ?                     | DataIsGlobal
             * | SymbolDataSimpleImpl<4434,7>              | S_LDATA_HLSL            | SymTagData               |               | SymTagData | ?                     | DataIsStaticLocal / DataIsFileStatic
             * | SymbolDataSimpleImpl<4435,7>              | S_FILESTATIC            | SymTagData               |               | SymTagData | ?                     | ?
             * | SymbolDataSimpleImpl<4436,7>              | S_LOCAL_DPC_GROUPSHARED | SymTagData               |               | SymTagData | LocIsStatic           | ?
             * | SymbolDataSimpleImpl<4450,7>              | S_GDATA_HLSL32          | SymTagData               |               | SymTagData | ?                     | DataIsGlobal
             * | SymbolDataSimpleImpl<4451,7>              | S_LDATA_HLSL32          | SymTagData               |               | SymTagData | ?                     | DataIsStaticLocal / DataIsFileStatic
             * | SymbolDataSimpleImpl<4452,7>              | S_GDATA_HLSL32_EX       | SymTagData               |               | SymTagData | ?                     | DataIsGlobal
             * | SymbolDataSimpleImpl<4453,7>              | S_LDATA_HLSL32_EX       | SymTagData               |               | SymTagData |                       | DataIsStaticLocal / DataIsFileStatic
             * | SymbolDataSimpleImpl<4464,7>              | S_BPREL32_INDIR         | SymTagData               |               | SymTagData | LocIsRegRelAliasIndir | DataIsLocal /  DataIsParam
             * | SymbolDataSimpleImpl<4465,7>              | S_REGREL32_INDIR        | SymTagData               |               | SymTagData | LocIsRegRelAliasIndir | DataIsLocal /  DataIsParam
             * | SymbolDataSimpleImpl<4470,7>              | S_STATICLOCAL           | SymTagData               |               | SymTagData |                       |
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
                case S_COMPILE2:
                case S_COMPILE2_ST: //Not supported by DIA
                    //CompileSym is inspected twice: once to create a SymTagCompilandDetails around the CompileSym record,
                    //and again to create a SymTagCompilandEnv around all of the environment strings hanging off the end of the symbol
                    return SymTagEnum.CompilandDetails;

                #endregion
                #region CompilandEnv (4)

                case S_OBJNAME:
                case S_OBJNAME_ST: //Not supported by DIA
                case S_BUILDINFO:
                case S_ENVBLOCK:
                    //CompilandEnv symbols are also created for each environment string hanging off the end of a CompileSym
                    return SymTagEnum.CompilandEnv;

                #endregion
                #region Function (5)

                case S_LPROC32:
                case S_LPROC32_ST: //Not supported by DIA
                case S_LPROC32_16t: //Not supported by DIA
                case S_GPROC32:
                case S_GPROC32_ST: //Not supported by DIA
                case S_GPROC32_16t: //Not supported by DIA
                case S_LPROCMIPS:
                case S_LPROCMIPS_ST: //Not supported by DIA
                case S_LPROCMIPS_16t: //Not supported by DIA
                case S_GPROCMIPS:
                case S_GPROCMIPS_ST: //Not supported by DIA
                case S_GPROCMIPS_16t: //Not supported by DIA
                case S_LPROCIA64:
                case S_LPROCIA64_ST: //Not supported by DIA
                case S_GPROCIA64:
                case S_GPROCIA64_ST: //Not supported by DIA
                case S_GMANPROC:
                case S_GMANPROC_ST: //Not supported by DIA
                case S_LMANPROC:
                case S_LMANPROC_ST: //Not supported by DIA
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
                case S_BLOCK32_ST: //Not supported by DIA
                case S_SEPCODE:
                    return SymTagEnum.Block;

                #endregion
                #region Data (7)

                case S_REGISTER:
                case S_REGISTER_16t: //Not supported by DIA
                case S_REGISTER_ST: //Not supported by DIA
                case S_CONSTANT:
                case S_CONSTANT_16t: //Not supported by DIA
                case S_CONSTANT_ST: //Not supported by DIA
                case S_MANYREG:
                case S_MANYREG_ST: //Not supported by DIA
                case S_MANYREG_16t: //Not supported by DIA
                case S_BPREL32:
                case S_BPREL32_ST: //Not supported by DIA
                case S_BPREL32_16t: //Not supported by DIA
                case S_LDATA32:
                case S_LDATA32_ST: //Not supported by DIA
                case S_LDATA32_16t: //Not supported by DIA
                case S_GDATA32:
                case S_GDATA32_ST: //Not supported by DIA
                case S_GDATA32_16t: //Not supported by DIA
                case S_REGREL32:
                case S_REGREL32_ST: //Not supported by DIA
                case S_REGREL32_16t: //Not supported by DIA
                case S_LTHREAD32:
                case S_LTHREAD32_ST: //Not supported by DIA
                case S_LTHREAD32_16t: //Not supported by DIA
                case S_GTHREAD32:
                case S_GTHREAD32_ST: //Not supported by DIA
                case S_GTHREAD32_16t: //Not supported by DIA
                case S_MANYREG2:
                case S_MANYREG2_ST: //Not supported by DIA
                case S_LOCALSLOT:
                case S_LOCALSLOT_ST: //Not supported by DIA
                case S_PARAMSLOT:
                case S_PARAMSLOT_ST: //Not supported by DIA
                case S_LMANDATA:
                case S_LMANDATA_ST: //Not supported by DIA
                case S_GMANDATA:
                case S_GMANDATA_ST: //Not supported by DIA
                case S_MANFRAMEREL:
                case S_MANFRAMEREL_ST: //Not supported by DIA
                case S_MANREGISTER:
                case S_MANREGISTER_ST: //Not supported by DIA
                case S_MANSLOT:
                case S_MANSLOT_ST: //Not supported by DIA
                case S_MANMANYREG:
                case S_MANMANYREG_ST: //Not supported by DIA
                case S_MANREGREL:
                case S_MANREGREL_ST: //Not supported by DIA
                case S_MANMANYREG2:
                case S_MANMANYREG2_ST: //Not supported by DIA
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
                case S_LABEL32_ST: //Not supported by DIA
                    return SymTagEnum.Label;

                #endregion
                #region PublicSymbol (10)

                case S_PUB32:
                case S_PUB32_ST: //Not supported by DIA
                case S_PUB32_16t: //Not supported by DIA
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
                case S_UDT_ST: //Not supported by DIA
                case S_UDT_16t: //Not supported by DIA
                    return SymTagEnum.Typedef;

                #endregion

                //BaseClass (18)
                //Friend (19)
                //FunctionArgType (20)
                //FuncDebugStart (21)
                //FuncDebugEnd (22)

                #region UsingNamespace (23)

                case S_UNAMESPACE:
                case S_UNAMESPACE_ST: //Not supported by DIA
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
                case S_THUNK32_ST: //Not supported by DIA
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

        public static DataKind GetDataKind(in this SymType symType)
        {
            //I am only aware of this being valid for symbols that resolve to SymTagData

            switch (symType.rectyp)
            {
                case S_REGISTER:
                case S_REGREL32:
                case S_LOCALSLOT:
                    return DataKind.DataIsLocal;

                case S_PARAMSLOT:
                    return DataKind.DataIsParam;

                case S_CONSTANT:
                case S_MANCONSTANT:
                    return DataKind.DataIsConstant;

                case S_GDATA32:
                case S_GTHREAD32:
                case S_GMANDATA:
                case S_GDATA_HLSL:
                case S_GDATA_HLSL32:
                case S_GDATA_HLSL32_EX:
                    return DataKind.DataIsGlobal;

                case S_MANYREG:
                    throw new NotImplementedException();

                case S_BPREL32: // DataIsLocal / DataIsParam (if typind > 0)
                    throw new NotImplementedException();

                case S_LDATA32:
                case S_LTHREAD32:
                    //There is logic for these to either be DataIsStaticLocal / DataIsFileStatic however GetTheData::disp_S_LDATA32/disp_S_LTHREAD32 sets the relevant field to 0, the default of static local
                    //is always overwritten with file satic
                    return DataKind.DataIsFileStatic;

                //The following kinds have either CV_LVARFLAGS (or CV_lvar_attr which contains CV_LVARFLAGS)
                //and are set via msdia140!varAttributeFields. Strictly speaking only S_LOCAL considers whether
                //fIsParam is set, but it's technically in the flags of all of them
                case S_MANFRAMEREL:
                case S_MANREGISTER:
                case S_MANSLOT:
                case S_MANREGREL:
                case S_ATTR_FRAMEREL:
                case S_ATTR_REGISTER:
                case S_ATTR_REGREL:
                case S_LOCAL:
                case S_FILESTATIC:
                case S_LOCAL_DPC_GROUPSHARED:
                {
                    var flags = ((LocalSym) symType).flags;

                    if (flags.fIsEnregGlob)
                        return flags.fIsEnregStat ? DataKind.DataIsFileStatic : DataKind.DataIsGlobal;

                    return flags.fIsParam ? DataKind.DataIsParam : DataKind.DataIsLocal;
                }

                case S_MANYREG2: //
                case S_LMANDATA: // DataIsStaticLocal / DataIsFileStatic
                case S_MANMANYREG: //CV_Lvar_attr -> CV_LVARFLAGS logic?
                case S_MANMANYREG2: //CV_Lvar_attr -> CV_LVARFLAGS logic?
                case S_ATTR_MANYREG: //CV_Lvar_attr -> CV_LVARFLAGS logic?
                case S_LDATA_HLSL: // DataIsStaticLocal / DataIsFileStatic
                case S_LDATA_HLSL32: // DataIsStaticLocal / DataIsFileStatic
                case S_LDATA_HLSL32_EX: //DataIsStaticLocal / DataIsFileStatic
                case S_BPREL32_INDIR: // DataIsLocal /  DataIsParam
                case S_REGREL32_INDIR: // DataIsLocal /  DataIsParam
                case S_STATICLOCAL: //
                    throw new NotImplementedException();

                default:
                    throw new NotImplementedException();
            }
        }

        public static bool TryGetLocationType(in this SymType symType, out LocationType locationType)
        {
            switch (symType.rectyp)
            {
                case S_REGISTER:
                case S_MANREGISTER:
                case S_ATTR_REGISTER:
                    locationType = LocationType.LocIsEnregistered;
                    return true;

                case S_CONSTANT:
                    locationType = LocationType.LocIsConstant;
                    return true;

                //Enhanced location info comes from COptDbgLocalTrav
                case S_MANYREG:
                case S_MANYREG2:
                case S_MANMANYREG:
                case S_MANMANYREG2:
                case S_ATTR_MANYREG:
                case S_LOCAL:
                case S_GDATA_HLSL:
                case S_LDATA_HLSL:
                case S_FILESTATIC:
                case S_GDATA_HLSL32:
                case S_LDATA_HLSL32:
                case S_GDATA_HLSL32_EX:
                case S_LDATA_HLSL32_EX:
                case S_STATICLOCAL:
                    Debug.Assert(false, "Not implemented");
                    locationType = default;
                    return false;

                case S_LDATA32:
                case S_GDATA32:
                case S_LMANDATA:
                case S_GMANDATA:
                case S_LOCAL_DPC_GROUPSHARED:
                case S_PUB16:
                case S_PUB32:
                case S_PUB32_16t: //Not supported by DIA
                case S_PUB32_ST: //Not supported by DIA
                    locationType = LocationType.LocIsStatic;
                    return true;

                case S_REGREL32:
                case S_MANFRAMEREL:
                case S_MANREGREL:
                case S_ATTR_FRAMEREL:
                case S_ATTR_REGREL:
                    locationType = LocationType.LocIsRegRel;
                    return true;

                case S_LTHREAD32:
                case S_GTHREAD32:
                    locationType = LocationType.LocIsTLS;
                    return true;

                case S_PARAMSLOT:
                case S_MANSLOT:
                    locationType = LocationType.LocIsSlot;
                    return true;

                case S_BPREL32_INDIR:
                case S_REGREL32_INDIR:
                    locationType = LocationType.LocIsRegRelAliasIndir;
                    return true;

                default:
                    //msdia140!getDataForProcSym
                    if (symType.IsProc())
                    {
                        locationType = LocationType.LocIsStatic;
                        return true;
                    }

                    locationType = default;
                    return false;
            }
        }
    }
}
