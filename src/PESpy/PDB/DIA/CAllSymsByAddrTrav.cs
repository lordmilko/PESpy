using System;
using System.Diagnostics;

namespace PESpy.PDB
{
    /// <summary>
    /// Models the essential functionality of the msdia140!CAllSymsByAddrTrav class used
    /// for resolving an RVA to its "nearest" symbol.
    /// </summary>
    internal abstract class CAllSymsByAddrTrav
    {
        internal readonly SymCache _symCache;
        protected ISECT _targetSeg;
        protected int _targetOff;

        internal CAllSymsByAddrTrav(SymCache symCache)
        {
            _symCache = symCache;
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

            var pubsTraverser = new CPubByAddrTrav(this, targetOffSeg, bestResult.offSegSym);
            findBetterSymbol(pubsTraverser, ref bestResult, targetOffSeg);

            var blockTraverser = new CBlockByAddrTrav(this, targetOffSeg, bestResult.offSegSym);
            findBetterSymbol(blockTraverser, ref bestResult, targetOffSeg);

            var dataTraverser = new CDataByAddrTrav(this, targetOffSeg, bestResult.offSegSym);
            findBetterSymbol(dataTraverser, ref bestResult, targetOffSeg);

            var globalDataTraverser = new CGlobalDataByAddrTrav(this, targetOffSeg, bestResult.offSegSym);
            findBetterSymbol(globalDataTraverser, ref bestResult, targetOffSeg);

            return bestResult.offSegSym.symType != default;
        }

        private unsafe bool findNextAddress(out TraverserResult result)
        {
            result = default;

            if (!EnumByAddrLocate(_targetSeg, _targetOff))
                return false;

            OffSeg pubOffSeg = new OffSeg
            {
                off = -1,
                seg = ISECT.Nil
            };

            if (EnumByAddrNext(out var pubSym))
            {
                do
                {
                    pubSym.TryGetOffSeg(out var candidateOff, out var candidateSeg);

                    if ((_targetSeg < candidateSeg || _targetSeg == candidateSeg && _targetOff <= candidateOff) && (_targetSeg != candidateSeg || _targetOff != candidateOff))
                    {
                        pubOffSeg.off = candidateOff;
                        pubOffSeg.seg = candidateSeg;
                        break;
                    }

                } while (EnumByAddrNext(out pubSym));
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

            EnumContribLocate(_targetSeg, _targetOff);

            if (!EnumContribNext(out var sc))
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

                if (!EnumContribNext(out sc))
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

        private static void findBetterSymbol(Traverser traverser, ref TraverserResult bestResult, OffSeg targetOffSeg)
        {
            if (traverser.next(out var candidate))
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

        #region Impl

        public abstract bool EnumByAddrLocate(ISECT seg, int off);

        public abstract bool EnumByAddrNext(out SymType symType);

        public abstract bool EnumContribLocate(ISECT seg, int off);

        public abstract bool EnumContribNext(out SC40 sc);

        public abstract bool EnumContribPrev(out SC40 sc);

        #endregion
    }
}
