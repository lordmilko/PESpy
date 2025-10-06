using System.Diagnostics;

namespace PESpy.PDB
{
    [DebuggerDisplay("fWritten = {fWritten}, iTSM = {iTSM}")]
    public readonly struct Modi50Flags
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly ushort value;

        /// <summary>
        /// TRUE if mod has been written since DBI opened
        /// </summary>
        public bool fWritten => (value & 0x1) != 0;

        /// <summary>
        /// spare
        /// </summary>
        public byte unused => (byte) ((value >> 1) & 0x7F);

        /// <summary>
        /// index into TSM list for this mods server
        /// </summary>
        public byte iTSM => (byte) ((value >> 8) & 0xFF);

        public Modi50Flags(ushort value)
        {
            this.value = value;
        }

        public static implicit operator Modi50Flags(ushort value) => new Modi50Flags(value);
        public static implicit operator ushort(Modi50Flags value) => value.value;
    }
}
