using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfVarString"/> structure.
    /// </summary>
    public readonly unsafe struct LfVarString : IViewable
    {
        private const int typlenOffset = 0;
        private const int leafOffset = 2;
        private const int lenOffset = 4;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfVarString* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public short len => value->len;

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            sizeof(short);   //len

        internal LfVarString(lfVarString* value)
        {
            this.value = value;
            TypType.AssertMissing(false, "Read value");
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.lfVarString, this, ViewKind.LfVarString, typlen + sizeof(short));

        int IViewable.NumChildren() => 3;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(typlen), typlenOffset, typlen);
                    break;

                case 1:
                    structWriter.WriteField(nameof(leaf), leafOffset, leaf, sizeof(ushort));
                    break;

                case 2:
                    structWriter.WriteField(nameof(len), lenOffset, len);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
