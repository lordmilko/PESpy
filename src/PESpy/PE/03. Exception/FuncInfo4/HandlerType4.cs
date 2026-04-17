using System;
using System.Diagnostics;

namespace PESpy
{
    [Source(SourceKind.ehdata4_export_h)]
    public unsafe readonly struct HandlerType4
    {
        private const int MAX_CONT_ADDRESSES = 2;

        public HandlerTypeHeader header { get; }

        /// <summary>
        /// Handler Type adjectives (bitfield)
        /// </summary>
        public int adjectives { get; }

        /// <summary>
        /// Image relative offset of the corresponding type descriptor
        /// </summary>
        public int dispType { get; }

        /// <summary>
        /// Displacement of catch object from base
        /// </summary>
        public int dispCatchObj { get; }

        /// <summary>
        /// Image relative offset of 'catch' code
        /// </summary>
        public int dispOfHandler { get; }

        /// <summary>
        /// Continuation address(es) of catch funclet<para/>
        /// Note, unlike the implementation in ehdata4_export.h, this value does not contain
        /// the function start added to it, as we don't know what that is
        /// </summary>
        public Span<int> continuationAddresses
        {
            get
            {
                fixed (HandlerType4* me = &this)
                    return new Span<int>(&me->_continuationAddress1, _numContinuationAddresses);
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly int _continuationAddress1;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly int _continuationAddress2;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly int _numContinuationAddresses;

        internal unsafe HandlerType4(int offset, ref byte* pData, int functionAddress)
        {
            header = new HandlerTypeHeader(offset, *pData);
            pData++;

            if (header.adjectives)
                adjectives = (int) FuncInfo4.ReadUnsigned(ref pData);

            if (header.dispType)
                dispType = (int) FuncInfo4.ReadUnsigned(ref pData);

            if (header.dispCatchObj)
                dispCatchObj = (int) FuncInfo4.ReadUnsigned(ref pData);

            dispOfHandler = FuncInfo4.ReadInt(ref pData);

            if (header.contIsRVA)
            {
                switch (header.contAddr)
                {
                    case HandlerTypeHeader.contType.ONE:
                        _continuationAddress1 = FuncInfo4.ReadInt(ref pData);
                        _numContinuationAddresses = 1;
                        break;

                    case HandlerTypeHeader.contType.TWO:
                        _continuationAddress1 = FuncInfo4.ReadInt(ref pData);
                        _continuationAddress2 = FuncInfo4.ReadInt(ref pData);
                        _numContinuationAddresses = 2;
                        break;
                }
            }
            else
            {
                //We read unsigned here rather than int. You're supposed to add the RuntimeFunction.BeginAddress to this

                switch (header.contAddr)
                {
                    case HandlerTypeHeader.contType.ONE:
                        _continuationAddress1 = functionAddress + FuncInfo4.ReadInt(ref pData);
                        _numContinuationAddresses = 1;
                        break;

                    case HandlerTypeHeader.contType.TWO:
                        _continuationAddress1 = functionAddress + FuncInfo4.ReadInt(ref pData);
                        _continuationAddress2 = functionAddress + FuncInfo4.ReadInt(ref pData);
                        _numContinuationAddresses = 2;
                        break;
                }
            }
        }
    }
}
