using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="ARMSWITCHTABLE"/> structure.
    /// </summary>
    public readonly unsafe struct ArmSwitchTable : IViewable
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly ARMSWITCHTABLE* value;

        /// <inheritdoc cref="ARMSWITCHTABLE.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="ARMSWITCHTABLE.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="ARMSWITCHTABLE.offsetBase"/>
        public CV_uoff32_t offsetBase => value->offsetBase;

        /// <inheritdoc cref="ARMSWITCHTABLE.sectBase"/>
        public short sectBase => value->sectBase;

        /// <inheritdoc cref="ARMSWITCHTABLE.switchType"/>
        public short switchType => value->switchType;

        /// <inheritdoc cref="ARMSWITCHTABLE.offsetBranch"/>
        public CV_uoff32_t offsetBranch => value->offsetBranch;

        /// <inheritdoc cref="ARMSWITCHTABLE.offsetTable"/>
        public CV_uoff32_t offsetTable => value->offsetTable;

        /// <inheritdoc cref="ARMSWITCHTABLE.sectBranch"/>
        public short sectBranch => value->sectBranch;

        /// <inheritdoc cref="ARMSWITCHTABLE.sectTable"/>
        public short sectTable => value->sectTable;

        /// <inheritdoc cref="ARMSWITCHTABLE.cEntries"/>
        public int cEntries => value->cEntries;

        internal const int StructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(uint)   + //offsetBase
            sizeof(short)  + //sectBase
            sizeof(short)  + //switchType
            sizeof(uint)   + //offsetBranch
            sizeof(uint)   + //offsetTable
            sizeof(short)  + //sectBranch
            sizeof(short)  + //sectTable
            sizeof(int);     //cEntries

        internal ArmSwitchTable(ARMSWITCHTABLE* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.ARMSWITCHTABLE, this, ViewKind.ArmSwitchTable, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(reclen), reclen);
            s.WriteField(nameof(rectyp), rectyp, sizeof(ushort));
            s.WriteField(nameof(offsetBase), offsetBase);
            s.WriteField(nameof(sectBase), sectBase);
            s.WriteField(nameof(switchType), switchType);
            s.WriteField(nameof(offsetBranch), offsetBranch);
            s.WriteField(nameof(offsetTable), offsetTable);
            s.WriteField(nameof(sectBranch), sectBranch);
            s.WriteField(nameof(sectTable), sectTable);
            s.WriteField(nameof(cEntries), cEntries);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }
    }
}
