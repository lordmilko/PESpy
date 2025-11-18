using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="MODTYPEREF"/> structure.
    /// </summary>
    public readonly unsafe struct ModTypeRef : IViewable
    {
        private const int reclenOffset = 0;
        private const int rectypOffset = 2;
        private const int dataOffset = 4;
        private const int word0Offset = 8;
        private const int word1Offset = 10;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly MODTYPEREF* value;

        public static implicit operator SymType(ModTypeRef value) => new SymType((SYMTYPE*) value.value);

        /// <inheritdoc cref="MODTYPEREF.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="MODTYPEREF.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="MODTYPEREF.fNone"/>
        public bool fNone => value->fNone;

        /// <inheritdoc cref="MODTYPEREF.fRefTMPCT"/>
        public bool fRefTMPCT => value->fRefTMPCT;

        /// <inheritdoc cref="MODTYPEREF.fOwnTMPCT"/>
        public bool fOwnTMPCT => value->fOwnTMPCT;

        /// <inheritdoc cref="MODTYPEREF.fOwnTMR"/>
        public bool fOwnTMR => value->fOwnTMR;

        /// <inheritdoc cref="MODTYPEREF.fOwnTM"/>
        public bool fOwnTM => value->fOwnTM;

        /// <inheritdoc cref="MODTYPEREF.fRefTM"/>
        public bool fRefTM => value->fRefTM;

        /// <inheritdoc cref="MODTYPEREF.reserved"/>
        public int reserved => value->reserved;

        /// <inheritdoc cref="MODTYPEREF.word0"/>
        public short word0 => value->word0;

        /// <inheritdoc cref="MODTYPEREF.word1"/>
        public short word1 => value->word1;

        internal const int StructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int)    + //data
            sizeof(short)  + //word0
            sizeof(short);   //word1

        internal ModTypeRef(MODTYPEREF* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.MODTYPEREF, this, ViewKind.ModTypeRef, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

        int IViewable.NumChildren() => 11;

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
                    structWriter.WriteBitField(nameof(fNone), dataOffset, fNone, sizeof(long), 1);
                    break;

                case 3:
                    structWriter.WriteBitField(nameof(fRefTMPCT), dataOffset, fRefTMPCT, sizeof(long), 1);
                    break;

                case 4:
                    structWriter.WriteBitField(nameof(fOwnTMPCT), dataOffset, fOwnTMPCT, sizeof(long), 1);
                    break;

                case 5:
                    structWriter.WriteBitField(nameof(fOwnTMR), dataOffset, fOwnTMR, sizeof(long), 1);
                    break;

                case 6:
                    structWriter.WriteBitField(nameof(fOwnTM), dataOffset, fOwnTM, sizeof(long), 1);
                    break;

                case 7:
                    structWriter.WriteBitField(nameof(fRefTM), dataOffset, fRefTM, sizeof(long), 1);
                    break;

                case 8:
                    structWriter.WriteBitField(nameof(reserved), dataOffset, reserved, sizeof(long), 1);
                    break;

                case 9:
                    structWriter.WriteBitField(nameof(word0), dataOffset, word0, sizeof(long), 1);
                    break;

                case 10:
                    structWriter.WriteBitField(nameof(word1), dataOffset, word1, sizeof(long), 9 + 16); //The bitfield is 32 bits, but Microsoft erroneously only added 9 bits of padding
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
