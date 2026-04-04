namespace PESpy.PDB
{
    /// <summary>
    /// Models the essential functionality of the msdia140!CBlockByAddrTrav class used
    /// for resolving an RVA to its "nearest" block symbol at or under a function symbol
    /// within a specific module.
    /// </summary>
    internal class CBlockByAddrTrav : CFuncByAddrTrav
    {
        public CBlockByAddrTrav(
            CAllSymsByAddrTrav parentTrav,
            OffSeg targetOffSeg,
            OffSegSym bestOffSeg) : base(parentTrav, targetOffSeg, bestOffSeg)
        {
        }

        public override bool next(out TraverserResult result)
        {
            if (FInit(out result))
            {
                //Need to check length because block has a name but it's empty
                result.hasName = result.offSegSym.symType.TryGetName(out var name) && name.Length > 0; //todo: use codeviewaccessor?
                return true;
            }

            result = default;
            return false;
        }

        internal override bool FInit(out TraverserResult result)
        {
            if (!base.FInit(out result))
                return false;

            _parentTrav._symCache.GetModuleSymbols(_imod, out var symTypeList, out var codeViewModuleAccessor);

            var enumerator = ((BlockSym) result.offSegSym.symType).GetChildren(codeViewModuleAccessor).GetEnumerator();

            while (enumerator.MoveNext())
            {
                var childSymType = enumerator.Current;

                if (!childSymType.IsBlockSym())
                    continue;

                if (!childSymType.TryGetOffSeg(out var off, out var seg) || !childSymType.TryGetLength(out var length))
                    continue;

                if (seg > _targetSeg || (seg == _targetSeg && off > _targetOff))
                    break;

                if (off <= _targetOff)
                {
                    uint disp = _targetSeg == seg
                        ? (uint) (_targetOff - off)
                        : uint.MaxValue;

                    if (disp < length)
                    {
                        //Caller will check if we have a name (which we don't)
                        result.offSegSym = new OffSegSym
                        {
                            off = off,
                            seg = seg,
                            symType = childSymType
                        };

                        //Now dig into this symbol
                        enumerator = ((BlockSym) result.offSegSym.symType).GetChildren(codeViewModuleAccessor).GetEnumerator();
                    }
                }
            }

            return true;
        }
    }
}
