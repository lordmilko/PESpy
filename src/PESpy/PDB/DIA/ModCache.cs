using System;
using System.Collections.Generic;
using System.Linq;
using ClrDebug.PDB;
using static ClrDebug.PDB.SYM_ENUM_e;

namespace PESpy.PDB
{
    internal abstract class ModCache
    {
        protected abstract SymTypeList Symbols { get; }

        private OffSegSym[] _functionSymbols;
        private OffSegSym[] _dataSymbols;

        #region Block

        public bool blockByAddr(ISECT targetSeg, int targetOff, out OffSegSym offSegSym)
        {
            fInitFuncPositionCache();

            return TryBinarySearchSymbols(_functionSymbols, targetSeg, targetOff, out offSegSym);
        }

#if FALSE
        //Not part of DIA; abstracts the manual lookup logic out of blockByAddr
        private bool blockByAddrManual(ISECT targetSeg, int targetOff, out OffSegSym offSegSym)
        {
            //We will never actually use this implementation however because
            //unlike DIA, we always go for the binary search regardless of the number of entries we have. This is just recorded here
            //for posterity

            var symbols = Symbols;

            SymType bestSymbol = default;
            int bestOffset = 0;
            ushort bestSeg = 0;

            //ModCache::blockByAddr
            foreach (var symType in symbols.GetTopLevel())
            {
                /* There is logic all over the place in DIA for special casing S_TRAMPOLINE symbols.
                 * S_TRAMPOLINE symbols are emitted in the * Linker * module in Debug builds for each
                 * ILT thunk that is also referenced in Publics. Due to the difference in bounding rules
                 * used for searching publics vs module symbols, in the event you're asking for an address 1 past
                 * the end of an ILT thunk, you won't get it if you ask for it via publics, but will if you ask
                 * via the module.
                 */

                if (symType.IsBlockSym() || symType.rectyp == SYM_ENUM_e.S_TRAMPOLINE)
                {
                    if (!symType.TryGetRawOffSeg(out var candidateOff, out var candidateSeg) || !symType.TryGetLength(out var length))
                        continue;

                    if (candidateSeg < targetSeg || candidateSeg == targetSeg && candidateOff <= targetOff)
                    {
                        uint displacement = targetSeg == candidateSeg
                            ? (uint) (targetOff - candidateOff)
                            : uint.MaxValue;

                        //Note: there's an issue with manual search in that we take the first symbol we find. But if there's
                        //multiple matches, the sort you get when you add them to a list may be slightly different. And in particular,
                        //it seems that DIA's quick sort produces a different order than what C# produces
                        if (displacement < length)
                        {
                            //We're inside the symbol; this is a perfect match
                            offSegSym = new OffSegSym
                            {
                                off = candidateOff,
                                seg = candidateSeg,
                                symType = symType
                            };
                            return true;
                        }

                        //IDA shows a duplicated if statement after this, however my analysis is we can merge
                        //what that if statement does into this. Note that while target offset is spilled
                        //into a local variable in multiple control flow paths, the value of target offset
                        //never changes

                        //The first time we have a "best imperfect match" bestSeg will be 0, so we'll use uint.MaxValue
                        //here. If targetSeg == candidateSeg above, displacement < bestDisplacement will return true,
                        //and that's how we set our initial value
                        uint bestDisplacement =
                            targetSeg == bestSeg
                            ? (uint) (targetOff - bestOffset)
                            : uint.MaxValue;

                        if (displacement < bestDisplacement)
                        {
                            bestSymbol = symType;
                            bestOffset = candidateOff;
                            bestSeg = candidateSeg;
                        }
                    }
                }
            }

            if (bestSymbol != default)
            {
                offSegSym = new OffSegSym
                {
                    off = bestOffset,
                    seg = bestSeg,
                    symType = bestSymbol
                };
                return true;
            }

            offSegSym = default;
            return false;
        }
#endif

