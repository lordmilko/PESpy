using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="SLOTSYM32"/> structure.
    /// </summary>
    public readonly unsafe struct SlotSym32 : IViewable
    {
        private const int reclenOffset = 0;
        private const int rectypOffset = 2;
        private const int iSlotOffset = 4;
        private const int typindOffset = 8;
        private const int nameOffset = 12;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly SLOTSYM32* value;

        public static implicit operator SymType(SlotSym32 value) => new SymType((SYMTYPE*) value.value);

        /// <inheritdoc cref="SLOTSYM32.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="SLOTSYM32.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="SLOTSYM32.iSlot"/>
        public int iSlot => value->iSlot;

        /// <inheritdoc cref="SLOTSYM32.typind"/>
        public TypOrEnumType typind => new TypOrEnumType((byte*) value, value->typind);

        /// <inheritdoc cref="SLOTSYM32.name"/>
        public SymString name => SymType.ReadString(value, value->name);

        #region PESpy

        public SymString GetName(ICodeViewAccessor? codeViewAccessor) => SymType.ReadString(value, value->name, codeViewAccessor);

        public SymType Parent => GetParent(null);

        public SymType GetParent(ICodeViewModuleAccessor? codeViewModuleAccessor) => SymType.GetParent((SYMTYPE*) value, codeViewModuleAccessor);

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int)    + //iSlot
            sizeof(int);     //typind

        private int BytesUsed() => FixedStructSize + name.Length + 1;

        internal SlotSym32(SLOTSYM32* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(this, ViewKind.SlotSym32, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

        int IViewable.NumChildren() => StructWriter.GetNumChildrenAlign4(5, BytesUsed());

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
                    structWriter.WriteSymStringField(nameof(name), nameOffset, SymType.ReadString(value, value->name, structWriter.GetSymbolAccessor()));
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
