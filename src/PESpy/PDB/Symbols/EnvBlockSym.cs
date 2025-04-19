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

        public ushort reclen => value->reclen;

        public SYM_ENUM_e rectyp => value->rectyp;

        public bool rev => value->rev;

        public byte pad => value->pad;

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

        internal EnvBlockSym(ENVBLOCKSYM* value)
        {
            this.value = value;
        }
    }
}

