using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfPointer_16t"/> structure.
    /// </summary>
    public readonly unsafe struct LfPointer16t : IViewable
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfPointer_16t* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->u.leaf;

        public TypOrEnumType utype => new TypOrEnumType((byte*) value, value->u.utype);

        public lfPointer_16t.lfPointerAttr_16t attr => value->u.attr;

        public lfPointer_16t.BaseInfo pbase => value->pbase;

        internal LfPointer16t(lfPointer_16t* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }
    }
}
