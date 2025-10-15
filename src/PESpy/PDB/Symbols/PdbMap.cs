using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="PDBMAP"/> structure.
    /// </summary>
    public readonly unsafe struct PdbMap : IViewable
    {
        private const int reclenOffset = 0;
        private const int rectypOffset = 2;
        private const int nameOffset = 4;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly PDBMAP* value;

        /// <inheritdoc cref="PDBMAP.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="PDBMAP.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="PDBMAP.name"/>
        public SymString name => SymType.ReadString(value, value->name);

        #region PESpy

        internal SymString GetName(ISymbolAccessor? symbolAccessor) => SymType.ReadString(value, value->name, symbolAccessor);

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort);  //rectyp

        private int BytesUsed => FixedStructSize + name.Length + 1;

        internal PdbMap(PDBMAP* value)
        {
            this.value = value;
            Debug.Assert(false, "Read destination PDB FileName");
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.PDBMAP, this, ViewKind.PdbMap, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

        int IViewable.NumChildren => StructWriter.GetNumChildrenAlign4(3, BytesUsed);

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
                    structWriter.WriteSymStringField(nameof(name), nameOffset, SymType.ReadString(value, value->name, structWriter.GetSymbolAccessor()));
                    break;

                case 3:
                    //Possible alignment
                    structWriter.AlignOrThrow(BytesUsed);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}
