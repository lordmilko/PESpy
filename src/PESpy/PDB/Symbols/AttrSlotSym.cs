using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="ATTRSLOTSYM"/> structure.
    /// </summary>
    public readonly unsafe struct AttrSlotSym : IViewable
    {
        private const int reclenOffset = 0;
        private const int rectypOffset = 2;
        private const int iSlotOffset = 4;
        private const int typindOffset = 8;
        private const int attrOffset = 12;
        private const int nameOffset = 20;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly ATTRSLOTSYM* value;

        /// <inheritdoc cref="ATTRSLOTSYM.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="ATTRSLOTSYM.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="ATTRSLOTSYM.iSlot"/>
        public int iSlot => value->iSlot;

        /// <inheritdoc cref="ATTRSLOTSYM.typind"/>
        public TypOrEnumType typind => new TypOrEnumType((byte*) value, value->typind);

        /// <inheritdoc cref="ATTRSLOTSYM.attr"/>
        public CV_lvar_attr attr => value->attr;

        /// <inheritdoc cref="ATTRSLOTSYM.name"/>
        public SymString name => SymType.ReadString(value, value->name);

        #region PESpy

        internal SymString GetName(ISymbolAccessor? symbolAccessor) => SymType.ReadString(value, value->name, symbolAccessor);

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int)    + //iSlot
            sizeof(int)    + //typind
            8;               //attr

        private int BytesUsed => FixedStructSize + name.Length + 1;

        internal AttrSlotSym(ATTRSLOTSYM* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.ATTRSLOTSYM, this, ViewKind.AttrSlotSym, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

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
                    structWriter.WriteField(nameof(iSlot), iSlotOffset, iSlot);
                    break;

                case 3:
                    structWriter.WriteField(nameof(typind), typindOffset, value->typind);
                    break;

                case 4:
                    structWriter.WriteField(nameof(attr), attrOffset, attr);
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
