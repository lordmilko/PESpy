using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="ATTRSLOTSYM"/> structure.
    /// </summary>
    public readonly unsafe struct AttrSlotSym
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly ATTRSLOTSYM* value;

        public ushort reclen => value->reclen;

        public SYM_ENUM_e rectyp => value->rectyp;

        public int iSlot => value->iSlot;

        public CV_typ_t typind => value->typind;

        public CV_lvar_attr attr => value->attr;

        public FixedUtf8String name => SymType.ReadString(value, value->name);

        internal AttrSlotSym(ATTRSLOTSYM* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}

