using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfEnum_16t"/> structure.
    /// </summary>
    public readonly unsafe struct LfEnum16t : IViewable
    {
        private const int typlenOffset = 0;
        private const int leafOffset = 2;
        private const int countOffset = 4;
        private const int utypeOffset = 6;
        private const int fieldOffset = 8;
        private const int propertyOffset = 10;
        private const int NameOffset = 12;
        private int uniquenameOffset => NameOffset + Name.Length + 1;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfEnum_16t* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public short count => value->count;

        public TypOrEnumType utype => new TypOrEnumType((byte*) value, value->utype);

        public TypOrEnumType field => new TypOrEnumType((byte*) value, value->field);

        public CV_prop_t property => value->property;

        public SymString Name => TypType.ReadString(value->Name);

        public SymString uniquename => GetUniqueName(null);

        #region PESpy

        public SymString GetName(ICodeViewAccessor? codeViewAccessor) => TypType.ReadString(value->Name, codeViewAccessor);

        internal SymString GetUniqueName(ICodeViewAccessor? codeViewAccessor)
        {
            if (property.hasuniquename)
            {
                var name = TypType.ReadString(value->Name, codeViewAccessor);

                var uniqueName = TypType.ReadString(value->Name + name.Length + 1, codeViewAccessor);

                return uniqueName;
            }

            return default;
        }

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            sizeof(short)  + //count
            sizeof(short)  + //utype
            sizeof(short)  + //field
            2;               //property

        private int BytesUsed()
        {
            var name = TypType.ReadString(value->Name);

            var length = FixedStructSize + name.Length + 1;

            if (property.hasuniquename)
            {
                var uniqueName = TypType.ReadString(value->Name + name.Length + 1);

                length += uniquename.Length + 1;
            }

            return length;
        }

        internal LfEnum16t(lfEnum_16t* value)
        {
            this.value = value;
        }

        public static implicit operator LfEasy(LfEnum16t easy) => new LfEasy((lfEasy*) (byte*) easy.value);

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.lfEnum_16t, this, ViewKind.LfEnum16t, typlen + sizeof(short));

        int IViewable.NumChildren() => StructWriter.GetNumChildrenAlign4(7, BytesUsed());

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
                    structWriter.WriteField(nameof(utype), utypeOffset, value->utype);
                    break;

                case 4:
                    structWriter.WriteField(nameof(field), fieldOffset, value->field);
                    break;

                case 5:
                    structWriter.WriteField(nameof(property), propertyOffset, property);
                    break;

                case 6:
                    structWriter.WriteSymStringField(nameof(Name), NameOffset, TypType.ReadString(value->Name, structWriter.GetSymbolAccessor()));
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
            return Name.ToString();
        }
    }
}
