using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfSTMember"/> structure.
    /// </summary>
    public readonly unsafe struct LfSTMember : IViewable
    {
        private const int leafOffset = 0;
        private const int attrOffset = 2;
        private const int indexOffset = 4;
        private const int NameOffset = 8;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfSTMember* value;

        //This type is only ever referenced from other records and so does not have a TYPTYPE.len

        public LEAF_ENUM_e leaf => value->leaf;

        public CV_fldattr_t attr => value->attr;

        public TypOrEnumType index => new TypOrEnumType((byte*) value, value->index);

        public SymString Name => TypType.ReadString(value->Name);

        #region PESpy

        internal SymString GetName(ISymbolAccessor? symbolAccessor) => TypType.ReadString(value->Name, symbolAccessor);

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            2              + //attr
            sizeof(int);     //index

        internal int StructSize => GetStructSize(null);

        internal int GetStructSize(ISymbolAccessor? symbolAccessor)
        {
            var str = TypType.ReadString(value->Name, symbolAccessor);

            return FixedStructSize + str.Length + 1;
        }

        internal LfSTMember(lfSTMember* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.lfSTMember, this, ViewKind.LfSTMember, GetStructSize(writer.GetSymbolAccessor()));

        int IViewable.NumChildren => 4;

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
                    structWriter.WriteField(nameof(index), indexOffset, index);
                    break;

                case 3:
                    structWriter.WriteSymStringField(nameof(Name), NameOffset, TypType.ReadString(value->Name, structWriter.GetSymbolAccessor()));
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
