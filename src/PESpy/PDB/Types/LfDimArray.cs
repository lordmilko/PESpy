using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfDimArray"/> structure.
    /// </summary>
    public readonly unsafe struct LfDimArray : IViewable
    {
        private const int typlenOffset = 0;
        private const int leafOffset = 2;
        private const int utypeOffset = 4;
        private const int diminfoOffset = 8;
        private const int nameOffset = 12;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfDimArray* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public TypOrEnumType utype => new TypOrEnumType((byte*) value, value->utype);

        public TypOrEnumType diminfo => new TypOrEnumType((byte*) value, value->diminfo);

        public SymString name => TypType.ReadString(value->name);

        #region PESpy

        public SymString GetName(ICodeViewAccessor? codeViewAccessor) => TypType.ReadString(value->name, codeViewAccessor);

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            sizeof(int)    + //utype
            sizeof(int);     //diminfo

        private int BytesUsed() => FixedStructSize + name.Length + 1;

        internal LfDimArray(lfDimArray* value)
        {
            this.value = value;
        }

        public static implicit operator LfEasy(LfDimArray easy) => new LfEasy((lfEasy*) (byte*) easy.value);

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(this, ViewKind.LfDimArray, typlen + sizeof(short));

        int IViewable.NumChildren() => StructWriter.GetNumChildrenAlign4(5, BytesUsed());

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
                    structWriter.WriteField(nameof(diminfo), diminfoOffset, value->diminfo);
                    break;

                case 4:
                    structWriter.WriteSymStringField(nameof(name), nameOffset, TypType.ReadString(value->name, structWriter.GetSymbolAccessor()));
                    break;

                case 5:
                    //Possible alignment
                    structWriter.AlignOrThrow(BytesUsed());
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
