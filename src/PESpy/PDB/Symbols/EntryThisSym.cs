using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="ENTRYTHISSYM"/> structure.
    /// </summary>
    public readonly unsafe struct EntryThisSym : IViewable
    {
        private const int reclenOffset = 0;
        private const int rectypOffset = 2;
        private const int thissymOffset = 4;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly ENTRYTHISSYM* value;

        public static implicit operator SymType(EntryThisSym value) => new SymType((SYMTYPE*) value.value);

        /// <inheritdoc cref="ENTRYTHISSYM.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="ENTRYTHISSYM.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="ENTRYTHISSYM.thissym"/>
        public byte thissym => value->thissym;

        #region PESpy

        public SymType Parent => GetParent(null);

        public SymType GetParent(ICodeViewModuleAccessor? codeViewModuleAccessor) => SymType.GetParent((SYMTYPE*) value, codeViewModuleAccessor);

        #endregion

        internal const int StructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(byte);    //thissym

        internal EntryThisSym(ENTRYTHISSYM* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(this, ViewKind.EntryThisSym, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

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
                    structWriter.WriteField(nameof(thissym), thissymOffset, thissym);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
