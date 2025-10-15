using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfAlias"/> structure.
    /// </summary>
    public readonly unsafe struct LfAlias : IViewable
    {
        private const int typlenOffset = 0;
        private const int leafOffset = 2;
        private const int utypeOffset = 4;
        private const int NameOffset = 8;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfAlias* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public TypOrEnumType utype => new TypOrEnumType((byte*) value, value->utype);

        public SymString Name => TypType.ReadString(value->Name);

        #region PESpy

        internal SymString GetName(ISymbolAccessor? symbolAccessor) => TypType.ReadString(value->Name, symbolAccessor);

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            sizeof(int);     //utype

        private int BytesUsed => FixedStructSize + Name.Length + 1;

        internal LfAlias(lfAlias* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.lfAlias, this, ViewKind.LfAlias, typlen + sizeof(short));

        int IViewable.NumChildren => StructWriter.GetNumChildrenAlign4(4, BytesUsed);

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
                    structWriter.WriteField(nameof(utype), utypeOffset, value->utype);
                    break;

                case 3:
                    structWriter.WriteSymStringField(nameof(Name), NameOffset, TypType.ReadString(value->Name, structWriter.GetSymbolAccessor()));
                    break;

                case 4:
                    //Possible alignment
                    structWriter.AlignOrThrow(BytesUsed);
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
