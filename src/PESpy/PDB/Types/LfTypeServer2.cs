using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfTypeServer2"/> structure.
    /// </summary>
    public readonly unsafe struct LfTypeServer2 : IViewable
    {
        private const int typlenOffset = 0;
        private const int leafOffset = 2;
        private const int sig70Offset = 4;
        private const int ageOffset = 20;
        private const int nameOffset = 24;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfTypeServer2* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public Guid sig70 => value->sig70;

        public int age => value->age;

        public SymString name => TypType.ReadString(value->name);

        #region PESpy

        public SymString GetName(ICodeViewAccessor? codeViewAccessor) => TypType.ReadString(value->name, codeViewAccessor);

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            16             + //sig70
            sizeof(int);     //age

        private int BytesUsed() => FixedStructSize + name.Length + 1;

        internal LfTypeServer2(lfTypeServer2* value)
        {
            this.value = value;
        }

        public static implicit operator LfEasy(LfTypeServer2 easy) => new LfEasy((lfEasy*) (byte*) easy.value);

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.lfTypeServer2, this, ViewKind.LfTypeServer2, typlen + sizeof(short));

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
                    structWriter.WriteField(nameof(sig70), sig70Offset, sig70);
                    break;

                case 3:
                    structWriter.WriteField(nameof(age), ageOffset, age);
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
