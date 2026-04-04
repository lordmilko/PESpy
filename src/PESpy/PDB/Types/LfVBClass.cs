using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfVBClass"/> structure.
    /// </summary>
    public readonly unsafe struct LfVBClass : IViewable
    {
        private const int leafOffset = 0;
        private const int attrOffset = 2;
        private const int indexOffset = 4;
        private const int vbptrOffset = 8;
        private const int vbpoffOffset = 12;
        private int offsetOffset
        {
            get
            {
                var numericData = TypType.ExtractNumericData(value->vbpoff);

                return vbpoffOffset + numericData.Length;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfVBClass* value;

        //This type is only ever referenced from other records and so does not have a TYPTYPE.len

        public LEAF_ENUM_e leaf => value->leaf;

        public CV_fldattr_t attr => value->attr;

        public TypOrEnumType index => new TypOrEnumType((byte*) value, value->index);

        public TypOrEnumType vbptr => new TypOrEnumType((byte*) value, value->vbptr);

        //attr comes before index in the 32-bit one, after it in the 16-bit one

        /// <summary>
        /// virtual base pointer offset from address point
        /// </summary>
        public ulong vbpoff
        {
            get
            {
                //offVbp in pdbdump, vbpoff in NT 4

                var numericData = TypType.ExtractNumericData(value->vbpoff);

                return numericData.UInt64;
            }
        }

        /// <summary>
        /// virtual base offset from vbtable
        /// </summary>
        public ulong offset
        {
            get
            {
                //offVbte in pdbdump, offset in NT 4

                //Skip over vbpoff
                var numericData1 = TypType.ExtractNumericData(value->vbpoff);
                var numericData2 = TypType.ExtractNumericData(value->vbpoff + numericData1.Length);

                return numericData2.UInt64;
            }
        }

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            2              + //attr
            sizeof(int)    + //index
            sizeof(int);     //vbptr

        internal int StructSize
        {
            get
            {
                var numericData1 = TypType.ExtractNumericData(value->vbpoff);
                var numericData2 = TypType.ExtractNumericData(value->vbpoff + numericData1.Length);

                return FixedStructSize + numericData1.Length + numericData2.Length;
            }
        }

        internal LfVBClass(lfVBClass* value)
        {
            this.value = value;
        }

        public static implicit operator LfEasy(LfVBClass easy) => new LfEasy((lfEasy*) (byte*) easy.value);

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.lfVBClass, this, ViewKind.LfVBClass, StructSize);

        int IViewable.NumChildren() => 6;

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
                    structWriter.WriteField(nameof(vbptr), vbptrOffset, value->vbptr);
                    break;

                case 4:
                    structWriter.WriteField(nameof(vbpoff), vbpoffOffset, vbpoff);
                    break;

                case 5:
                    structWriter.WriteField(nameof(offset), offsetOffset, offset);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
