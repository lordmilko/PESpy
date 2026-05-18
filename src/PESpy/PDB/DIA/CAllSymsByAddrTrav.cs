using System;
using System.Diagnostics;

namespace PESpy.PDB
{
    interface IEnumProvider
    {
        SymCache SymCache { get; }

        bool EnumByAddrLocate(ISECT seg, int off);

        bool EnumByAddrNext(out SymType symType);

        bool EnumContribLocate(ISECT seg, int off);

        bool EnumContribNext(out SC40 sc);

        bool EnumContribPrev(out SC40 sc);
    }

    /// <summary>
    /// Models the essential functionality of the msdia140!CAllSymsByAddrTrav class used
    /// for resolving an RVA to its "nearest" symbol.
    /// </summary>
    internal struct CAllSymsByAddrTrav<TEnumProvider> where TEnumProvider : IEnumProvider
    {
        private ISECT _targetSeg;
        private int _targetOff;
        private TEnumProvider _enumProvider;

        internal CAllSymsByAddrTrav(TEnumProvider enumProvider)
        {
            _enumProvider = enumProvider;
        }

        public bool FInit(
            ISECT targetSeg,
            int targetOff,
            out TraverserResult bestResult)
        {
            _targetSeg = targetSeg;
            _targetOff = targetOff;

            /* DIA calls PSGSI1::GetEnumByAddr. If this fails, this is a fatal error.
             * GetEnumByAddr fails if PSGSI1::readThunkMap fails. The only scenario
             * in which readThunkMap may fail is if its stream number is snNil, or it somehow
             * fails to read all of the required data from the PDB. Having 0 thunks is not grounds
             * for failure. */

            /* Next, DIA calls DBI1::getEnumContrib. The only failure scenarios are when the PDB is corrupt.
             * DIA enforces that you call either DBI1::getEnumContrib or DBI1::getEnumContrib2 based on whether or not
             * fSCv2 would be set. We don't have to worry about that, since we have a unified "base class" for section
             * contribs in our API. */

            //Once we're initialized, DIA dispatches to init() to handle actually running the show
            return init(targetSeg, targetOff, out bestResult);
        }

        private bool init(ISECT targetSeg, int targetOff, out TraverserResult bestResult)
        {
            //Init scans the ModCache symbols for something I don't understand,
            //then calls getEnclosingSymbol

            _targetSeg = targetSeg;
            _targetOff = targetOff;

            var offSeg = new OffSeg(targetOff, targetSeg);

            if (getEnclosingSymbol(offSeg, out bestResult))
                return true;

            //If no symbol was found, DIA calls findNextAddress
            return findNextAddress(out bestResult);
        }

        private bool getEnclosingSymbol(OffSeg targetOffSeg, out TraverserResult bestResult)
        {
            //getEnclosingSymbol creates a traverser for pubs, block, data
            //and global data, and attempts them in that order. It continues doing
            //this unless it encounters a fatal error. We don't have fatal errors,
            //so we can simplify things

            bestResult = default;

            var pubsTraverser = new CPubByAddrTrav<TEnumProvider>(_enumProvider, targetOffSeg, bestResult.offSegSym);

            //next() is normally called in findBetterSymbol, but we've pulled it out so we can eliminate allocations
            //without increasing code size from generics
            if (pubsTraverser.next(out var candidate))
                findBetterSymbol(candidate, ref bestResult, targetOffSeg);

            var blockTraverser = new CBlockByAddrTrav<TEnumProvider>(_enumProvider, targetOffSeg, bestResult.offSegSym);

            if (blockTraverser.next(out candidate))
                findBetterSymbol(candidate, ref bestResult, targetOffSeg);

            var dataTraverser = new CDataByAddrTrav<TEnumProvider>(_enumProvider, targetOffSeg, bestResult.offSegSym, label: false);

            if (dataTraverser.next(out candidate))
                findBetterSymbol(candidate, ref bestResult, targetOffSeg);

            var globalDataTraverser = new CGlobalDataByAddrTrav<TEnumProvider>(_enumProvider, targetOffSeg);

            if (globalDataTraverser.next(out candidate))
                findBetterSymbol(candidate, ref bestResult, targetOffSeg);

            /* DIA doesn't actually seem to support labels at all; even though you can search for SymTagLabel, you don't
             * seem to get any results, and when you specify an RVA to search for, the best you'll get is a public symbol.
             * It's a bit of a tricky situation, because there are times when you _do_ want to get labels, and times that you _don't_.
             * Also, I'm not sure whether asking only for labels may result in a spurious match if it's the only thing we're looking for.
             * I thought that only going for labels when you have an imperfect match might work, but that's no good when we're really just
             * looking for a top level entity. As such, for now label support has been commented out. FileAccessor will really want labels
             * however to show inside code! */

            //if (bestResult.offSegSym.off != targetOffSeg.off || bestResult.offSegSym.seg != targetOffSeg.seg)
            //{
            //    //Try for a label

            //    dataTraverser = new CDataByAddrTrav<TEnumProvider>(_enumProvider, targetOffSeg, bestResult.offSegSym, label: true);

            //    if (dataTraverser.next(out candidate))
            //        findBetterSymbol(candidate, ref bestResult, targetOffSeg);
            //}

            return bestResult.offSegSym.symType != default;
        }