        private void fInitFuncPositionCache()
        {
            //DIA disallows using a cache if the entire module's symbols are less than 1024 bytes, if it's a minimal PDB or it's ENC

            if (_functionSymbols != null)
                return;

            var symbols = Symbols;

            if (symbols == null)
            {
                _functionSymbols = Array.Empty<OffSegSym>();
                return;
            }

            var dict = new Dictionary<ulong, OffSegSym>();

            foreach (var symType in symbols.GetTopLevel())
            {
                if (symType.IsBlockSym() || symType.rectyp == SYM_ENUM_e.S_TRAMPOLINE)
                {
                    symType.TryGetRawOffSeg(out var off, out var seg);

                    var key = (ulong) seg << 32 | (uint) off;

                    //ModCache maintains a special cache of sepcode sym to parent function symbol,
                    //however I don't know any reason why we would need that yet (DIA doesn't use
                    //this when resolving lexical parents, so that's not a reason)

                    if (!dict.ContainsKey(key))
                    {
                        dict.Add(key, new OffSegSym
                        {
                            off = off,
                            seg = seg,
                            symType = symType
                        });
                    }
                }
            }

            _functionSymbols = FinalizeArray(dict);
            return;
        }

#endregion
        #region data

        public bool dataByAddr(ISECT targetSeg, int targetOff, out OffSegSym offSegSym)
        {
            fInitDataPositionCache();

            return TryBinarySearchSymbols(_dataSymbols, targetSeg, targetOff, out offSegSym);
        }

#if FALSE
        private bool dataByAddrManual(ISECT targetSeg, int targetOff, out OffSegSym offSegSym)
        {
            //DIA only cares about S_LDATA32 and S_GDATA32. It only cares about S_STATICLOCAL in the event we're a minimal PDB.
            //We want to consider labels and thread data as well; we will never actually use this implementation however because
            //unlike DIA, we always go for the binary search regardless of the number of entries we have. This is just recorded here
            //for posterity

            var symbols = Symbols;

            SymType bestSymbol = default;
            int bestOffset = 0;
            ushort bestSeg = 0;

            foreach (var symType in symbols)
            {
                int candidateOff;
                ISECT candidateSeg;

                switch (symType.rectyp)
                {
                    //DIA ignores symbols that don't have a name. We can't just do that, because
                    //a length prefixed string won't start with a \0, so we need to defer this check
                    //until the symbol is looking good

                    //DataSym16
                    case S_LDATA16:
                    case S_GDATA16:
                        var dataSym16 = (DataSym16) symType;
                        candidateSeg = dataSym16.seg;
                        candidateOff = dataSym16.off;
                        break;

                    //DataSym3216t
                    case S_LDATA32_16t:
                    case S_GDATA32_16t:
                        var dataSym3216t = (DataSym3216t) symType;
                        candidateSeg = dataSym3216t.seg;
                        candidateOff = dataSym3216t.off;
                        break;

                    //DataSym32
                    case S_LDATA32:
                    case S_GDATA32:
                        var dataSym32 = (DataSym32) symType;
                        candidateSeg = dataSym32.seg;
                        candidateOff = dataSym32.off;
                        break;

                    default:
                        continue;
                }

                //Similar logic to what we use for resolving function symbols, however there's no "length"
                //to tell us that we're inside the symbol, and we also need to check whether the name is correct
                if (candidateSeg < targetSeg || candidateSeg == targetSeg && candidateOff <= targetOff)
                {
                    uint displacement = targetSeg == candidateSeg
                        ? (uint) (targetOff - candidateOff)
                        : uint.MaxValue;

                    uint bestDisplacement =
                        targetSeg == bestSeg
                        ? (uint) (targetOff - bestOffset)
                        : uint.MaxValue;

                    //Defer getting the name until the last moment to avoid having to try and lookup the CodeViewAccessor
                    if (displacement < bestDisplacement && symType.GetName().Length > 0)
                    {
                        bestSymbol = symType;
                        bestOffset = candidateOff;
                        bestSeg = candidateSeg;
                    }
                }
            }

            if (bestSymbol != default)
            {
                offSegSym = new OffSegSym
                {
                    off = bestOffset,
                    seg = bestSeg,
                    symType = bestSymbol
                };
                return true;
            }

            offSegSym = default;
            return false;
        }
#endif

