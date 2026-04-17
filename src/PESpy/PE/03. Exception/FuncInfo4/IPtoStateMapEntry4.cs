namespace PESpy
{
    [Source(SourceKind.ehdata4_export_h)]
    public readonly struct IPtoStateMapEntry4
    {
        /// <summary>
        /// Image relative offset of IP<para/>
        /// compressed int, function-relative AND delta-encoded from the previous entry.
        /// For example, entries of 0,5,15 map to 0,5,20 bytes from the start of the function<para/>
        /// Thus, the final value is equal to <see cref="RuntimeFunction.BeginAddress"/> + the sum of this
        /// <see cref="Ip"/> and all previous <see cref="Ip"/> values in the <see cref="IPtoStateMap4"/>.
        /// </summary>
        public int Ip { get; } //todo: we need to add xrefs from these to the functions they reference!

        public int RawIp { get; }

        //States are encoded +1 so as to not encode a negative. This gets
        //the value with -1 applied to it
        public int State { get; }

        internal unsafe IPtoStateMapEntry4(ref byte* pData, int functionAddress, int prevAmount)
        {
            RawIp = (int) FuncInfo4.ReadUnsigned(ref pData);
            Ip = functionAddress + prevAmount + RawIp;

            // States are encoded +1 so as to not encode a negative
            State = (int) (FuncInfo4.ReadUnsigned(ref pData) - 1);
        }
    }
}
