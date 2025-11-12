using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="REFMINIPDB"/> structure.
    /// </summary>
    public readonly unsafe struct RefMiniPdb : IViewable
    {
        private const int reclenOffset = 0;
        private const int rectypOffset = 2;
        private const int isectCoffOffset = 4;
        private const int typindOffset = 8;
        private const int imodOffset = 12;
        private const int dataOffset = 14;
        private const int nameOffset = 16;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly REFMINIPDB* value;

        /// <inheritdoc cref="REFMINIPDB.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="REFMINIPDB.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="REFMINIPDB.isectCoff"/>
        public int isectCoff => value->isectCoff;

        /// <inheritdoc cref="REFMINIPDB.typind"/>
        public TypOrEnumType typind => new TypOrEnumType((byte*) value, value->typind);

        /// <inheritdoc cref="REFMINIPDB.imod"/>
        public ushort imod => value->imod;

        /// <inheritdoc cref="REFMINIPDB.fLocal"/>
        public bool fLocal => value->fLocal;

        /// <inheritdoc cref="REFMINIPDB.fData"/>
        public bool fData => value->fData;

        /// <inheritdoc cref="REFMINIPDB.fUDT"/>
        public bool fUDT => value->fUDT;

        /// <inheritdoc cref="REFMINIPDB.fLabel"/>
        public bool fLabel => value->fLabel;

        /// <inheritdoc cref="REFMINIPDB.fConst"/>
        public bool fConst => value->fConst;

        /// <inheritdoc cref="REFMINIPDB.reserved"/>
        public short reserved => value->reserved;

        /// <inheritdoc cref="REFMINIPDB.name"/>
        public SymString name => SymType.ReadString(value, value->name);

        #region PESpy

        internal SymString GetName(ICodeViewAccessor? codeViewAccessor) => SymType.ReadString(value, value->name, codeViewAccessor);

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int)    + //isectCoff
            sizeof(int)    + //typind
            sizeof(short)  + //imod
            sizeof(short);   //data

        private int BytesUsed() => FixedStructSize + name.Length + 1;

        internal RefMiniPdb(REFMINIPDB* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.REFMINIPDB, this, ViewKind.RefMiniPdb, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

        int IViewable.NumChildren() => StructWriter.GetNumChildrenAlign4(12, BytesUsed());

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
                    structWriter.WriteField(nameof(isectCoff), isectCoffOffset, isectCoff);
                    break;

                case 3:
                    structWriter.WriteField(nameof(typind), typindOffset, value->typind);
                    break;


                #region BitField

                case 4:
                    structWriter.WriteBitField(nameof(imod), dataOffset, imod, sizeof(long), 1);
                    break;

                case 5:
                    structWriter.WriteBitField(nameof(fLocal), dataOffset, fLocal, sizeof(long), 1);
                    break;

                case 6:
                    structWriter.WriteBitField(nameof(fData), dataOffset, fData, sizeof(long), 1);
                    break;

                case 7:
                    structWriter.WriteBitField(nameof(fUDT), dataOffset, fUDT, sizeof(long), 1);
                    break;

                case 8:
                    structWriter.WriteBitField(nameof(fLabel), dataOffset, fLabel, sizeof(long), 1);
                    break;

                case 9:
                    structWriter.WriteBitField(nameof(fConst), dataOffset, fConst, sizeof(long), 1);
                    break;

                case 10:
                    structWriter.WriteBitField(nameof(reserved), dataOffset, reserved, sizeof(long), 11);
                    break;

                #endregion

                case 11:
                    structWriter.WriteSymStringField(nameof(name), nameOffset, SymType.ReadString(value, value->name, structWriter.GetSymbolAccessor()));
                    break;

                case 12:
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