        private unsafe bool findNextAddress(out TraverserResult result)
        {
            result = default;

            if (!_enumProvider.EnumByAddrLocate(_targetSeg, _targetOff))
                return false;

            OffSeg pubOffSeg = new OffSeg
            {
                off = -1,
                seg = ISECT.Nil
            };

            if (_enumProvider.EnumByAddrNext(out var pubSym))
            {
                do
                {
                    pubSym.TryGetRawOffSeg(out var candidateOff, out var candidateSeg);

                    if ((_targetSeg < candidateSeg || _targetSeg == candidateSeg && _targetOff <= candidateOff) && (_targetSeg != candidateSeg || _targetOff != candidateOff))
                    {
                        pubOffSeg.off = candidateOff;
                        pubOffSeg.seg = candidateSeg;
                        break;
                    }

                } while (_enumProvider.EnumByAddrNext(out pubSym));
            }
            else
            {
                throw new NotImplementedException();
            }

            OffSeg contribOffSeg = new OffSeg
            {
                off = -1,
                seg = ISECT.Nil
            };

            _enumProvider.EnumContribLocate(_targetSeg, _targetOff);

            if (!_enumProvider.EnumContribNext(out var sc))
                throw new NotImplementedException();

            while (true)
            {
                var scSeg = sc.isect;
                var scOff = sc.off;

                contribOffSeg.seg = scSeg;
                contribOffSeg.off = scOff;

                if ((_targetSeg < scSeg || _targetSeg == scSeg && _targetOff <= scOff)
                    && (_targetSeg != scSeg || _targetOff != scOff)
                    && sc.dwCharacteristics != 0)
                {
                    break;
                }

                if (!_enumProvider.EnumContribNext(out sc))
                    break;
            }

            OffSeg initialQueryOffSeg = pubOffSeg;

            if (contribOffSeg.seg < pubOffSeg.seg ||
                (contribOffSeg.seg == pubOffSeg.seg && contribOffSeg.off < pubOffSeg.off))
            {
                initialQueryOffSeg = contribOffSeg;
            }

            if (initialQueryOffSeg.off == -1 && initialQueryOffSeg.seg == ISECT.Nil)
                return false;

            var hasValue = false;

            var queryOffSeg = initialQueryOffSeg;

            var effectiveTargetOff = _targetOff;

            TraverserResult bestResult = default;

            var hadAnyResult = false;

            while (true)
            {
                bool gotSymbol;

                if ((gotSymbol = getEnclosingSymbol(queryOffSeg, out result)))
                {
                    bestResult = result;
                    hadAnyResult = true;
                    queryOffSeg = new OffSeg(result.offSegSym.off, result.offSegSym.seg);
                }

                if (!gotSymbol || queryOffSeg.seg < _targetSeg
                    || (queryOffSeg.seg == _targetSeg) && queryOffSeg.off <= _targetOff)
                {
                    if (!hasValue)
                        throw new NotImplementedException();

                    if ((_targetSeg < initialQueryOffSeg.seg || _targetSeg == initialQueryOffSeg.seg && _targetOff <= initialQueryOffSeg.off)
                        && new OffSeg(effectiveTargetOff, _targetSeg) != initialQueryOffSeg
                        && initialQueryOffSeg.seg <= 0xFFFF
                        && initialQueryOffSeg != new OffSeg(-1, ISECT.Nil))
                    {
                        Debug.Assert(hadAnyResult);
                        result = bestResult;
                        return true;
                    }

                    result = default;
                    return false;
                }

                initialQueryOffSeg = queryOffSeg;
                queryOffSeg.off--;
                hasValue = true;

                //If we've rewound past the start of the section, go to the previous section
                if (queryOffSeg.off == -1)
                {
                    queryOffSeg.off = int.MaxValue; //Max signed value, should not be unsigned
                    queryOffSeg.seg -= (queryOffSeg.seg != 0 ? 1 : 0);
                }
            }

            throw new NotImplementedException();
        }

        private static void findBetterSymbol(in TraverserResult candidate, ref TraverserResult bestResult, OffSeg targetOffSeg)
        {
            var candidateSeg = candidate.offSegSym.seg;
            var candidateOff = candidate.offSegSym.off;

            var bestSeg = bestResult.offSegSym.seg;
            var bestOff = bestResult.offSegSym.off;

            var targetSeg = targetOffSeg.seg;
            var targetOff = targetOffSeg.off;

            if ((bestSeg < candidateSeg || (bestSeg == candidateSeg && bestOff <= candidateOff))
                && (candidateSeg < targetSeg || (candidateSeg == targetSeg && candidateOff <= targetOff))
                && (bestSeg != candidateSeg || bestOff != candidateOff || candidate.hasName))
            {
                bestResult = candidate;
            }
        }
    }
}
