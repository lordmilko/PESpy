using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy
{
    /// <summary>
    /// Represents the _GS_HANDLER_DATA structure.
    /// </summary>
    [DebuggerDisplay("EHandler = {EHandler}, UHandler = {UHandler}, HasAlignment = {HasAlignment}, CookieOffset = {CookieOffset}, AlignedBaseOffset = {AlignedBaseOffset}, Alignment = {Alignment}")]
    public readonly unsafe struct GsHandlerData : IViewableValue
    {
        private const int AlignedBaseOffsetOffset = 4;
        private const int AlignmentOffset = 4;

        private readonly byte* value;

        /* typedef struct _GS_HANDLER_DATA {
         *     union {
         *         struct {
         *             ULONG EHandler : 1;
         *             ULONG UHandler : 1;
         *             ULONG HasAlignment : 1;
         *         } Bits;
         *         LONG CookieOffset;
         *     } u;
         *     LONG AlignedBaseOffset;
         *     LONG Alignment;
         * };
         * 
         * I believe that this type is in fact variable length based on whether
         * or not HasAlignment is true!
         */

        public int Offset { get; }

        public GsHandlerData(int offset, byte* value)
        {
            Offset = offset;
            this.value = value;
        }

        public bool EHandler => ((*(uint*) value) & 0x1) != 0;
        public bool UHandler => ((*(uint*) value) & 0x2) != 0;
        public bool HasAlignment => ((*(uint*) value) & 0x4) != 0;

        /// <summary>
        /// Gets the cookie offset with the bits pertaining to <see cref="EHandler"/>, <see cref="UHandler"/>
        /// and <see cref="HasAlignment"/> cleared. The function that this data pertains to will move __security_cookie
        /// into rsp+CookieOffset, which will later be retrieved and validated in __security_check_cookie
        /// </summary>
        public uint CookieOffset => (*(uint*) value) & 0xFFFFFFF8;

        /// <summary>
        /// Gets the raw unmodified _GS_HANDLER_DATA.CookieOffset without having cleared the bits that overlap <see cref="EHandler"/>,
        /// <see cref="UHandler"/> and <see cref="HasAlignment"/>.
        /// </summary>
        public uint RawCookieOffset => (*(uint*) value);

        public uint? AlignedBaseOffset => HasAlignment ? *((uint*) value + 1) : null;

        public uint? Alignment => HasAlignment ? *((uint*) value + 2) : null;

        internal int StructSize => HasAlignment ? 12 : 4;

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings._GS_HANDLER_DATA, this, ViewKind.GsHandlerData, StructSize);

        int IViewable.NumChildren() => HasAlignment ? 6 : 4;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteBitField(nameof(EHandler), 0, EHandler, sizeof(int), 1);
                    break;

                case 1:
                    structWriter.WriteBitField(nameof(UHandler), 0, UHandler, sizeof(int), 1);
                    break;

                case 2:
                    structWriter.WriteBitField(nameof(HasAlignment), 0, HasAlignment, sizeof(int), 1);
                    break;

                case 3:
                    //The definition of _GS_HANDLER_DATA is arguably incorrect, since the CookieOffset
                    //should own the remaining 29 bits. We don't currently support displaying unions, so
                    //we'll just show this as a bitfield and show the "real" CookieOffset portion
                    structWriter.WriteBitField(nameof(CookieOffset), 0, CookieOffset, sizeof(int), 29);
                    break;

                case 4:
                    if (HasAlignment)
                        structWriter.WriteField(nameof(AlignedBaseOffset), AlignedBaseOffsetOffset, AlignedBaseOffset.Value);
                    else
                        throw new IndexOutOfRangeException();
                    break;

                case 5:
                    if (HasAlignment)
                        structWriter.WriteField(nameof(Alignment), AlignmentOffset, Alignment.Value);
                    else
                        throw new IndexOutOfRangeException();
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
