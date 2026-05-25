using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy
{
    //Rich Headers essentially capture the @comp.id of compilands. When the ImageSymbol is for @comp.id,
    //the value has the same meaning as rich headers

    //Another symbol with special meaning is @feat.00

    public struct ImageSymbol : IValue, IViewable
    {
        //Special section numbers
        public const short IMAGE_SYM_UNDEFINED = 0;
        public const short IMAGE_SYM_ABSOLUTE = -1;
        public const short IMAGE_SYM_SECTION_MAX = unchecked((short) 0xFEFF); //0xFF00-0xFFFF are special
        public const int IMAGE_SYM_SECTION_MAX_EX = int.MaxValue;

        public const ushort N_BTMASK = 0x000F;
        public const ushort N_TMASK = 0x0030;
        //N_TMASK1 and N_TMASK2 don't seem to be used
        public const ushort N_BTSHIFT = 4;
        public const ushort N_TSHIFT = 2;

        private const int NameOffset = 0;
        private const int ValueOffset = 8;
        private const int SectionNumberOffset = 12;
        private const int TypeOffset = 14;
        private const int StorageClassOffset = 16;
        private const int NumberOfAuxSymbolsOffset = 17;

        public NameOrOffset Name { get; }

        public uint Value => chunk.PeekUInt32(ValueOffset);

        public ushort SectionNumber => chunk.PeekUInt16(SectionNumberOffset);

        public IMAGE_SYM_TYPE Type => (IMAGE_SYM_TYPE) chunk.PeekUInt16(TypeOffset);

        public IMAGE_SYM_TYPE BasicType => (IMAGE_SYM_TYPE) ((ushort) Type & N_BTMASK);

        public IMAGE_SYM_DTYPE DerivedType => (IMAGE_SYM_DTYPE) (((ushort) Type & N_TMASK) >> N_BTSHIFT);

        public IMAGE_SYM_CLASS StorageClass => (IMAGE_SYM_CLASS) chunk.PeekByte(StorageClassOffset);

        public byte NumberOfAuxSymbols => chunk.PeekByte(NumberOfAuxSymbolsOffset);

        public ImageAuxSymbol[] AuxSymbols { get; }

        /// <summary>
        /// Gets additional contextual data about this symbol based on its <see cref="Name"/>.<para/>
        /// When the name is "@comp.id", this value will be a <see cref="ProdItem"/>.
        /// </summary>
        public object Data
        {
            get
            {
                if (Name.Name == "@comp.id"u8)
                    return new ProdItem(Value);

                return null;
            }
        }

        public long Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            8 + //Name (both halves)
            sizeof(int) + //Value
            sizeof(short) + //SectionNumber
            sizeof(short) + //Type
            sizeof(byte) + //StorageClass
            sizeof(byte); //NumberOfAuxSymbols

        private readonly MemoryChunk chunk;

        internal ImageSymbol(in MemoryChunk chunk, CoffSymbolTable symbolTable)
        {
            this.chunk = chunk;
            AuxSymbols = default!;

            /* If the name is 8 bytes or less, it can be declared immediately inline. Otherwise,
             * the name is declared in the string table that immediately follows the list of symbols,
             * and the name contains a pointer into it. If the first 4 bytes of the name are all 0, then
             * the second 4 bytes is an offset into the string table. Otherwise, the name is the name */
            Name = new NameOrOffset(chunk, symbolTable);

            var numAux = NumberOfAuxSymbols;

            if (numAux == 0)
                AuxSymbols = Array.Empty<ImageAuxSymbol>();
            else
            {
                var results = new ImageAuxSymbol[numAux];

                var read = 18;

                for (var i = 0; i < numAux; i++)
                {
                    var aux = new ImageAuxSymbol(chunk.Slice(read));

                    read += ImageAuxSymbol.StructSize;

                    results[i] = aux;
                }

                AuxSymbols = results;
            }
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.ImageSymbol, StructSize + (NumberOfAuxSymbols * ImageAuxSymbol.StructSize));

        int IViewable.NumChildren() => (Name.Short == 0 ? 2 : 1) + 5 + AuxSymbols.Length;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            if (Name.Short == 0)
            {
                switch (index)
                {
                    case 0:
                        structWriter.WriteField("Name.Short", NameOffset, Name.Short);
                        return;

                    case 1:
                        structWriter.WriteField("Name.Long", NameOffset + 4, Name.Long);
                        return;
                }

                //Everything after index 1 we pretend as if we're in long mode, where there was one previous child
                index--;
            }
            else
            {
                if (index == 0)
                {
                    structWriter.WriteNullPaddedAnsiField(nameof(Name), NameOffset, Name.Name, 8);
                    return;
                }
            }

            switch (index)
            {
                case 1:
                    structWriter.WriteField(nameof(Value), ValueOffset, Value);
                    break;

                case 2:
                    structWriter.WriteField(nameof(SectionNumber), SectionNumberOffset, SectionNumber);
                    break;

                case 3:
                    structWriter.WriteField(nameof(Type), TypeOffset, Type, sizeof(short));
                    break;

                case 4:
                    structWriter.WriteField(nameof(StorageClass), StorageClassOffset, StorageClass, sizeof(byte));
                    break;

                case 5:
                    structWriter.WriteField(nameof(NumberOfAuxSymbols), NumberOfAuxSymbolsOffset, NumberOfAuxSymbols);
                    break;

                default:
                    structWriter.WriteInline(AuxSymbols[index - 6]);
                    break;
            }
        }

        public readonly struct NameOrOffset
        {
            public FixedAnsiString Name { get; }

            public int Short { get; }
            public int Long { get; }

            internal NameOrOffset(in MemoryChunk chunk, CoffSymbolTable symbolTable)
            {
                var @short = chunk.PeekInt32(0);
                var @long = chunk.PeekInt32(4);

                if (@short == 0)
                {
                    Short = @short;
                    Long = @long;
                    Name = (FixedAnsiString) symbolTable.GetString(Long);
                }
                else
                {
                    Name = chunk.PeekNullPaddedAnsi(0, 8);
                    Short = @short;
                    Long = @long;
                }
            }

            public static bool operator ==(NameOrOffset left, string right) => left.Name == right;
            public static bool operator !=(NameOrOffset left, string right) => left.Name != right;

            public override bool Equals(object obj)
            {
                if (obj is not NameOrOffset o)
                    return false;

                return Short == o.Short && Long == o.Long;
            }

            public override int GetHashCode()
            {
                var hashCode = Short.GetHashCode();

                hashCode = (hashCode ^ 397) ^ Long.GetHashCode();

                return hashCode;
            }

            public override string ToString()
            {
                return Name.ToString();
            }
        }

        public override string ToString()
        {
            return Name.ToString();
        }
    }
}