        //Name is made up; actual implementation is inline in dataByAddr
        private void fInitDataPositionCache()
        {
            //DIA disallows using a cache if the entire module's symbols are less than 1024 bytes, if it's a minimal PDB or it's ENC

            if (_dataSymbols != null)
                return;

            var symbols = Symbols;

            if (symbols == null)
            {
                _dataSymbols = Array.Empty<OffSegSym>();
                return;
            }

            var dict = new Dictionary<ulong, OffSegSym>();

            foreach (var symType in symbols)
            {
                int candidateOff;
                ISECT candidateSeg;

                switch (symType.rectyp)
                {
                    //DIA doesn't actually seem to support labels at all; even though you can search for SymTagLabel, you don't
                    //seem to get any results, and when you specify an RVA to search for, the best you'll get is a public symbol.
                    //I think this is no good, so we've shoved label support in here

                    //Note that GTHREAD support doesn't go here, it goes in SymCache; you don't find GTHREAD inside a module

                    //LabelSym16
                    case S_LABEL16:
                        var labelSym16 = (LabelSym16) symType;

                        if (labelSym16.name.Length == 0)
                            continue;

                        candidateSeg = labelSym16.seg;
                        candidateOff = labelSym16.off;
                        break;

                    //LabelSym32
                    case S_LABEL32_ST:
                    case S_LABEL32:
                        var labelSym32 = (LabelSym32) symType;

                        if (labelSym32.name.Length == 0)
                            continue;

                        candidateSeg = labelSym32.seg;
                        candidateOff = labelSym32.off;
                        break;

                    //DataSym16
                    case S_LDATA16:
                    case S_GDATA16:
                        var dataSym16 = (DataSym16) symType;

                        if (dataSym16.name.Length == 0)
                            continue;

                        candidateSeg = dataSym16.seg;
                        candidateOff = dataSym16.off;
                        break;

                    //DataSym3216t
                    case S_LDATA32_16t:
                    case S_GDATA32_16t:
                        var dataSym3216t = (DataSym3216t) symType;

                        if (dataSym3216t.name.Length == 0)
                            continue;

                        candidateSeg = dataSym3216t.seg;
                        candidateOff = dataSym3216t.off;
                        break;

                    //DataSym32
                    case S_LDATA32:
                    case S_LDATA32_ST:
                    case S_GDATA32:
                    case S_GDATA32_ST:
                    case S_GTHREAD32:
                    case S_GTHREAD32_ST: //DIA doesn't seem to support global thread data either, but we do
                        var dataSym32 = (DataSym32) symType;

                        if (dataSym32.name.Length == 0)
                            continue;

                        candidateSeg = dataSym32.seg;
                        candidateOff = dataSym32.off;
                        break;

                    default:
                        continue;
                }

                var key = (ulong) candidateSeg << 32 | (uint) candidateOff;

                if (!dict.ContainsKey(key))
                {
                    dict.Add(key, new OffSegSym
                    {
                        off = candidateOff,
                        seg = candidateSeg,
                        symType = symType
                    });
                }
            }

            _dataSymbols = FinalizeArray(dict);
            return;
        }

#endregion

        private bool TryBinarySearchSymbols(
            OffSegSym[] symbols,
            ISECT targetSeg,
            int targetOff,
            out OffSegSym offSegSym)
        {
            var hi = symbols.Length;
            var lo = 0;

            //I think this might be a half open interval search or something
            while (hi > 0)
            {
                var mid = hi / 2;

                var item = symbols[lo + mid];

                if (targetSeg < item.seg || (targetSeg == item.seg && targetOff < item.off))
                    hi = mid;
                else
                {
                    lo = lo + mid + 1;
                    hi = hi - mid - 1;
                }
            }

            //I'm not sure if I inadvertantly fixed this, but it seems to me that there may be a difference between us and DIA
            //based on the quicksort algorithm DIA used to sort their list of symbols in the first place.
            //Not sure what we can do about this without reversing how exactly their sort algorithm works
            if (lo > 0)
            {
                offSegSym = symbols[lo - 1];
                return true;
            }

            offSegSym = default;
            return false;
        }

        private OffSegSym[] FinalizeArray(Dictionary<ulong, OffSegSym> dict)
        {
            var arr = dict.Values.ToArray();
            Array.Sort(arr, (a, b) =>
            {
                var diff = a.seg.CompareTo(b.seg);

                if (diff != 0)
                    return diff;

                return a.off.CompareTo(b.off);
            });

            return arr;
        }
    }
}
