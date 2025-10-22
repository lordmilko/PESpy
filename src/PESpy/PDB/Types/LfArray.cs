using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfArray"/> structure.
    /// </summary>
    public readonly unsafe struct LfArray : IViewable
    {
        private const int typlenOffset = 0;
        private const int leafOffset = 2;
        private const int elemtypeOffset = 4;
        private const int idxtypeOffset = 8;
        private const int lengthOffset = 12;
        private int nameOffset
        {
            get
            {
                TypType.ExtractNumericData(value->data, out _, out var bytesRead);

                return 12 + bytesRead;
            }
        }


        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfArray* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public TypOrEnumType elemtype => new TypOrEnumType((byte*) value, value->elemtype);

        public TypOrEnumType idxtype => new TypOrEnumType((byte*) value, value->idxtype);

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            sizeof(int)    + //elemtype
            sizeof(int);     //idxtype

        #region data

        public int length
        {
            get
            {
                TypType.ExtractNumericData(value->data, out var length, out var bytesRead);

                return (int) length;
            }
        }

        public SymString name => GetName(null);

        #endregion
        #region PESpy

        internal SymString GetName(ISymbolAccessor? symbolAccessor)
        {
            TypType.ExtractNumericData(value->data, out _, out var bytesRead);

            //I am assuming I need to use normal ST/UTF parsing logic
            return TypType.ReadString(value->data + bytesRead, symbolAccessor);
        }

        #endregion

        private int BytesUsed
        {
            get
            {
                TypType.ExtractNumericData(value->data, out _, out var bytesRead);

                var str = TypType.ReadString(value->data + bytesRead);

                return FixedStructSize + bytesRead + str.Length;
            }
        }

        internal LfArray(lfArray* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.lfArray, this, ViewKind.LfArray, typlen + sizeof(short));

        int IViewable.NumChildren() => StructWriter.GetNumChildrenAlign4(6, BytesUsed);

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
                    structWriter.WriteField(nameof(elemtype), elemtypeOffset, value->elemtype);
                    break;

                case 3:
                    structWriter.WriteField(nameof(idxtype), idxtypeOffset, value->idxtype);
                    break;

                case 4:
                    structWriter.WriteNumericData(nameof(length), lengthOffset, value->data);
                    break;

                case 5:
                    structWriter.WriteSymStringField(nameof(name), nameOffset, GetName(structWriter.GetSymbolAccessor()));
                    break;

                case 6:
                    //Possible alignment
                    structWriter.AlignOrThrow(BytesUsed);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }

        public override string ToString()
        {
            return $"{elemtype}[{length}]";
        }
    }
}
