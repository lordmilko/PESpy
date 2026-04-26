using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfPreComp_16t"/> structure.
    /// </summary>
    public readonly unsafe struct LfPreComp16t : IViewable
    {
        private const int typlenOffset = 0;
        private const int leafOffset = 2;
        private const int startOffset = 4;
        private const int countOffset = 6;
        private const int signatureOffset = 8;
        private const int nameOffset = 12;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfPreComp_16t* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public short start => value->start;

        public short count => value->count;

        public int signature => value->signature;

        public SymString name => TypType.ReadString(value->name);

        #region PESpy

        public SymString GetName(ICodeViewAccessor? codeViewAccessor) => TypType.ReadString(value->name, codeViewAccessor);

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            sizeof(short)  + //start
            sizeof(short)  + //count
            sizeof(int);     //signature

        private int BytesUsed() => FixedStructSize + name.Length + 1;

        internal LfPreComp16t(lfPreComp_16t* value)
        {
            this.value = value;
        }

        public static implicit operator LfEasy(LfPreComp16t easy) => new LfEasy((lfEasy*) (byte*) easy.value);

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(this, ViewKind.LfPreComp16t, typlen + sizeof(short));

        int IViewable.NumChildren() => StructWriter.GetNumChildrenAlign4(6, BytesUsed());

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
                    structWriter.WriteField(nameof(start), startOffset, start);
                    break;

                case 3:
                    structWriter.WriteField(nameof(count), countOffset, count);
                    break;

                case 4:
                    structWriter.WriteField(nameof(signature), signatureOffset, signature);
                    break;

                case 5:
                    structWriter.WriteSymStringField(nameof(name), nameOffset, TypType.ReadString(value->name, structWriter.GetSymbolAccessor()));
                    break;

                case 6:
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
