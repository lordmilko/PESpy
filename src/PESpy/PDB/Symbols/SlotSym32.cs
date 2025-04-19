using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="SLOTSYM32"/> structure.
    /// </summary>
    public readonly unsafe struct SlotSym32
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly SLOTSYM32* value;

        public ushort reclen => value->reclen;

        public SYM_ENUM_e rectyp => value->rectyp;

        public int iSlot => value->iSlot;

        public CV_typ_t typind => value->typind;

        public FixedUtf8String name => SymType.ReadString(value, value->name);

        internal SlotSym32(SLOTSYM32* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}

