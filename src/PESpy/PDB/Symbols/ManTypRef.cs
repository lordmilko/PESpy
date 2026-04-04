using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="MANTYPREF"/> structure.
    /// </summary>
    public readonly unsafe struct ManTypRef : IViewable
    {
        private const int reclenOffset = 0;
        private const int rectypOffset = 2;
        private const int typindOffset = 4;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly MANTYPREF* value;

        public static implicit operator SymType(ManTypRef value) => new SymType((SYMTYPE*) value.value);

        /// <inheritdoc cref="MANTYPREF.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="MANTYPREF.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="MANTYPREF.typind"/>
        public TypOrEnumType typind => new TypOrEnumType((byte*) value, value->typind);

        #region PESpy

        public SymType Parent => GetParent(null);

        public SymType GetParent(ICodeViewModuleAccessor? codeViewModuleAccessor) => SymType.GetParent((SYMTYPE*) value, codeViewModuleAccessor);

        #endregion

        internal const int StructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int);     //typind

        internal ManTypRef(MANTYPREF* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.MANTYPREF, this, ViewKind.ManTypRef, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

        int IViewable.NumChildren() => 3;

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
                    structWriter.WriteField(nameof(typind), typindOffset, value->typind);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
