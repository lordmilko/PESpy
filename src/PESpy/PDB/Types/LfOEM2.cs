using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfOEM2"/> structure.
    /// </summary>
    public readonly unsafe struct LfOEM2 : IViewable
    {
        private const int typlenOffset = 0;
        private const int leafOffset = 2;
        private const int idOemOffset = 4;
        private const int countOffset = 20;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfOEM2* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public Guid idOem => value->idOem;

        public int count => value->count;

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            16             + //idOem
            sizeof(int);     //count

        internal LfOEM2(lfOEM2* value)
        {
            this.value = value;
            TypType.AssertMissing(false, "Read index");
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.lfOEM2, this, ViewKind.LfOEM2, typlen + sizeof(short));

        int IViewable.NumChildren => 4;

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
                    structWriter.WriteField(nameof(idOem), idOemOffset, idOem);
                    break;

                case 3:
                    structWriter.WriteField(nameof(count), countOffset, count);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
