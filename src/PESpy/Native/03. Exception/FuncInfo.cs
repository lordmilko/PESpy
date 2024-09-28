namespace PESpy.Native
{
    internal struct FuncInfo
    {
        //public int magicNumber:29;        // Identifies version of compiler
        //public int bbtFlags:3;            // flags that may be set by BBT processing
        public int magicNumberAndBBTFlags;

        public int maxState;           // Highest state number plus one (thus number of entries in unwind map)
        public int dispUnwindMap;       // Image relative offset of the unwind map
        public int nTryBlocks;          // Number of 'try' blocks in this function
        public int dispTryBlockMap; // Image relative offset of the handler map
        public int nIPMapEntries;       // # entries in the IP-to-state map. NYI (reserved)
        public int dispIPtoStateMap;    // Image relative offset of the IP to state map
        public int dispUwindHelp;       // Displacement of unwind helpers from base
        public int dispESTypeList;      // Image relative list of types for exception specifications

        public int EHFlags;			// Flags for some features.
    }
}
