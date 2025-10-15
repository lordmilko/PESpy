using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="DATASYM32_16t"/> structure.
    /// </summary>
    [DebuggerDisplay("{SymTypeProxy.DebuggerDisplay(this),nq}")] //For S_PUB32_16t
    public readonly unsafe struct DataSym3216t : IViewable
    {
        private const int reclenOffset = 0;
        private const int rectypOffset = 2;
        private const int offOffset = 4;
        private const int segOffset = 8;
        private const int typindOffset = 10;
        private const int nameOffset = 12;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly DATASYM32_16t* value;

        /// <inheritdoc cref="DATASYM32_16t.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="DATASYM32_16t.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="DATASYM32_16t.off"/>
        public CV_uoff32_t off => value->off;

        /// <inheritdoc cref="DATASYM32_16t.seg"/>
        public ushort seg => value->seg;

        /// <inheritdoc cref="DATASYM32_16t.typind"/>
        public TypOrEnumType typind => new TypOrEnumType((byte*) value, value->typind);

        /// <inheritdoc cref="DATASYM32_16t.name"/>
        public SymString name => SymType.ReadString(value, value->name);

        #region PESpy

        public int? RelativeVirtualAddress => SymType.GetRelativeVirtualAddress(value, seg, off);

        internal SymString GetName(ISymbolAccessor? symbolAccessor) => SymType.ReadString(value, value->name, symbolAccessor);

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(uint)   + //off
            sizeof(short)  + //seg
            sizeof(short);   //typind

        private int BytesUsed => FixedStructSize + name.Length + 1;

        internal DataSym3216t(DATASYM32_16t* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.DATASYM32_16t, this, ViewKind.DataSym3216t, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

        int IViewable.NumChildren => StructWriter.GetNumChildrenAlign4(6, BytesUsed);

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
                    structWriter.WriteField(nameof(off), offOffset, off);
                    break;

                case 3:
                    structWriter.WriteField(nameof(seg), segOffset, seg);
                    break;

                case 4:
                    structWriter.WriteField(nameof(typind), typindOffset, value->typind);
                    break;

                case 5:
                    structWriter.WriteSymStringField(nameof(name), nameOffset, SymType.ReadString(value, value->name, structWriter.GetSymbolAccessor()));
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
            return name.ToString();
        }
    }
}
