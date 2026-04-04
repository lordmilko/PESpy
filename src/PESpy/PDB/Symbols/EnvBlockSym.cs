using System;
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
        private const int reclenOffset = 0;
        private const int rectypOffset = 2;
        private const int flagsOffset = 4;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly ENVBLOCKSYM* value;

        public static implicit operator SymType(EnvBlockSym value) => new SymType((SYMTYPE*) value.value);

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

        #region PESpy

        public SymType Parent => GetParent(null);

        public SymType GetParent(ICodeViewModuleAccessor? codeViewModuleAccessor) => SymType.GetParent((SYMTYPE*) value, codeViewModuleAccessor);

        #endregion

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

                #region BitField

                case 2:
                    structWriter.WriteBitField(nameof(rev), flagsOffset, rev, sizeof(byte), 1);
                    break;

                case 3:
                    structWriter.WriteBitField(nameof(pad), flagsOffset, pad, sizeof(byte), 7);
                    break;

                #endregion

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
