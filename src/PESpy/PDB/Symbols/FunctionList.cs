using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="FUNCTIONLIST"/> structure.
    /// </summary>
    public readonly unsafe struct FunctionList : IViewable
    {
        private const int reclenOffset = 0;
        private const int rectypOffset = 2;
        private const int countOffset = 4;
        private const int funcsOffset = 8;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly FUNCTIONLIST* value;

        public static implicit operator SymType(FunctionList value) => new SymType((SYMTYPE*) value.value);

        /// <inheritdoc cref="FUNCTIONLIST.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="FUNCTIONLIST.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="FUNCTIONLIST.count"/>
        public int count => value->count;

        /// <inheritdoc cref="FUNCTIONLIST.funcs"/>
        public NativeSpan<CV_typ_t> funcs => new NativeSpan<CV_typ_t>(value->funcs, count);

        public Span<int> invocations
        {
            get
            {
                /* Hanging off the end of the FUNCTIONLIST is a CV_typ_t[] with "count" elements in it, and an int[] of invocation counts
                 * with _at most_ "count" elements in it. Any invocation counts that extend beyond the end of this structure are implicitly 0.
                 * e.g. consider an S_CALLEES with reclen 10. Including sizeof(reclen) itself the FunctionList is 12 bytes. reclen + rectyp + count
                 * take up 8 bytes already, leaving 4 bytes for the trailing data. A CV_typ_t takes up 4 bytes, and now we're out of data.
                 * The invocation count for that item goes beyond the end of the structure, and so it is implicitly 0 */

                //No need to do SymType.GetSymbolLength; we know we're not one of the 3 dodgy ST ref symbols
                var remainingBytes = ((reclen + 2) - 8) - (count * 4);

                if (remainingBytes == 0)
                    return new Span<int>();

                var start = &value->funcs[count]; //Get the position after the last index (which is count - 1)
                return new Span<int>(start, remainingBytes / 4);
            }
        }

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int);     //count

        internal FunctionList(FUNCTIONLIST* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.FUNCTIONLIST, this, ViewKind.FunctionList, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

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
                    structWriter.WriteField(nameof(count), countOffset, count);
                    break;

                case 3:
                    structWriter.WriteField(nameof(funcs), funcsOffset, funcs);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
