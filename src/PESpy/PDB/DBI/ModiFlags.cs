using System.Diagnostics;

namespace PESpy.PDB
{
    [DebuggerDisplay("fWritten = {fWritten}")]
    public readonly struct ModiFlags
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
        public ushort unused => (ushort) (value >> 1);

        public ModiFlags(ushort value)
        {
            this.value = value;
        }

        public static implicit operator ModiFlags(ushort value) => new ModiFlags(value);
    }
}
