using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="DPCSYMTAGMAP"/> structure.
    /// </summary>
    public readonly unsafe struct DPCSymTagMap : IViewable
    {
        private const int reclenOffset = 0;
        private const int rectypOffset = 2;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly DPCSYMTAGMAP* value;

        public static implicit operator SymType(DPCSymTagMap value) => new SymType((SYMTYPE*) value.value);

        /// <inheritdoc cref="DPCSYMTAGMAP.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="DPCSYMTAGMAP.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        #region PESpy

        public SymType Parent => GetParent(null);

        public SymType GetParent(ICodeViewModuleAccessor? codeViewModuleAccessor) => SymType.GetParent((SYMTYPE*) value, codeViewModuleAccessor);

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort);  //rectyp

        internal DPCSymTagMap(DPCSYMTAGMAP* value)
        {
            this.value = value;
            Debug.Assert(false, "Read CV_DPC_SYM_TAG_MAP_ENTRY[] mapEntries");
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(this, ViewKind.DPCSymTagMap, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

        int IViewable.NumChildren() => 2;

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

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
