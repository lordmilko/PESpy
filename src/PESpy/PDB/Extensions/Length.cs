using System;
using System.Runtime.CompilerServices;
using ClrDebug.DIA;
using ClrDebug.PDB;
using static ClrDebug.PDB.LEAF_ENUM_e;
using static ClrDebug.PDB.SYM_ENUM_e;

namespace PESpy.PDB
{
    public static partial class TypTypeExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetLength(this TypType typType, out int length) =>
            TryGetLength((LfEasy) typType, out length);

        public static bool TryGetLength(this LfEasy lfEasy, out int length)
        {
            switch (lfEasy.leaf)
            {
                case LF_ENUM:
                case LF_ENUM_ST: //Not supported by DIA
                    return TryGetLength(((LfEnum) lfEasy).utype, out length);

                case LF_ENUM_16t:
                    throw new NotImplementedException();

                case LF_POINTER_16t:
                    throw new NotImplementedException();

                case LF_POINTER:
                    var lfPointer = (LfPointer) lfEasy;

                    //msdia140!getPtrData first tries to use the size specified in the record,
                    //but if the size is 0 it looks at the pointer mode instead

                    var ptr = (LfPointer) lfEasy;
                    var size = ptr.attr.size;

                    if (size != 0)
                        length = size;
                    else
                        length = ptr.attr.ptrtype == CV_ptrtype_e.CV_PTR_64 ? 8 : 4;

                    return true;

                //LfArray
                case LF_ARRAY:
                case LF_ARRAY_ST:
                    length = ((LfArray) lfEasy).length;
                    return true;

                //LfArray16t
                case LF_ARRAY_16t:
                    length = ((LfArray16t) lfEasy).length;
                    return true;

                //LfBitfield
                case LF_BITFIELD:
                    length = ((LfBitfield) lfEasy).length;
                    return true;

                //LfBitfield16t
                case LF_BITFIELD_16t:
                    length = ((LfBitfield16t) lfEasy).length;
                    return true;

                //LfClass
                case LF_CLASS:
                case LF_CLASS_ST:
                case LF_INTERFACE:
                case LF_STRUCTURE:
                case LF_STRUCTURE_ST:
                    /* NativeAOT LF_CLASS information is completely broken;
                     * they report that all LF_CLASS instances are the size of a pointer,
                     * on the basis that these are reference types
                     * This was fixed here https://github.com/dotnet/runtime/pull/115293 on May 5th, 2025,
                     * however it's not our job to take care of this; PDBFile merely exposes what's contained
                     * in the PDB. Consumers need to wrap the data contained in PDBFile into coherent "symbols"
                     * that account for any issues like this */
                    length = ((LfClass) lfEasy).length;
                    return true;

                //LfClass16t
                case LF_CLASS_16t:
                case LF_STRUCTURE_16t:
                    length = ((LfClass16t) lfEasy).length;
                    return true;

                //LfUnion
                case LF_UNION:
                case LF_UNION_ST:
                    length = ((LfUnion) lfEasy).length;
                    return true;

                //LfUnion16t
                case LF_UNION_16t:
                    length = ((LfUnion16t) lfEasy).length;
                    return true;
            }

            length = default;
            return false;
        }
    }

    public static partial class SymTypeExtensions
    {
        /// <summary>
        /// <inheritdoc cref="IDiaSymbol.get_length"/><para/>
        /// Corresponds to <see cref="IDiaSymbol.get_length"/>
        /// </summary>
        public static unsafe bool TryGetLength(in this SymType symType, out int length, ICodeViewAccessor? codeViewAccessor = null)
        {
            //I'm only seeing a "len" property on block sym types. I haven't checked what DIA does.
            //However, block symbols only share the first 4 members, so we can't just cast to BlockSym16/32.
            //We need to switch

            switch (symType.rectyp)
            {
                case S_THUNK16: //Not supported by DIA
                    length = ((ThunkSym16) symType).len;
                    return true;

                case S_THUNK32_ST: //Not supported by DIA
                case S_THUNK32:
                    length = ((ThunkSym32) symType).len;
                    return true;

                case S_BLOCK16: //Not supported by DIA
                case S_WITH16: //Not supported by DIA
                    length = ((BlockSym16) symType).len;
                    return true;

                case S_BLOCK32_ST: //Not supported by DIA
                case S_BLOCK32:
                case S_WITH32_ST: //Not supported by DIA
                case S_WITH32:
                    length = ((BlockSym32) symType).len;
                    return true;

                case S_LPROC16: //Not supported by DIA
                case S_GPROC16: //Not supported by DIA
                    length = ((ProcSym16) symType).len;
                    return true;

                case S_LPROC32_ST: //Not supported by DIA
                case S_LPROC32:
                case S_GPROC32_ST: //Not supported by DIA
                case S_GPROC32:
                case S_LPROC32_DPC:
                    length = ((ProcSym32) symType).len;
                    return true;

                case S_GPROC32_16t: //Not supported by DIA
                    length = ((ProcSym3216t) symType).len;
                    return true;

                case S_LPROCMIPS_16t: //Not supported by DIA
                case S_GPROCMIPS_16t: //Not supported by DIA
                    length = ((ProcSymMips16t) symType).len;
                    return true;

                case S_LPROCMIPS_ST: //Not supported by DIA
                case S_LPROCMIPS:
                case S_GPROCMIPS_ST: //Not supported by DIA
                case S_GPROCMIPS:
                    length = ((ProcSymMips) symType).len;
                    return true;

                case S_LPROCIA64_ST://Not supported by DIA
                case S_LPROCIA64:
                case S_GPROCIA64:
                case S_GPROCIA64_ST: //Not supported by DIA
                    length = ((ProcSymIA64) symType).len;
                    return true;

                case S_GMANPROC:
                case S_GMANPROC_ST: //Not supported by DIA
                case S_LMANPROC:
                case S_LMANPROC_ST: //Not supported by DIA
                    length = ((ManProcSym) symType).len;
                    return true;

                //DIA has lots of logic all over the case for special casing S_TRAMPOLINE. S_TRAMPOLINE does not
                //have a corresponding S_END symbol however, and should not be considered a block
                case S_SEPCODE:
                    length = ((SepCodeSym) symType).length;
                    return true;

                //case S_LPROC32_ID:
                //case S_GPROC32_ID:
                //case S_LPROCMIPS_ID:
                //case S_GPROCMIPS_ID:
                //case S_LPROCIA64_ID:
                //case S_GPROCIA64_ID:

                //I think this requires full analysis of the inline symbol
                //case S_INLINESITE:

                //case S_LPROC32_DPC_ID:

                //I think this requires full analysis of the inline symbol
                //case S_INLINESITE2:

                //Not publically documented, but used by DIA
                //case S_GPROC32EX:
                //case S_LPROC32EX:
                //case S_GPROC32EX_ID:
                //case S_LPROC32EX_ID:

                case S_PUB16:
                {
                    codeViewAccessor ??= SymbolMemoryTracker.GetAccessor((long) (SYMTYPE*) symType);
                    var dataSym16 = (DataSym16) symType;
                    var name = dataSym16.GetName(codeViewAccessor);
                    return TryGetPublicLength(symType, name, codeViewAccessor, dataSym16.off, dataSym16.seg, out length);
                }

                case S_PUB32_16t:
                {
                    codeViewAccessor ??= SymbolMemoryTracker.GetAccessor((long) (SYMTYPE*) symType);
                    var dataSym3216t = (DataSym3216t) symType;
                    var name = dataSym3216t.GetName(codeViewAccessor);
                    return TryGetPublicLength(symType, name, codeViewAccessor, dataSym3216t.off, dataSym3216t.seg, out length);
                }

                case S_PUB32_ST:
                case S_PUB32:
                {
                    codeViewAccessor ??= SymbolMemoryTracker.GetAccessor((long) (SYMTYPE*) symType);
                    var pubSym32 = (PubSym32) symType;
                    var name = pubSym32.GetName(codeViewAccessor);
                    return TryGetPublicLength(symType, name, codeViewAccessor, pubSym32.off, pubSym32.seg, out length);
                }

                case S_TRAMPOLINE:
                    length = ((TrampolineSym) symType).cbThunk;
                    return true;

                default:
                    //If it's a thing with an off/seg, fallback to probing the section contrib.
                    //I'm not sure if this goes beyond what DIA does
                    if (symType.TryGetRawOffSeg(out var off, out var seg))
                    {
                        codeViewAccessor ??= SymbolMemoryTracker.GetAccessor((long) (SYMTYPE*) symType);

                        if (codeViewAccessor.TryGetSectionContrib(symType, seg, off, out var sc))
                        {
                            length = sc.cb - (off - sc.off);
                            return true;
                        }
                    }
                    break;
            }

            length = default;
            return false;
        }

        private static bool TryGetPublicLength(
            SymType symType,
            FixedUtf8String name,
            ICodeViewAccessor codeViewAccessor,
            int off,
            ISECT seg,
            out int length)
        {
            //I believe you can just look at the length of the associated section contrib

            //For @ILT symbols, DIA reports the length as 0.
            //I can see DIA looking for @ILT in SymbolDataSimpleImpl<4366,10>::getData
            //so I'm guessing the name is set somewhere in there. However, I would present that
            //in fact the actual length you should use is 5 (or whatever the listed thunk size is)

            length = default;

            if (name.StartsWith("@ILT"))
            {
                if (codeViewAccessor is PDBFile pdbFile)
                {
                    var psgsi = pdbFile.PSGSI;

                    if (psgsi != null)
                    {
                        length = psgsi.PSGsiHdr.cbSizeOfThunk;
                        return true;
                    }
                }

                //Returning a length of 0 and true is troublesome; don't do what DIA does
                length = default;
                return false;
            }

            if (codeViewAccessor != null)
            {
                /* I know that DIA does something to do with looking up the section contrib associated with the public
                 * for reporting the public's length, but I'm not sure if it just blindly reports "the entire SC length
                 * is the length" or if it computes the length _remaining_ in the SC after where the current symbol starts.
                 * I feel like the logical thing to do is to compute the appropriate offset, so we'll do that */
                if (codeViewAccessor.TryGetSectionContrib(symType, seg, off, out var sc))
                    length = sc.cb - (off - off);
                else
                    length = default;

                if (codeViewAccessor is PDBFile f)
                {
                    /* An additional check we can potentially do is to lookup what the address of the next item in the address map is. The distance
                     * between the current symbol and that also gives us a length; whichever length is shorter (the section contrib or the address map)
                     * length should be our reported length */

                    var addressMap = f.PSGSI?.AddressMapSymbols;

                    if (addressMap != null)
                    {
                        addressMap.BinarySearchAddressMap(off, seg, out _, out var virtualLow, out _);

                        //Watch out, because the next symbol might be at the same address as well!
                        while (virtualLow < addressMap.VirtualCount - 1)
                        {
                            var nextSym = addressMap.GetVirtualSymbol(virtualLow + 1);

                            if (!nextSym.TryGetRawOffSeg(out var nextOff, out var nextSeg))
                                break;

                            if (nextSeg == seg)
                            {
                                if (nextOff == off)
                                {
                                    //Woops, this symbol is at the exact same address!
                                    virtualLow++;
                                    continue;
                                }

                                //OK, we've got a different address, now check whether our SC or address map
                                //info is better

                                var lengthToNext = nextOff - off;

                                //I've confirmed this does indeed help with vftables in big section contribs
                                length = length == 0 ? lengthToNext : Math.Min(lengthToNext, length);
                                return true;
                            }
                            else
                                break;
                        }
                    }
                }

                return length != 0;
            }

            return false;
        }
    }
}
