using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfVBClass_16t"/> structure.
    /// </summary>
    public readonly unsafe struct LfVBClass16t : IViewable
    {
        private const int leafOffset = 0;
        private const int indexOffset = 2;
        private const int vbptrOffset = 4;
        private const int attrOffset = 6;
        private const int vbpoffOffset = 8;
        private int offsetOffset
        {
            get
            {
                TypType.ExtractNumericData(value->vbpoff, out _, out var bytesRead);

                return vbpoffOffset + bytesRead;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfVBClass_16t* value;

        //This type is only ever referenced from other records and so does not have a TYPTYPE.len

        public LEAF_ENUM_e leaf => value->leaf;

        public TypOrEnumType index => new TypOrEnumType((byte*) value, value->index);

        public TypOrEnumType vbptr => new TypOrEnumType((byte*) value, value->vbptr);

        public CV_fldattr_t attr => value->attr;

        /// <summary>
        /// virtual base pointer offset from address point
        /// </summary>
        public ulong vbpoff
        {
            get
            {
                //offVbp in pdbdump, vbpoff in NT 4

                TypType.ExtractNumericData(value->vbpoff, out var vbpoff, out _);

                return vbpoff;
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
                TypType.ExtractNumericData(value->vbpoff, out _, out var read);

                TypType.ExtractNumericData(value->vbpoff + read, out var offset, out _);

                return offset;
            }
        }

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            sizeof(short)  + //index
            sizeof(short)  + //vbptr
            2;               //attr

        internal int StructSize
        {
            get
            {
                TypType.ExtractNumericData(value->vbpoff, out _, out var bytesRead1);
                TypType.ExtractNumericData(value->vbpoff + bytesRead1, out _, out var bytesRead2);

                return FixedStructSize + bytesRead1 + bytesRead2;
            }
        }

        internal LfVBClass16t(lfVBClass_16t* value)
        {
            this.value = value;
            TypType.AssertMissing(false, "Read vbpoff");
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.lfVBClass_16t, this, ViewKind.LfVBClass16t, StructSize);

        int IViewable.NumChildren() => 6;

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
                    structWriter.WriteField(nameof(vbptr), vbptrOffset, value->vbptr);
                    break;

                case 3:
                    structWriter.WriteField(nameof(attr), attrOffset, attr);
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
