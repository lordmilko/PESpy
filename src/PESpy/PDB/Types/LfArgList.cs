using System;
using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfArgList"/> structure.
    /// </summary>
    public readonly unsafe struct LfArgList
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfArgList* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public int count => value->count;

        public TypOrEnumType[] arg
        {
            get
            {
                var raw = new Span<CV_typ_t>(value->arg, count);
                var arr = new TypOrEnumType[raw.Length];

                for (var i = 0; i < arr.Length; i++)
                    arr[i] = new TypOrEnumType((byte*) value, raw[i]);

                return arr;
            }
        }

        internal LfArgList(lfArgList* value)
        {
            this.value = value;
            this.value = value;
        }
    }
}
