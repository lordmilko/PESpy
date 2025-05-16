using System.Collections.Generic;
using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="ENVBLOCKSYM"/> structure.
    /// </summary>
    public readonly unsafe struct EnvBlockSym
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly ENVBLOCKSYM* value;

        /// <inheritdoc cref="ENVBLOCKSYM.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="ENVBLOCKSYM.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="ENVBLOCKSYM.rev"/>
        public bool rev => value->rev;

        /// <inheritdoc cref="ENVBLOCKSYM.pad"/>
        public byte pad => value->pad;

        /// <inheritdoc cref="ENVBLOCKSYM.rgsz"/>
        public AnsiString[] rgsz
        {
            get
            {
                var ptr = value->rgsz;

                var results = new List<AnsiString>();

                while (true)
                {
                    var str = (AnsiString) ptr;
                    results.Add(str);

                    var length = str.Length;

                    if (length == 0)
                        break;

                    ptr += length + 1;
                }

                return results.ToArray();
            }
        }

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(byte);    //flags

        internal EnvBlockSym(ENVBLOCKSYM* value)
        {
            this.value = value;
        }
    }
}

