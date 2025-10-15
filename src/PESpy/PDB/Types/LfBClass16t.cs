using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfBClass_16t"/> structure.
    /// </summary>
    public readonly unsafe struct LfBClass16t : IViewable
    {
        private const int leafOffset = 0;
        private const int indexOffset = 2;
        private const int attrOffset = 4;
        private const int offsetOffset = 6;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfBClass_16t* value;

        //This type is only ever referenced from other records and so does not have a TYPTYPE.len

        public LEAF_ENUM_e leaf => value->leaf;

        public TypOrEnumType index => new TypOrEnumType((byte*) value, value->index);

        public CV_fldattr_t attr => value->attr;

        public ulong offset
        {
            get
            {
                TypType.ExtractNumericData(value->offset, out var offset, out _);

                return offset;
            }
        }

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            sizeof(short)  + //index
            2;               //attr

        internal int StructSize
        {
            get
            {
                TypType.ExtractNumericData(value->offset, out _, out var bytesRead);

                return FixedStructSize + bytesRead;
            }
        }

        internal LfBClass16t(lfBClass_16t* value)
        {
            this.value = value;
        }


        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.lfBClass_16t, this, ViewKind.LfBClass16t, StructSize);

        int IViewable.NumChildren => 4;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(leaf), leafOffset, leaf, sizeof(ushort));
                    break;

                case 1:
                    structWriter.WriteField(nameof(index), indexOffset, index);
                    break;

                case 2:
                    structWriter.WriteField(nameof(attr), attrOffset, attr);
                    break;

                case 3:
                    structWriter.WriteNumericData(nameof(offset), offsetOffset, value->offset);
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
