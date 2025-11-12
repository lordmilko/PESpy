using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfMFuncId"/> structure.
    /// </summary>
    public readonly unsafe struct LfMFuncId : IViewable
    {
        private const int typlenOffset = 0;
        private const int leafOffset = 2;
        private const int parentTypeOffset = 4;
        private const int typeOffset = 8;
        private const int nameOffset = 12;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfMFuncId* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public TypOrEnumType parentType => new TypOrEnumType((byte*) value, value->parentType);

        public TypOrEnumType type => new TypOrEnumType((byte*) value, value->type);

        public SymString name => TypType.ReadString(value->name);

        #region PESpy

        internal SymString GetName(ICodeViewAccessor? codeViewAccessor) => TypType.ReadString(value->name, codeViewAccessor);

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            sizeof(int)    + //parentType
            sizeof(int);     //type

        private int BytesUsed() => FixedStructSize + name.Length + 1;

        internal LfMFuncId(lfMFuncId* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.lfMFuncId, this, ViewKind.LfMFuncId, typlen + sizeof(short));

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
                    structWriter.WriteField(nameof(parentType), parentTypeOffset, value->parentType);
                    break;

                case 3:
                    structWriter.WriteField(nameof(type), typeOffset, value->type);
                    break;

                case 4:
                    structWriter.WriteSymStringField(nameof(name), nameOffset, TypType.ReadString(value->name, structWriter.GetSymbolAccessor()));
                    break;

                case 5:
                    //Note: there's a bunch of unknown bytes at the end. Same with LfFuncId

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
