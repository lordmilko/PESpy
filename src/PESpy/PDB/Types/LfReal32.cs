using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfReal32"/> structure.
    /// </summary>
    public readonly unsafe struct LfReal32 : IViewable
    {
        private const int typlenOffset = 0;
        private const int leafOffset = 2;
        private const int valOffset = 4;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfReal32* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public float val => value->val;

        internal const int StructSize =
            sizeof(ushort) + //leaf
            sizeof(float);   //val

        internal LfReal32(lfReal32* value)
        {
            this.value = value;
        }

        public static implicit operator LfEasy(LfReal32 easy) => new LfEasy((lfEasy*) (byte*) easy.value);

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(this, ViewKind.LfReal32, typlen + sizeof(short));

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
                    structWriter.WriteField(nameof(val), valOffset, val);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
