using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfMember_16t"/> structure.
    /// </summary>
    public readonly unsafe struct LfMember16t : IViewable
    {
        private const int leafOffset = 0;
        private const int indexOffset = 2;
        private const int attrOffset = 4;
        private const int offsetOffset = 6;
        private int nameOffset
        {
            get
            {
                var numericData = TypType.ExtractNumericData(value->offset);

                return offsetOffset + numericData.Length;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfMember_16t* value;

        //This type is only ever referenced from other records and so does not have a TYPTYPE.len

        public LEAF_ENUM_e leaf => value->leaf;

        public TypOrEnumType index => new TypOrEnumType((byte*) value, value->index);

        public CV_fldattr_t attr => value->attr;

        #region offset

        //variable length offset of field followed by length prefixed name of field

        public int offset
        {
            get
            {
                var numericData = TypType.ExtractNumericData(value->offset);

                return numericData.Int32;
            }
        }

        public SymString name => GetName(null);

        #endregion
        #region PESpy

        public SymString GetName(ICodeViewAccessor? codeViewAccessor)
        {
            //I am assuming I need to use normal ST/UTF parsing logic
            var numericData = TypType.ExtractNumericData(value->offset);

            return TypType.ReadString(value->offset + numericData.Length, codeViewAccessor);
        }

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            sizeof(short)  + //index
            2;               //attr

        internal int StructSize => GetStructSize(null);

        internal int GetStructSize(ICodeViewAccessor? codeViewAccessor)
        {
            var numericData = TypType.ExtractNumericData(value->offset);

            var str = TypType.ReadString(value->offset + numericData.Length, codeViewAccessor);

            return FixedStructSize + numericData.Length + str.Length + 1;
        }

        internal LfMember16t(lfMember_16t* value)
        {
            this.value = value;
        }

        public static implicit operator LfEasy(LfMember16t easy) => new LfEasy((lfEasy*) (byte*) easy.value);

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.lfMember_16t, this, ViewKind.LfMember16t, GetStructSize(writer.GetSymbolAccessor()));

        int IViewable.NumChildren() => 3;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(leaf), leafOffset, leaf, sizeof(ushort));
                    break;

                case 1:
                    structWriter.WriteField(nameof(index), indexOffset, value->index);
                    break;

                case 2:
                    structWriter.WriteField(nameof(attr), attrOffset, attr);
                    break;

                case 3:
                    structWriter.WriteStructField(nameof(offset), offsetOffset, TypType.ExtractNumericData(value->offset));
                    break;

                case 4:
                    structWriter.WriteSymStringField(nameof(name), nameOffset, GetName(structWriter.GetSymbolAccessor()));
                    break;

                //Do not align; the parent will apply padding

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
