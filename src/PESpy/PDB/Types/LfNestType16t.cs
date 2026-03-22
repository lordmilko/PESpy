using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfNestType_16t"/> structure.
    /// </summary>
    public readonly unsafe struct LfNestType16t : IViewable
    {
        private const int leafOffset = 0;
        private const int indexOffset = 2;
        private const int NameOffset = 6;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfNestType_16t* value;

        //This type is only ever referenced from other records and so does not have a TYPTYPE.len

        public LEAF_ENUM_e leaf => value->leaf;

        public TypOrEnumType index => new TypOrEnumType((byte*) value, value->index);

        public SymString Name => TypType.ReadString(value->Name);

        #region PESpy

        public SymString GetName(ICodeViewAccessor? codeViewAccessor) => TypType.ReadString(value->Name, codeViewAccessor);

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            sizeof(short);   //index

        internal int StructSize => GetStructSize(null);

        internal int GetStructSize(ICodeViewAccessor? codeViewAccessor)
        {
            var str = TypType.ReadString(value->Name, codeViewAccessor);

            return FixedStructSize + str.Length + 1;
        }

        internal LfNestType16t(lfNestType_16t* value)
        {
            this.value = value;
        }

        public static implicit operator LfEasy(LfNestType16t easy) => new LfEasy((lfEasy*) (byte*) easy.value);

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.lfNestType_16t, this, ViewKind.LfNestType16t, GetStructSize(writer.GetSymbolAccessor()));

        int IViewable.NumChildren() => 3;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(leaf), leafOffset, leaf, sizeof(ushort));
                    break;

                case 1:
                    structWriter.WriteField(nameof(index), indexOffset, value->index);
                    break;

                case 2:
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
