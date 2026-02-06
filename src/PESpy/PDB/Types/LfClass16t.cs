using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfClass_16t"/> structure.
    /// </summary>
    public readonly unsafe struct LfClass16t : IViewable
    {
        private const int typlenOffset = 0;
        private const int leafOffset = 2;
        private const int countOffset = 4;
        private const int fieldOffset = 6;
        private const int propertyOffset = 8;
        private const int derivedOffset = 10;
        private const int vshapeOffset = 12;
        private const int lengthOffset = 14;
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
        private readonly lfClass_16t* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public short count => value->count;

        public TypOrEnumType field => new TypOrEnumType((byte*) value, value->field);

        public CV_prop_t property => value->property;

        public TypOrEnumType derived => new TypOrEnumType((byte*) value, value->derived);

        public TypOrEnumType vshape => new TypOrEnumType((byte*) value, value->vshape);

        #region data

        //"data" describes the length of the structure in bytes, and name. In addition, if property.hasuniquename is set,
        //there is a decorated name following the name

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

        public SymString GetName(ICodeViewAccessor? codeViewAccessor)
        {
            TypType.ExtractNumericData(value->data, out _, out var bytesRead);

            //I am assuming I need to use normal ST/UTF parsing logic
            return TypType.ReadString(value->data + bytesRead, codeViewAccessor);
        }

        internal SymString GetUniqueName(ICodeViewAccessor? codeViewAccessor)
        {
            if (property.hasuniquename)
            {
                TypType.ExtractNumericData(value->data, out _, out var bytesRead);

                //I am assuming I need to use normal ST/UTF parsing logic
                var name = TypType.ReadString(value->data + bytesRead, codeViewAccessor);

                //I am assuming I need to use normal ST/UTF parsing logic
                return TypType.ReadString(value->data + bytesRead + name.Length + 1, codeViewAccessor); //+1 because it's either null terminated or length prefixed
            }

            return default;
        }

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            sizeof(short)  + //count
            sizeof(short)  + //field
            2              + //property
            sizeof(short)  + //derived
            sizeof(short);   //vshape

        private int BytesUsed()
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

        internal LfClass16t(lfClass_16t* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.lfClass_16t, this, ViewKind.LfClass16t, typlen + sizeof(short));

        int IViewable.NumChildren() => StructWriter.GetNumChildrenAlign4(8, BytesUsed()) + (property.hasuniquename ? 1 : 0);

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
                    structWriter.WriteField(nameof(derived), derivedOffset, value->derived);
                    break;

                case 6:
                    structWriter.WriteField(nameof(vshape), vshapeOffset, value->vshape);
                    break;

                case 7:
                    structWriter.WriteNumericData(nameof(length), lengthOffset, value->data);
                    break;

                case 8:
                    structWriter.WriteSymStringField(nameof(name), nameOffset, GetName(structWriter.GetSymbolAccessor()));
                    break;

                case 9:
                    if (property.hasuniquename)
                        structWriter.WriteSymStringField(nameof(uniquename), uniquenameOffset, GetUniqueName(structWriter.GetSymbolAccessor()));
                    else
                        structWriter.AlignOrThrow(BytesUsed());
                    break;

                case 10:
                    if (property.hasuniquename)
                    {
                        //Possible alignment
                        structWriter.AlignOrThrow(BytesUsed());
                    }
                    else
                        throw new NotImplementedException(); //We already aligned above

                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
