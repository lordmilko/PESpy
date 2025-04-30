using System.Diagnostics;

namespace PESpy.PDB
{
    [DebuggerDisplay("fWritten = {fWritten}, fECEnabled = {fECEnabled}, iTSM = {iTSM}")]
    public readonly struct Modi60Flags
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly ushort value;

        /// <summary>
        /// TRUE if mod has been written since DBI opened
        /// </summary>
        public bool fWritten => (value & 0x1) != 0;

        /// <summary>
        /// TRUE if mod has EC symbolic information
        /// </summary>
        public bool fECEnabled => (value & 0x2) != 0;

        /// <summary>
        /// spare
        /// </summary>
        public byte unused => (byte) ((value >> 1) & 0x3F);

        /// <summary>
        /// index into TSM list for this mods server
        /// </summary>
        public byte iTSM => (byte) ((value >> 8) & 0xFF);

        public Modi60Flags(ushort value)
        {
            this.value = value;
        }

        public static implicit operator Modi60Flags(ushort value) => new Modi60Flags(value);
    }
}
