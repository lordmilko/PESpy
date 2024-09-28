namespace PESpy.Native
{
    internal struct FuncInfo4
    {
        public FuncInfoHeader header;
        public uint bbtFlags;            // flags that may be set by BBT processing

        public int dispUnwindMap;       // Image relative offset of the unwind map
        public int dispTryBlockMap;     // Image relative offset of the handler map
        public int dispIPtoStateMap;    // Image relative offset of the IP to state map
        public int dispFrame;           // displacement of address of function frame wrt establisher frame, only used for catch funclets
    }
}
