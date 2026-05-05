using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace PESpy
{
    //Name is made up

    [DebuggerTypeProxy(typeof(OldSymTypeProxy))]
    [DebuggerDisplay("{OldSymTypeProxy.DebuggerDisplay(this),nq}")]
    public readonly unsafe struct OldSymType
    {
        private const byte MaskIs32Bit = 0x80;

        private readonly byte* value;

        public byte reclen => *value;

        public OLDSYM rectyp => GetRecTyp(value);

        internal static OLDSYM GetRecTyp(byte* value) => (OLDSYM) (*(value + 1) & ~MaskIs32Bit);

        public bool Is32Bit => GetIs32Bit(value);

        //Offsets are either 16-bit or 32-bit based on whether or not the type has the high bit set
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool GetIs32Bit(byte* value) => (*(value + 1) & MaskIs32Bit) != 0;

        internal static int GetOffset(byte* value, int offset)
        {
            if (GetIs32Bit(value))
                return *(int*) (value + offset);

            return *(short*) (value + offset);
        }

        //Get a byte located after an offset, considering whether or not
        //the symbol contains a 32-bit offset or not
        internal static byte GetPostOffsetByte(byte* value, int offset16)
        {
            if (GetIs32Bit(value))
                return *(value + offset16 + sizeof(short));

            return *(value + offset16);
        }

        internal static short GetPostOffsetInt16(byte* value, int offset16)
        {
            if (GetIs32Bit(value))
                return *(short*) (value + offset16 + sizeof(short));

            return *(short*) (value + offset16);
        }

        internal static int GetPostOffsetInt32(byte* value, int offset16)
        {
            if (GetIs32Bit(value))
                return *(int*) (value + offset16 + sizeof(short));

            return *(int*) (value + offset16);
        }

        internal static SymString GetPostOffsetString(byte* value, int offset16, int reclen)
        {
            var effectiveOffset = GetIs32Bit(value)
                ? offset16 + sizeof(short)
                : offset16;

            if (effectiveOffset >= reclen + 1) //reclen doesn't include the length of reclen itself
                return default;

            //BlkSymType has a name field, but it may not be present; you can detect
            //this by looking at the reclen

            return new SymString(value + effectiveOffset + 1, true);
        }
        public NativeSpan<byte> Data => new NativeSpan<byte>(value + 2, reclen - 1);

        internal OldSymType(byte* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            if (value == default)
                return "<null>";

            return StringOldSymTypeDispatcher.Instance.Dispatch(this);
        }

        public static bool operator ==(OldSymType left, OldSymType right) => left.value == right.value;
        public static bool operator !=(OldSymType left, OldSymType right) => left.value != right.value;

        public static implicit operator BlkSymType(OldSymType oldSymType) => new BlkSymType(oldSymType.value);
        public static implicit operator BPSymType(OldSymType oldSymType) => new BPSymType(oldSymType.value);
        public static implicit operator ConSymType(OldSymType oldSymType) => new ConSymType(oldSymType.value);
        public static implicit operator CV4BlkSymType(OldSymType oldSymType) => new CV4BlkSymType(oldSymType.value);
        public static implicit operator CV4LabSymType(OldSymType oldSymType) => new CV4LabSymType(oldSymType.value);
        public static implicit operator CV4WithSymType(OldSymType oldSymType) => new CV4WithSymType(oldSymType.value);
        public static implicit operator LabSymType(OldSymType oldSymType) => new LabSymType(oldSymType.value);
        public static implicit operator LocSymType(OldSymType oldSymType) => new LocSymType(oldSymType.value);
        public static implicit operator ProcSymType(OldSymType oldSymType) => new ProcSymType(oldSymType.value);
        public static implicit operator RegSymType(OldSymType oldSymType) => new RegSymType(oldSymType.value);
        public static implicit operator ThunkSymType(OldSymType oldSymType) => new ThunkSymType(oldSymType.value);
        public static implicit operator TypeDefSymType(OldSymType oldSymType) => new TypeDefSymType(oldSymType.value);
        public static implicit operator WithSymType(OldSymType oldSymType) => new WithSymType(oldSymType.value);
    }
}
