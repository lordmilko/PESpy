using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfReal48"/> structure.
    /// </summary>
    public readonly unsafe struct LfReal48 : IViewable
    {
        private const int typlenOffset = 0;
        private const int leafOffset = 2;
        private const int valOffset = 4;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfReal48* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public NativeSpan<byte> val => new NativeSpan<byte>(value->val, 6);

        internal const int FixedStructSize =
            sizeof(ushort) +  //leaf
            6; //val

        internal LfReal48(lfReal48* value)
        {
            this.value = value;
        }

        public static implicit operator LfEasy(LfReal48 easy) => new LfEasy((lfEasy*) (byte*) easy.value);

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(this, ViewKind.LfReal48, typlen + sizeof(short));

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
