using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="BUILDINFOSYM"/> structure.
    /// </summary>
    public readonly unsafe struct BuildInfoSym : IViewable
    {
        private const int reclenOffset = 0;
        private const int rectypOffset = 2;
        private const int idOffset = 4;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly BUILDINFOSYM* value;

        public static implicit operator SymType(BuildInfoSym value) => new SymType((SYMTYPE*) value.value);

        /// <inheritdoc cref="BUILDINFOSYM.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="BUILDINFOSYM.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="BUILDINFOSYM.id"/>
        public TypOrEnumType id => new TypOrEnumType((byte*) value, value->id);

        #region PESpy

        public SymType Parent => GetParent(null);

        public SymType GetParent(ICodeViewModuleAccessor? codeViewModuleAccessor) => SymType.GetParent((SYMTYPE*) value, codeViewModuleAccessor);

        #endregion

        internal const int StructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int);     //id

        internal BuildInfoSym(BUILDINFOSYM* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(this, ViewKind.BuildInfoSym, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

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
                    structWriter.WriteField(nameof(id), idOffset, value->id);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
