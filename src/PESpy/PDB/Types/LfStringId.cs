using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfStringId"/> structure.
    /// </summary>
    public readonly unsafe struct LfStringId : IViewable
    {
        private const int typlenOffset = 0;
        private const int leafOffset = 2;
        private const int idOffset = 4;
        private const int nameOffset = 8;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfStringId* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public TypOrEnumType id => new TypOrEnumType((byte*) value, value->id);

        public SymString name => TypType.ReadString(value->name);

        #region PESpy

        public SymString GetName(ICodeViewAccessor? codeViewAccessor) => TypType.ReadString(value->name, codeViewAccessor);

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            sizeof(int);     //id

        private int BytesUsed() => FixedStructSize + name.Length + 1;

        internal LfStringId(lfStringId* value)
        {
            this.value = value;
        }

        public static implicit operator LfEasy(LfStringId easy) => new LfEasy((lfEasy*) (byte*) easy.value);

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(this, ViewKind.LfStringId, typlen + sizeof(short));

        int IViewable.NumChildren() => StructWriter.GetNumPaddedChildren(4, typlen, BytesUsed());

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
                    structWriter.WriteField(nameof(id), idOffset, value->id);
                    break;

                case 3:
                    structWriter.WriteSymStringField(nameof(name), nameOffset, TypType.ReadString(value->name, structWriter.GetSymbolAccessor()));
                    break;

                case 4:
                    //Possible padding
                    structWriter.PadTypOrThrow(typlen, BytesUsed());
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
