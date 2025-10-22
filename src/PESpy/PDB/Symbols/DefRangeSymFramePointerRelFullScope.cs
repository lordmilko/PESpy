using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="DEFRANGESYMFRAMEPOINTERREL_FULL_SCOPE"/> structure.
    /// </summary>
    public readonly unsafe struct DefRangeSymFramePointerRelFullScope : IViewable
    {
        private const int reclenOffset = 0;
        private const int rectypOffset = 2;
        private const int offFramePointerOffset = 4;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly DEFRANGESYMFRAMEPOINTERREL_FULL_SCOPE* value;

        /// <inheritdoc cref="DEFRANGESYMFRAMEPOINTERREL_FULL_SCOPE.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="DEFRANGESYMFRAMEPOINTERREL_FULL_SCOPE.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="DEFRANGESYMFRAMEPOINTERREL_FULL_SCOPE.offFramePointer"/>
        public CV_off32_t offFramePointer => value->offFramePointer;

        internal const int StructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int);     //offFramePointer

        internal DefRangeSymFramePointerRelFullScope(DEFRANGESYMFRAMEPOINTERREL_FULL_SCOPE* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.DEFRANGESYMFRAMEPOINTERREL_FULL_SCOPE, this, ViewKind.DefRangeSymFramePointerRelFullScope, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

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
                    structWriter.WriteField(nameof(offFramePointer), offFramePointerOffset, offFramePointer);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
