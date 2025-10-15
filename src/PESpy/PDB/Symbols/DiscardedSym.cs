using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="DISCARDEDSYM"/> structure.
    /// </summary>
    public readonly unsafe struct DiscardedSym : IViewable
    {
        private const int reclenOffset = 0;
        private const int rectypOffset = 2;
        private const int discardedDataOffset = 4;
        private const int fileidOffset = 8;
        private const int linenumOffset = 12;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly DISCARDEDSYM* value;

        /// <inheritdoc cref="DISCARDEDSYM.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="DISCARDEDSYM.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="DISCARDEDSYM.discarded"/>
        public CV_DISCARDED_e discarded => value->discarded;

        /// <inheritdoc cref="DISCARDEDSYM.reserved"/>
        public int reserved => value->reserved;

        /// <inheritdoc cref="DISCARDEDSYM.fileid"/>
        public int fileid => value->fileid;

        /// <inheritdoc cref="DISCARDEDSYM.linenum"/>
        public int linenum => value->linenum;

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int)    + //discardedData
            sizeof(int)    + //fileid
            sizeof(int);     //linenum

        internal DiscardedSym(DISCARDEDSYM* value)
        {
            this.value = value;
            Debug.Assert(false, "Read data");
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.DISCARDEDSYM, this, ViewKind.DiscardedSym, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

        int IViewable.NumChildren => 6;

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
                    structWriter.WriteBitField(nameof(discarded), discardedDataOffset, discarded, sizeof(int), 8);
                    break;

                case 3:
                    structWriter.WriteBitField(nameof(reserved), discardedDataOffset, reserved, sizeof(int), 24);
                    break;

                case 4:
                    structWriter.WriteField(nameof(fileid), fileidOffset, fileid);
                    break;

                case 5:
                    structWriter.WriteField(nameof(linenum), linenumOffset, linenum);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
