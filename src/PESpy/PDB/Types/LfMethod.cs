using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfMethod"/> structure.
    /// </summary>
    public readonly unsafe struct LfMethod : IViewable
    {
        private const int leafOffset = 0;
        private const int countOffset = 2;
        private const int mListOffset = 4;
        private const int NameOffset = 8;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfMethod* value;

        //This type is only ever referenced from other records and so does not have a TYPTYPE.len

        public LEAF_ENUM_e leaf => value->leaf;

        public short count => value->count;

        public TypOrEnumType mList => new TypOrEnumType((byte*) value, value->mList);

        public SymString Name => TypType.ReadString(value->Name);

        #region PESpy

        public SymString GetName(ICodeViewAccessor? codeViewAccessor) => TypType.ReadString(value->Name, codeViewAccessor);

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            sizeof(short)  + //count
            sizeof(int);     //mList

        internal int StructSize => GetStructSize(null);

        internal int GetStructSize(ICodeViewAccessor? codeViewAccessor)
        {
            var str = TypType.ReadString(value->Name, codeViewAccessor);

            return FixedStructSize + str.Length + 1;
        }

        internal LfMethod(lfMethod* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.lfMethod, this, ViewKind.LfMethod, GetStructSize(writer.GetSymbolAccessor()));

        int IViewable.NumChildren() => 4;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(leaf), leafOffset, leaf, sizeof(ushort));
                    break;

                case 1:
                    structWriter.WriteField(nameof(count), countOffset, count);
                    break;

                case 2:
                    structWriter.WriteField(nameof(mList), mListOffset, value->mList);
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
