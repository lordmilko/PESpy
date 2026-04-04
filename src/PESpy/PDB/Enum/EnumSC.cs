namespace PESpy.PDB
{
    //Based on EnumSC<T> in dbicommon.h

    /// <summary>
    /// Provides facilities for iterating forwards and backwards through section contribs,
    /// while allowing for the "closest" section contrib to be selected in the event an address
    /// is specified that does not lie within the bounds of a specific section contrib.
    /// </summary>
    public struct EnumSC
    {
        private ISectionContribs _sectionContribs;
        private int _index;

        internal EnumSC(ISectionContribs sectionContribs)
        {
            _sectionContribs = sectionContribs;
            _index = -1;
        }

        public bool Locate(ISECT seg, int off)
        {
            if (_sectionContribs.TryGetSection(seg, off, out _index, out var sc))
            {
                _index--; //Next will increment it
                return true;
            }

            //Need to fallback to either the bottom or the top
            var lo = _sectionContribs[0];

            if (SC40.IsAddrInSC(lo, seg, off) < 0)
            {
                _index = -1;
                return false;
            }

            var hi = _sectionContribs[_sectionContribs.Length - 1];

            if (SC40.IsAddrInSC(hi, seg, off) > 0)
            {
                _index = _sectionContribs.Length - 1;
                return false;
            }

            //sc is not cleared on failure; rather it contains the last sc we got up to
            //so we can then use it here
            var comparison = SC40.IsAddrInSC(sc, seg, off);

            //The pointer arithmetic that PDB1 does has it doing +0 here but with our index based logic we need to do -1
            _index += (comparison > 0 ? -1 : -2);
            return false;
        }

        public SC40 Current => _sectionContribs[_index];

        public bool Next()
        {
            if (++_index < _sectionContribs.Length)
                return true;

            return false;
        }

        public bool Previous()
        {
            if (_index > 0)
            {
                _index--;
                return true;
            }

            return false;
        }
    }
}
