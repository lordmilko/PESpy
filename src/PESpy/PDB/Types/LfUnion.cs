using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfUnion"/> structure.
    /// </summary>
    public readonly unsafe struct LfUnion : IViewable
    {
        private const int typlenOffset = 0;
        private const int leafOffset = 2;
        private const int countOffset = 4;
        private const int propertyOffset = 6;
        private const int fieldOffset = 8;
        private const int lengthOffset = 12;
        private int nameOffset
        {
            get
            {
                TypType.ExtractNumericData(value->data, out _, out var bytesRead);

                return lengthOffset + bytesRead;
            }
        }

        private int uniquenameOffset
        {
            get
            {
                TypType.ExtractNumericData(value->data, out _, out var bytesRead);

                var str = TypType.ReadString(value->data + bytesRead);

                return length + bytesRead + str.Length + 1;
            }
        }


        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfUnion* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public short count => value->count;

        public CV_prop_t property => value->property;

        public TypOrEnumType field => new TypOrEnumType((byte*) value, value->field);

        #region data

        //"data" describes the length of the structure in bytes, and name

        public int length
        {
            get
            {
                //Length may be 0, this is normal
                TypType.ExtractNumericData(value->data, out var length, out var bytesRead);

                return (int) length;
            }
        }

        public SymString name => GetName(null);

        public SymString uniquename => GetUniqueName(null);

        #endregion
        #region PESpy

        internal SymString GetName(ISymbolAccessor? symbolAccessor)
        {
            TypType.ExtractNumericData(value->data, out _, out var bytesRead);

            //I am assuming I need to use normal ST/UTF parsing logic
            return TypType.ReadString(value->data + bytesRead, symbolAccessor);
        }

        internal SymString GetUniqueName(ISymbolAccessor? symbolAccessor)
        {
            if (property.hasuniquename)
            {
                TypType.ExtractNumericData(value->data, out _, out var bytesRead);

                //I am assuming I need to use normal ST/UTF parsing logic
                var name = TypType.ReadString(value->data + bytesRead, symbolAccessor);

                //I am assuming I need to use normal ST/UTF parsing logic
                return TypType.ReadString(value->data + bytesRead + name.Length + 1, symbolAccessor); //+1 because it's either null terminated or length prefixed
            }

            return default;
        }

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            sizeof(short)  + //count
            2              + //property
            sizeof(int);     //field

        private int BytesUsed
        {
            get
            {
                TypType.ExtractNumericData(value->data, out _, out var bytesRead);

                var str = TypType.ReadString(value->data + bytesRead);

                var length = bytesRead + str.Length + 1;

                if (property.hasuniquename)
                {
                    var uniqueName = TypType.ReadString(value->data + bytesRead + name.Length + 1);

                    length += uniquename.Length + 1;
                }

                return FixedStructSize + length;
            }
        }

        internal LfUnion(lfUnion* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.lfUnion, this, ViewKind.LfUnion, typlen + sizeof(short));

        int IViewable.NumChildren => StructWriter.GetNumChildrenAlign4(7, BytesUsed) + (property.hasuniquename ? 1 : 0);

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
                    structWriter.WriteField(nameof(property), propertyOffset, property);
                    break;

                case 4:
                    structWriter.WriteField(nameof(field), fieldOffset, value->field);
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
                        structWriter.AlignOrThrow(BytesUsed);
                    break;

                case 8:
                    if (property.hasuniquename)
                    {
                        //Possible alignment
                        structWriter.AlignOrThrow(BytesUsed);
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
