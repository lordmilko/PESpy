using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfVTShape"/> structure.
    /// </summary>
    public readonly unsafe struct LfVTShape : IViewable
    {
        private const int typlenOffset = 0;
        private const int leafOffset = 2;
        private const int countOffset = 4;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfVTShape* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public short count => value->count;

        public CV_VTS_desc_e[] desc
        {
            get
            {
                var results = new CV_VTS_desc_e[count];

                var pDescs = ((byte*) value) + 4; //The pointer starts from after the length

                //e.g. you might have 55 55 50
                for (var i = 0; i < count; i++)
                {
                    var nibble = (i & 1) == 0
                        ? (*pDescs >> 4) & 0xF
                        : (*pDescs & 0xF);

                    results[i] = (CV_VTS_desc_e) nibble;

                    if ((i & 1) != 0)            // after using high nibble
                        pDescs++;
                }

                return results;
            }
        }

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            sizeof(short);   //count

        internal LfVTShape(lfVTShape* value)
        {
            this.value = value;
            TypType.AssertMissing(false, "Read desc");
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.lfVTShape, this, ViewKind.LfVTShape, typlen + sizeof(short));

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
                    structWriter.WriteField(nameof(count), countOffset, count);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
