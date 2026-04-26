using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfBClass"/> structure.
    /// </summary>
    public readonly unsafe struct LfBClass : IViewable
    {
        private const int leafOffset = 0;
        private const int attrOffset = 2;
        private const int indexOffset = 4;
        private const int offsetOffset = 8;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfBClass* value;

        //This type is only ever referenced from other records and so does not have a TYPTYPE.len

        public LEAF_ENUM_e leaf => value->leaf;

        public CV_fldattr_t attr => value->attr;

        public TypOrEnumType index => new TypOrEnumType((byte*) value, value->index);

        public ulong offset
        {
            get
            {
                var numericData = TypType.ExtractNumericData(value->offset);

                return numericData.UInt64;
            }
        }

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            2              + //attr
            sizeof(int);     //index

        internal int StructSize
        {
            get
            {
                var numericData = TypType.ExtractNumericData(value->offset);

                return FixedStructSize + numericData.Length;
            }
        }

        internal LfBClass(lfBClass* value)
        {
            this.value = value;
            TypType.AssertMissing(false, "Read offset");
        }

        public static implicit operator LfEasy(LfBClass easy) => new LfEasy((lfEasy*) (byte*) easy.value);

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(this, ViewKind.LfBClass, StructSize);

        int IViewable.NumChildren() => 4;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(leaf), leafOffset, leaf, sizeof(ushort));
                    break;

                case 1:
                    structWriter.WriteField(nameof(attr), attrOffset, attr);
                    break;

                case 2:
                    structWriter.WriteField(nameof(index), indexOffset, value->index);
                    break;

                case 3:
                    structWriter.WriteStructField(nameof(offset), offsetOffset, TypType.ExtractNumericData(value->offset));
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }

        public override string ToString()
        {
            return index.ToString();
        }
    }
}
