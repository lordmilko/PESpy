using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfOEM"/> structure.
    /// </summary>
    public readonly unsafe struct LfOEM : IViewable
    {
        private const int typlenOffset = 0;
        private const int leafOffset = 2;
        private const int cvOEMOffset = 4;
        private const int recOEMOffset = 6;
        private const int countOffset = 8;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfOEM* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public short cvOEM => value->cvOEM;

        public short recOEM => value->recOEM;

        public int count => value->count;

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            sizeof(short)  + //cvOEM
            sizeof(short)  + //recOEM
            sizeof(int);     //count

        internal LfOEM(lfOEM* value)
        {
            this.value = value;
            TypType.AssertMissing(false, "Read index");
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.lfOEM, this, ViewKind.LfOEM, typlen + sizeof(short));

        int IViewable.NumChildren => 5;

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
                    structWriter.WriteField(nameof(cvOEM), cvOEMOffset, cvOEM);
                    break;

                case 3:
                    structWriter.WriteField(nameof(recOEM), recOEMOffset, recOEM);
                    break;

                case 4:
                    structWriter.WriteField(nameof(count), countOffset, count);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
