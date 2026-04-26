using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfUnion_16t"/> structure.
    /// </summary>
    public readonly unsafe struct LfUnion16t : IViewable
    {
        private const int typlenOffset = 0;
        private const int leafOffset = 2;
        private const int countOffset = 4;
        private const int fieldOffset = 6;
        private const int propertyOffset = 8;
        private const int lengthOffset = 10;
        private int nameOffset
        {
            get
            {
                var numericData = TypType.ExtractNumericData(value->data);

                return lengthOffset + numericData.Length;
            }
        }

        private int uniquenameOffset
        {
            get
            {
                var numericData = TypType.ExtractNumericData(value->data);

                var str = TypType.ReadString(value->data + numericData.Length);

                return length + numericData.Length + str.Length + 1;
            }
        }


        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfUnion_16t* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public short count => value->count;

        public TypOrEnumType field => new TypOrEnumType((byte*) value, value->field);

        public CV_prop_t property => value->property;

        #region data

        //"data" describes the length of the structure in bytes, and name

        public int length
        {
            get
            {
                //Length may be 0, this is normal
                var numericData = TypType.ExtractNumericData(value->data);

                return numericData.Int32;
            }
        }

        public SymString name => GetName(null);

        public SymString uniquename => GetUniqueName(null);

        #endregion
        #region PESpy

        public SymString GetName(ICodeViewAccessor? codeViewAccessor)
        {
            var numericData = TypType.ExtractNumericData(value->data);

            //I am assuming I need to use normal ST/UTF parsing logic
            return TypType.ReadString(value->data + numericData.Length, codeViewAccessor);
        }

        internal SymString GetUniqueName(ICodeViewAccessor? codeViewAccessor)
        {
            if (property.hasuniquename)
            {
                var numericData = TypType.ExtractNumericData(value->data);

                //I am assuming I need to use normal ST/UTF parsing logic
                var name = TypType.ReadString(value->data + numericData.Length, codeViewAccessor);

                //I am assuming I need to use normal ST/UTF parsing logic
                return TypType.ReadString(value->data + numericData.Length + name.Length + 1, codeViewAccessor); //+1 because it's either null terminated or length prefixed
            }

            return default;
        }

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            sizeof(short)  + //count
            sizeof(short)  + //field
            2;               //property

        private int BytesUsed()
        {
            var numericData = TypType.ExtractNumericData(value->data);

            var str = TypType.ReadString(value->data + numericData.Length);

            var length = numericData.Length + str.Length + 1;

            if (property.hasuniquename)
            {
                var uniqueName = TypType.ReadString(value->data + numericData.Length + name.Length + 1);

                length += uniquename.Length + 1;
            }

            return FixedStructSize + length;
        }

        internal LfUnion16t(lfUnion_16t* value)
        {
            this.value = value;
        }

        public static implicit operator LfEasy(LfUnion16t easy) => new LfEasy((lfEasy*) (byte*) easy.value);

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(this, ViewKind.LfUnion16t, typlen + sizeof(short));

        int IViewable.NumChildren() => StructWriter.GetNumChildrenAlign4(7, BytesUsed()) + (property.hasuniquename ? 1 : 0);

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

                case 3:
                    structWriter.WriteField(nameof(field), fieldOffset, value->field);
                    break;

                case 4:
                    structWriter.WriteField(nameof(property), propertyOffset, property);
                    break;

                case 5:
                    structWriter.WriteField(nameof(length), lengthOffset, length);
                    break;

                case 6:
                    structWriter.WriteSymStringField(nameof(name), nameOffset, GetName(structWriter.GetSymbolAccessor()));
                    break;

                case 7:
                    if (property.hasuniquename)
                        structWriter.WriteSymStringField(nameof(uniquename), uniquenameOffset, GetUniqueName(structWriter.GetSymbolAccessor()));
                    else
                        structWriter.AlignOrThrow(BytesUsed());
                    break;

                case 8:
                    if (property.hasuniquename)
                    {
                        //Possible alignment
                        structWriter.AlignOrThrow(BytesUsed());
                    }
                    else
                        throw new IndexOutOfRangeException(); //We already aligned above

                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}
