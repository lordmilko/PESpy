using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="REFSYM"/> structure.
    /// </summary>
    public readonly unsafe struct RefSym : IViewable
    {
        private const int reclenOffset = 0;
        private const int rectypOffset = 2;
        private const int sumNameOffset = 4;
        private const int ibSymOffset = 8;
        private const int imodOffset = 12;
        private const int usFillOffset = 14;
        private const int nameOffset = 16;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly REFSYM* value;

        public static implicit operator SymType(RefSym value) => new SymType((SYMTYPE*) value.value);

        /// <inheritdoc cref="REFSYM.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="REFSYM.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="REFSYM.sumName"/>
        public int sumName => value->sumName;

        /// <inheritdoc cref="REFSYM.ibSym"/>
        public int ibSym => value->ibSym;

        /// <inheritdoc cref="REFSYM.imod"/>
        public ushort imod => value->imod;

        /// <inheritdoc cref="REFSYM.usFill"/>
        public short usFill => value->usFill;

        //RefSym is the symbol type used by old ST symbols. These symbols have a hidden name after them not accounted for in their lengths
        public SymString name => GetName(null);

        #region PESpy

        public SymType Symbol => GetSymbol(null);

        internal SymType GetSymbol(ICodeViewAccessor? codeViewAccessor) => SymType.GetSymbol(value, imod, ibSym, codeViewAccessor);

        internal SymString GetName(ICodeViewAccessor? codeViewAccessor)
        {
            codeViewAccessor ??= SymbolMemoryTracker.GetAccessor((long) value);

            //If we're NB11, there isn't a hidden name after us
            if (codeViewAccessor is NB05SymbolAccessor a && a.CodeViewSig == CodeViewSig.NB11)
                return default;

            return SymType.ReadString(value, ((byte*) value) + reclen + sizeof(ushort), codeViewAccessor);
        }

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int)    + //sumName
            sizeof(int)    + //ibSym
            sizeof(short)  + //imod
            sizeof(short);   //usFill

        //todo: dont necessarily include the name in all of the symbols that check for nb11 like we do here
        private int BytesUsed() => FixedStructSize + name.Length + 1;

        internal RefSym(REFSYM* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.REFSYM, this, ViewKind.RefSym, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

        int IViewable.NumChildren() => StructWriter.GetNumChildrenAlign4(7, BytesUsed());

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
                    structWriter.WriteField(nameof(sumName), sumNameOffset, sumName);
                    break;

                case 3:
                    structWriter.WriteField(nameof(ibSym), ibSymOffset, ibSym);
                    break;

                case 4:
                    structWriter.WriteField(nameof(imod), imodOffset, imod);
                    break;

                case 5:
                    structWriter.WriteField(nameof(usFill), usFillOffset, usFill);
                    break;

                case 6:
                    structWriter.WriteSymStringField(nameof(name), nameOffset, GetName(structWriter.GetSymbolAccessor()));
                    break;

                case 7:
                    //Possible alignment
                    structWriter.AlignOrThrow(BytesUsed());
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }

        public override string ToString()
        {
            var str = name;

            if (str.Length == 0)
                return Symbol.ToString();

            return str.ToString();
        }
    }
}
