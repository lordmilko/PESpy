using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="POGOINFO"/> structure.
    /// </summary>
    public readonly unsafe struct PogoInfo : IViewable
    {
        private const int reclenOffset = 0;
        private const int rectypOffset = 2;
        private const int invocationsOffset = 4;
        private const int dynCountOffset = 8;
        private const int numInstrsOffset = 12;
        private const int staInstLiveOffset = 16;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly POGOINFO* value;

        public static implicit operator SymType(PogoInfo value) => new SymType((SYMTYPE*) value.value);

        /// <inheritdoc cref="POGOINFO.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="POGOINFO.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="POGOINFO.invocations"/>
        public int invocations => value->invocations;

        /// <inheritdoc cref="POGOINFO.dynCount"/>
        public long dynCount => value->dynCount;

        /// <inheritdoc cref="POGOINFO.numInstrs"/>
        public int numInstrs => value->numInstrs;

        /// <inheritdoc cref="POGOINFO.staInstLive"/>
        public int staInstLive => value->staInstLive;

        #region PESpy

        public SymType Parent => GetParent(null);

        public SymType GetParent(ICodeViewModuleAccessor? codeViewModuleAccessor) => SymType.GetParent((SYMTYPE*) value, codeViewModuleAccessor);

        #endregion

        internal const int StructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int)    + //invocations
            sizeof(long)   + //dynCount
            sizeof(int)    + //numInstrs
            sizeof(int);     //staInstLive

        internal PogoInfo(POGOINFO* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.POGOINFO, this, ViewKind.PogoInfo, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

        int IViewable.NumChildren() => 6;

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
                    structWriter.WriteField(nameof(invocations), invocationsOffset, invocations);
                    break;

                case 3:
                    structWriter.WriteField(nameof(dynCount), dynCountOffset, dynCount);
                    break;

                case 4:
                    structWriter.WriteField(nameof(numInstrs), numInstrsOffset, numInstrs);
                    break;

                case 5:
                    structWriter.WriteField(nameof(staInstLive), staInstLiveOffset, staInstLive);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
