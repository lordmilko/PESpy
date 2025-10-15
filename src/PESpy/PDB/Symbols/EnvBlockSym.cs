using System.Collections.Generic;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="ENVBLOCKSYM"/> structure.
    /// </summary>
    public readonly unsafe struct EnvBlockSym : IViewable
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

                using var results = new PooledList<AnsiString>();

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

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.ENVBLOCKSYM, this, ViewKind.EnvBlockSym, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(reclen), reclen);
            s.WriteField(nameof(rectyp), rectyp, sizeof(ushort));

            using (var bitField = s.WriteBitFields<byte>())
            {
                bitField.WriteField(nameof(rev), rev, 1);
                bitField.WriteField(nameof(pad), pad, 7);
            }

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }
    }
}
