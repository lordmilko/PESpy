using System;
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
        private const int reclenOffset = 0;
        private const int rectypOffset = 2;
        private const int offsetBaseOffset = 4;
        private const int sectBaseOffset = 8;
        private const int switchTypeOffset = 10;
        private const int offsetBranchOffset = 12;
        private const int offsetTableOffset = 16;
        private const int sectBranchOffset = 20;
        private const int sectTableOffset = 22;
        private const int cEntriesOffset = 24;

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

        int IViewable.NumChildren => 10;

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
                    structWriter.WriteField(nameof(offsetBase), offsetBaseOffset, offsetBase);
                    break;

                case 3:
                    structWriter.WriteField(nameof(sectBase), sectBaseOffset, sectBase);
                    break;

                case 4:
                    structWriter.WriteField(nameof(switchType), switchTypeOffset, switchType);
                    break;

                case 5:
                    structWriter.WriteField(nameof(offsetBranch), offsetBranchOffset, offsetBranch);
                    break;

                case 6:
                    structWriter.WriteField(nameof(offsetTable), offsetTableOffset, offsetTable);
                    break;

                case 7:
                    structWriter.WriteField(nameof(sectBranch), sectBranchOffset, sectBranch);
                    break;

                case 8:
                    structWriter.WriteField(nameof(sectTable), sectTableOffset, sectTable);
                    break;

                case 9:
                    structWriter.WriteField(nameof(cEntries), cEntriesOffset, cEntries);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
