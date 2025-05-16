using System;
using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="FUNCTIONLIST"/> structure.
    /// </summary>
    public readonly unsafe struct FunctionList
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly FUNCTIONLIST* value;

        /// <inheritdoc cref="FUNCTIONLIST.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="FUNCTIONLIST.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="FUNCTIONLIST.count"/>
        public int count => value->count;

        /// <inheritdoc cref="FUNCTIONLIST.funcs"/>
        public Span<CV_typ_t> funcs => new Span<CV_typ_t>(value->funcs, count);

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
    }
}

