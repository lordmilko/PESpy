using System;
using System.Diagnostics;
using System.Linq;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfFieldList_16t"/> structure.
    /// </summary>
    public readonly unsafe struct LfFieldList16t : IViewable
    {
        private const int typlenOffset = 0;
        private const int leafOffset = 2;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfFieldList_16t* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public LfEasy[] fields => LfFieldList.EnumerateFields(typlen - sizeof(ushort), (IntPtr) value->data, null).ToArray();

        internal const int FixedStructSize =
            sizeof(ushort);  //leaf

        internal LfFieldList16t(lfFieldList_16t* value)
        {
            this.value = value;
        }

        public static implicit operator LfEasy(LfFieldList16t easy) => new LfEasy((lfEasy*) (byte*) easy.value);

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.lfFieldList_16t, this, ViewKind.LfFieldList16t, typlen + sizeof(short));

        int IViewable.NumChildren() => 1;

        void IViewable.WriteChild(int index, ref StructWriter structWriter) => LfFieldList.WriteChild(typlen, leaf, value->data, index, ref structWriter);
    }
}
