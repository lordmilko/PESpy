using System;
using ClrDebug.DIA;
using ClrDebug.PDB;
using static ClrDebug.PDB.LEAF_ENUM_e;
using static ClrDebug.PDB.SYM_ENUM_e;

namespace PESpy.PDB
{
    public static partial class TypTypeExtensions
    {
        public static bool TryGetDataKind(in this LfEasy lfEasy, out DataKind dataKind)
        {
            switch (lfEasy.leaf)
            {
                case LF_MEMBER:
                case LF_MEMBER_ST: //Not supported by DIA
                case LF_MEMBER_16t:  //Not supported by DIA
                    dataKind = DataKind.DataIsMember;
                    return true;

                case LF_STMEMBER:
                case LF_STMEMBER_ST: //Not supported by DIA
                case LF_STMEMBER_16t:  //Not supported by DIA
                    dataKind = DataKind.DataIsStaticMember;
                    return true;

                default:
                    dataKind = default;
                    return false;
            }
        }
    }

    public static partial class SymTypeExtensions
    {
        public static unsafe bool TryGetDataKind(
            in this SymType symType,
            out DataKind value,
            SymType parent = default,
            ICodeViewAccessor? codeViewAccessor = null,
            ICodeViewModuleAccessor? codeViewModuleAccessor = null)
        {
            //I am only aware of this being valid for symbols that resolve to SymTagData

            switch (symType.rectyp)
            {
                case S_REGISTER_16t: //Not supported by DIA
                case S_REGISTER:
                case S_REGISTER_ST: //Not supported by DIA
                case S_LOCALSLOT:
                case S_LOCALSLOT_ST: //Not supported by DIA
                    value = DataKind.DataIsLocal;
                    return true;

                case S_PARAMSLOT:
                case S_PARAMSLOT_ST: //Not supported by DIA
                    value = DataKind.DataIsParam;
                    return true;

                case S_CONSTANT_16t: //Not supported by DIA
                case S_CONSTANT:
                case S_CONSTANT_ST: //Not supported by DIA
                case S_MANCONSTANT:
                    value = DataKind.DataIsConstant;
                    return true;

                case S_GDATA32_16t: //Not supported by DIA
                case S_GDATA32:
                case S_GDATA32_ST: //Not supported by DIA
                case S_GTHREAD32:
                case S_GTHREAD32_ST: //Not supported by DIA
                case S_GMANDATA:
                case S_GMANDATA_ST: //Not supported by DIA
                case S_GDATA_HLSL:
                case S_GDATA_HLSL32:
                case S_GDATA_HLSL32_EX:
                    value = DataKind.DataIsGlobal;
                    return true;

                case S_MANYREG_16t: //Not supported by DIA
                case S_MANYREG:
                case S_MANYREG_ST: //Not supported by DIA
                    throw new NotImplementedException();

                case S_LDATA32_16t: //Not supported by DIA
                case S_LDATA32:
                case S_LDATA32_ST: //Not supported by DIA
                case S_LTHREAD32_16t: //Not supported by DIA
                case S_LTHREAD32:
                case S_LTHREAD32_ST: //Not supported by DIA
                    //There is logic for these to either be DataIsStaticLocal / DataIsFileStatic however as far as I can see GetTheData::disp_S_LDATA32/disp_S_LTHREAD32 sets the relevant field to 0, the default of static local
                    //is always overwritten with file static. But this is wrong. It's a static local if the field is literally a local of a function e.g. coreclr ->
                    //[S_LDATA32] `CallComputeVTables'::`2'::s_pAddrMETHOD__COMWRAPPERS__COMPUTE_VTABLES
                    if (parent != default)
                    {
                        value = DataKind.DataIsStaticLocal;
                        return true;
                    }

                    value = DataKind.DataIsFileStatic;
                    return true;


                #region CV_LVARFLAGS

                //The following kinds have either CV_LVARFLAGS (or CV_lvar_attr which contains CV_LVARFLAGS)
                //and are set via msdia140!varAttributeFields. Strictly speaking only S_LOCAL considers whether
                //fIsParam is set, but it's technically in the flags of all of them
                case S_MANFRAMEREL:
                case S_MANFRAMEREL_ST: //Not supported by DIA
                case S_MANREGISTER:
                case S_MANREGISTER_ST: //Not supported by DIA
                case S_MANSLOT:
                case S_MANSLOT_ST: //Not supported by DIA
                case S_MANREGREL:
                case S_MANREGREL_ST: //Not supported by DIA
                    throw new NotImplementedException(); //I know they've got CV_LVARFLAGS, but I don't know what their struct is

                //The physical position of the flags in these various struct types is all over the place, so I can't just pretend they all have the same physical
                //layout and cast to one random type
                case S_ATTR_FRAMEREL:
                    value = GetLVarDataKind(((FrameRelSym) symType).attr.flags);
                    return true;

                case S_ATTR_REGISTER: //ATTRREGSYM
                    value = GetLVarDataKind(((AttrRegSym) symType).attr.flags);
                    return true;

                case S_ATTR_REGREL: //ATTRREGREL
                    value = GetLVarDataKind(((AttrRegRel) symType).attr.flags);
                    return true;

                case S_LOCAL: //LOCALSYM
                    var localSym = (LocalSym) symType;

                    //In NativeAOT, parameter names can be "___this", so let's check
                    //for that too
                    if (localSym.flags.fIsParam)
                    {
                        var name = localSym.GetName(codeViewAccessor).AsSpan();

                        if (name.SequenceEqual("this"u8) || name.SequenceEqual("___this"u8))
                        {
                            value = DataKind.DataIsObjectPtr;
                            return true;
                        }
                    }

                    value = GetLVarDataKind(((LocalSym) symType).flags);
                    return true;

                case S_FILESTATIC: //FILESTATICSYM
                    value = GetLVarDataKind(((FileStaticSym) symType).flags);
                    return true;

                case S_LOCAL_DPC_GROUPSHARED: //LOCALDPCGROUPSHAREDSYM
                    value = GetLVarDataKind(((LocalDPCGroupSharedSym) symType).flags);
                    return true;

                #endregion

                //msdia140!assignNonAttrLocalVarKind checks for the following items (we've added the ST/16-bit ones ourselves)
                case S_BPREL16:
                case S_BPREL32_16t:
                case S_BPREL32_ST:
                case S_BPREL32: // DataIsLocal / DataIsParam (if typind > 0)
                case S_BPREL32_INDIR: // DataIsLocal /  DataIsParam
                case S_BPREL32_ENCTMP:
                case S_BPREL32_INDIR_ENCTMP:
                case S_REGREL16:
                case S_REGREL32_16t:
                case S_REGREL32_ST:
                case S_REGREL32: //todo: apparently some symbols including this can have a $ and hidden text after the null terminated name?
                case S_REGREL32_INDIR: // DataIsLocal /  DataIsParam
                case S_REGREL32_ENCTMP:
                case S_REGREL32_INDIR_ENCTMP:
                    //Note: include any additional symbols in the list below, GetSymTagEnum, as well as SymHelp's LocalSymbolParser list for simple variable types
                    if (parent == null)
                    {
                        //We can walk backwards to find our parent blocksym
                        throw new NotImplementedException();
                    }

                    //DIA calls tiFuncType. Fundamentally, the parent must be a function (which then implies it has a type)
                    if (parent.IsProc())
                    {
                        if (parent.TryGetType(out var maybeFunctionType))
                        {
                            var functionType = maybeFunctionType.TypTyp.Value;

                            var numParams = 0;

                            /* When you hve a function foo(int a, ...) the Visual Studio Call Stack window correctly shows
                             * this as being the signature. How does Visual Studio know that the last argument is varargs?
                             * 
                             * - cppdebug!CppEE::CTypeFormatter::GetTypeNameForDisplay sets the parameter to ... when it's
                             *   a base type parameter of type btNoType
                             * - Unrelated to this, per msdia140!dParamsVararg considers a parameter to be varargs when
                             *   it's the last parameter of the arglist, and the type index is 0. This information is not
                             *   surfaced within DIA; DIA just uses this fact internally to reduce the number of potential
                             *   args for it to inspect */

                            switch (functionType.leaf)
                            {
                                case LEAF_ENUM_e.LF_PROCEDURE:
                                    var lfProcArgs = ((LfArgList) ((LfProc) functionType).arglist.TypTyp).arg;

                                    numParams = lfProcArgs.Count;

                                    if (numParams > 0 && lfProcArgs[lfProcArgs.Count - 1] == 0)
                                        numParams--; //Subtract varargs parameter

                                    break;

                                case LEAF_ENUM_e.LF_PROCEDURE_16t:
                                    var lfProc16tArgs = ((LfArgList16t) ((LfProc16t) functionType).arglist.TypTyp).arg;

                                    numParams = lfProc16tArgs.Count;

                                    if (numParams > 0 && lfProc16tArgs[lfProc16tArgs.Count - 1] == 0)
                                        numParams--; //Subtract varargs parameter

                                    break;

                                case LEAF_ENUM_e.LF_MFUNCTION:
                                    var lfMFuncArgs = ((LfArgList) ((LfMFunc) functionType).arglist.TypTyp).arg;

                                    //The LF_ARGLIST does not include "this" in the count, whereas the S_GPROC32
                                    //_does_ include "this" as a child
                                    numParams = lfMFuncArgs.Count;

                                    //>1 since we've got our fake "this" in our count
                                    if (numParams > 1 && lfMFuncArgs[lfMFuncArgs.Count - 1] == 0)
                                        numParams--; //Subtract varargs parameter

                                    break;

                                case LEAF_ENUM_e.LF_MFUNCTION_16t:
                                    var lfMFunc16tArgs = ((LfArgList16t) ((LfMFunc16t) functionType).arglist.TypTyp).arg;

                                    numParams = lfMFunc16tArgs.Count + 1;

                                    if (numParams > 1 && lfMFunc16tArgs[lfMFunc16tArgs.Count - 1] == 0)
                                        numParams--; //Subtract varargs parameter

                                    break;

                                default:
                                    throw new NotImplementedException();
                            }

                            var children = ((BlockSym) parent).GetChildren(codeViewModuleAccessor);

                            var numParamsSeen = 0;

                            foreach (var child in children)
                            {
                                //If we're being asked about a symbol like S_REGREL32, this is a symbol that only occurs in an "old style" context.
                                //So we don't need to consider S_LOCAL (which is new style), we just need to answer "as far as old style symbols go,
                                //is this symbol a parameter or a variable"

                                switch (child.rectyp)
                                {
                                    case S_BPREL16:
                                    case S_BPREL32_16t:
                                    case S_BPREL32_ST:
                                    case S_BPREL32:
                                    case S_BPREL32_INDIR:
                                    case S_BPREL32_ENCTMP:
                                    case S_BPREL32_INDIR_ENCTMP:
                                    case S_REGREL16:
                                    case S_REGREL32_16t:
                                    case S_REGREL32_ST:
                                    case S_REGREL32:
                                    case S_REGREL32_INDIR:
                                    case S_REGREL32_ENCTMP:
                                    case S_REGREL32_INDIR_ENCTMP:
                                        if (child == symType)
                                        {
                                            if (numParamsSeen < numParams)
                                            {
                                                value = DataKind.DataIsParam;
                                                return true;
                                            }
                                        }

                                        numParamsSeen++;
                                        break;
                                }

                                if ((SYMTYPE*) child >= (SYMTYPE*) symType)
                                    break;
                            }
                            value = DataKind.DataIsLocal;
                            return true;
                        }
                    }

                    //If we can't specifically prove that it's a parameter, then default to local
                    value = DataKind.DataIsLocal;
                    return true;

                case S_MANYREG2: //
                case S_MANYREG2_ST: //Not supported by DIA
                case S_LMANDATA: // DataIsStaticLocal / DataIsFileStatic
                case S_LMANDATA_ST: //Not supported by DIA
                case S_MANMANYREG: //CV_Lvar_attr -> CV_LVARFLAGS logic?
                case S_MANMANYREG_ST: //Not supported by DIA
                case S_MANMANYREG2: //CV_Lvar_attr -> CV_LVARFLAGS logic?
                case S_MANMANYREG2_ST: //Not supported by DIA
                case S_ATTR_MANYREG: //CV_Lvar_attr -> CV_LVARFLAGS logic?
                case S_LDATA_HLSL: // DataIsStaticLocal / DataIsFileStatic
                case S_LDATA_HLSL32: // DataIsStaticLocal / DataIsFileStatic
                case S_LDATA_HLSL32_EX: //DataIsStaticLocal / DataIsFileStatic
                case S_STATICLOCAL: //
                    throw new NotImplementedException();

                default:
                    if (symType.IsDefRangeSym())
                    {
                        //CV_LVARFLAGS

                        //The symbol should be preceeded by an S_LOCAL, S_FILESTATIC or S_LOCAL_DPC_GROUPSHARED. Get the parent symbol, and then iterate
                        //forwards until we encounter this symbol, The last S_LOCAL, S_FILESTATIC or S_LOCAL_DPC_GROUPSHARED we saw before us describes our
                        //type

                        if (parent == default)
                        {
                            codeViewModuleAccessor ??= SymbolMemoryTracker.GetModuleAccessor((long) (SYMTYPE*) symType);
                            parent = symType.GetParent(codeViewModuleAccessor);
                        }

                        if (parent != default)
                        {
                            var children = ((BlockSym) parent).GetChildren(codeViewModuleAccessor);

                            SymType owner = default;

                            foreach (var child in children)
                            {
                                switch (child.rectyp)
                                {
                                    case S_LOCAL:
                                        owner = child;
                                        break;

                                    case S_FILESTATIC:
                                        owner = child;
                                        break;

                                    case S_LOCAL_DPC_GROUPSHARED:
                                        owner = child;
                                        break;

                                    default:
                                        if ((SYMTYPE*) child == (SYMTYPE*) symType)
                                        {
                                            if (owner != null)
                                                return owner.TryGetDataKind(out value, parent, codeViewAccessor, codeViewModuleAccessor);

                                            //Fail
                                            value = default;
                                            return false;
                                        }
                                        break;
                                }

                                //Fail
                            }
                        }
                    }

                    value = default;
                    return false;
            }
        }

        private static DataKind GetLVarDataKind(CV_LVARFLAGS flags)
        {
            if (flags.fIsEnregGlob)
                return flags.fIsEnregStat ? DataKind.DataIsFileStatic : DataKind.DataIsGlobal;

            return flags.fIsParam ? DataKind.DataIsParam : DataKind.DataIsLocal;
        }
    }
}
