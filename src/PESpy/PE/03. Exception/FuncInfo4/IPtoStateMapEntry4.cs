using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy
{
    [Source(SourceKind.ehdata4_export_h)]
    public readonly struct IPtoStateMapEntry4 : IViewableValue
    {
        /// <summary>
        /// Image relative offset of IP<para/>
        /// compressed int, function-relative AND delta-encoded from the previous entry.
        /// For example, entries of 0,5,15 map to 0,5,20 bytes from the start of the function<para/>
        /// Thus, the final value is equal to <see cref="RuntimeFunction.BeginAddress"/> + the sum of this
        /// <see cref="Ip"/> and all previous <see cref="Ip"/> values in the <see cref="IPtoStateMap4"/>.
        /// </summary>
        public int Ip { get; }

        public int RawIp { get; }

        //States are encoded +1 so as to not encode a negative. This gets
        //the value with -1 applied to it
        public int State { get; }

        public long Offset { get; }

        internal int StructSize =>
            FuncInfo4.GetLength((uint) RawIp) +
            FuncInfo4.GetLength((uint) (State + 1));

        private readonly int functionAddress;

        internal unsafe IPtoStateMapEntry4(long offset, ref byte* pData, int functionAddress, int prevAmount)
        {
            Offset = offset;
            this.functionAddress = functionAddress;
            RawIp = (int) FuncInfo4.ReadUnsigned(ref pData);
            Ip = functionAddress + prevAmount + RawIp;

            // States are encoded +1 so as to not encode a negative
            State = unchecked((int) (FuncInfo4.ReadUnsigned(ref pData) - 1));
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //Asserting on Ip != RawIp doesn't work because you might have some prevAmount in there too
            Debug.Assert(functionAddress != 0, "Cannot write xrefs when the functionAddress was not specified");

            if (functionAddress != 0)
                writer.WriteUniqueRVAXRef(Offset, 0, Ip);

            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.IPtoStateMapEntry4, StructSize);

        int IViewable.NumChildren() => 2;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(Ip), 0, RawIp, FuncInfo4.GetLength((uint) RawIp));
                    break;

                case 1:
                    var adjustedState = State + 1;
                    structWriter.WriteField(nameof(State), FuncInfo4.GetLength((uint) RawIp), adjustedState, FuncInfo4.GetLength((uint) adjustedState));
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
