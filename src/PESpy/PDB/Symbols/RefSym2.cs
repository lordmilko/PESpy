using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="REFSYM2"/> structure.
    /// </summary>
    public readonly unsafe struct RefSym2 : IViewable
    {
        private const int reclenOffset = 0;
        private const int rectypOffset = 2;
        private const int sumNameOffset = 4;
        private const int ibSymOffset = 8;
        private const int imodOffset = 12;
        private const int nameOffset = 14;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly REFSYM2* value;

        public static implicit operator SymType(RefSym2 value) => new SymType((SYMTYPE*) value.value);

        /// <inheritdoc cref="REFSYM2.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="REFSYM2.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="REFSYM2.sumName"/>
        public int sumName => value->sumName;

        /// <inheritdoc cref="REFSYM2.ibSym"/>
        public int ibSym => value->ibSym;

        /// <inheritdoc cref="REFSYM2.imod"/>
        public ushort imod => value->imod;

        /// <inheritdoc cref="REFSYM2.name"/>
        public SymString name => SymType.ReadString(value, value->name);

        #region PESpy

        public SymType Symbol => GetSymbol(null);

        internal SymType GetSymbol(ICodeViewAccessor? codeViewAccessor) => SymType.GetSymbol(value, imod, ibSym, codeViewAccessor);

        internal SymString GetName(ICodeViewAccessor? codeViewAccessor) => SymType.ReadString(value, value->name, codeViewAccessor);

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int)    + //sumName
            sizeof(int)    + //ibSym
            sizeof(short);   //imod

        private int BytesUsed() => FixedStructSize + name.Length + 1;

        internal RefSym2(REFSYM2* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.REFSYM2, this, ViewKind.RefSym2, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

        int IViewable.NumChildren() => StructWriter.GetNumChildrenAlign4(6, BytesUsed());

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
                    structWriter.WriteSymStringField(nameof(name), nameOffset, SymType.ReadString(value, value->name, structWriter.GetSymbolAccessor()));
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
