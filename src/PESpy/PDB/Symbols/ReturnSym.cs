using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="RETURNSYM"/> structure.
    /// </summary>
    public readonly unsafe struct ReturnSym : IViewable
    {
        private const int reclenOffset = 0;
        private const int rectypOffset = 2;
        private const int flagsOffset = 4;
        private const int styleOffset = 4;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly RETURNSYM* value;

        public static implicit operator SymType(ReturnSym value) => new SymType((SYMTYPE*) value.value);

        /// <inheritdoc cref="RETURNSYM.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="RETURNSYM.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="RETURNSYM.flags"/>
        public CV_GENERIC_FLAG flags => value->flags;

        /// <inheritdoc cref="RETURNSYM.style"/>
        public CV_GENERIC_STYLE_e style => value->style;

        #region PESpy

        public SymType Parent => GetParent(null);

        public SymType GetParent(ICodeViewModuleAccessor? codeViewModuleAccessor) => SymType.GetParent((SYMTYPE*) value, codeViewModuleAccessor);

        #endregion

        internal const int StructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            2              + //flags
            sizeof(byte);    //style

        internal ReturnSym(RETURNSYM* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(this, ViewKind.ReturnSym, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

        int IViewable.NumChildren() => 4;

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
                    structWriter.WriteField(nameof(flags), flagsOffset, flags);
                    break;

                case 3:
                    structWriter.WriteField(nameof(style), styleOffset, style, sizeof(byte));
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
