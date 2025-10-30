using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="CONSTSYM_16t"/> structure.
    /// </summary>
    public readonly unsafe struct ConstSym16t : IViewable
    {
        private const int reclenOffset = 0;
        private const int rectypOffset = 2;
        private const int typindOffset = 4;
        private const int valueOffset = 6;
        private int nameOffset
        {
            get
            {
                TypType.ExtractNumericData((byte*) &raw->value, out _, out var bytesRead);

                return valueOffset + bytesRead;
            }
        }


        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly CONSTSYM_16t* raw;

        /// <inheritdoc cref="CONSTSYM_16t.reclen"/>
        public ushort reclen => raw->reclen;

        /// <inheritdoc cref="CONSTSYM_16t.rectyp"/>
        public SYM_ENUM_e rectyp => raw->rectyp;

        /// <inheritdoc cref="CONSTSYM_16t.typind"/>
        public TypOrEnumType typind => new TypOrEnumType((byte*) raw, raw->typind);

        /// <inheritdoc cref="CONSTSYM_16t.value"/>
        public ulong value
        {
            get
            {
                //Length may be 0, this is normal
                TypType.ExtractNumericData((byte*) &raw->value, out var value, out _);

                return value;
            }
        }

        /// <inheritdoc cref="CONSTSYM_16t.name"/>
        public SymString name => SymType.ReadString(raw, raw->name);

        #region PESpy

        internal SymString GetName(ICodeViewAccessor? codeViewAccessor)
        {
            TypType.ExtractNumericData((byte*) &raw->value, out _, out var bytesRead);

            return SymType.ReadString(raw, (byte*) &raw->value + bytesRead, codeViewAccessor);
        }

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(short); //typind

        private int BytesUsed
        {
            get
            {
                TypType.ExtractNumericData((byte*) &raw->value, out _, out var bytesRead);

                var str = SymType.ReadString(raw, (byte*) &raw->value + bytesRead);

                return FixedStructSize + bytesRead + str.Length + 1;
            }
        }

        internal ConstSym16t(CONSTSYM_16t* value)
        {
            this.raw = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.CONSTSYM_16t, this, ViewKind.ConstSym16t, SymType.GetSymbolLength((SYMTYPE*) raw, writer.GetSymbolAccessor()));

        int IViewable.NumChildren() => StructWriter.GetNumChildrenAlign4(5, BytesUsed);

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(reclen), reclenOffset, reclen);
                    break;

                case 1:
                    structWriter.WriteField(nameof(rectyp), rectypOffset, rectyp, sizeof(ushort));
                    break;

                case 2:
                    structWriter.WriteField(nameof(typind), typindOffset, raw->typind);
                    break;

                case 3:
                    structWriter.WriteNumericData(nameof(value), valueOffset, (byte*) &raw->value);
                    break;

                case 4:
                    structWriter.WriteSymStringField(nameof(name), nameOffset, GetName(structWriter.GetSymbolAccessor()));
                    break;

                case 5:
                    //Possible alignment
                    structWriter.AlignOrThrow(BytesUsed);
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
