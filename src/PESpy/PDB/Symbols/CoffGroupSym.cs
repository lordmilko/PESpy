using System;
using System.Diagnostics;
using ClrDebug;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="COFFGROUPSYM"/> structure.
    /// </summary>
    public readonly unsafe struct CoffGroupSym : IViewable
    {
        private const int reclenOffset = 0;
        private const int rectypOffset = 2;
        private const int cbOffset = 4;
        private const int characteristicsOffset = 8;
        private const int offOffset = 12;
        private const int segOffset = 16;
        private const int nameOffset = 18;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly COFFGROUPSYM* value;

        public static implicit operator SymType(CoffGroupSym value) => new SymType((SYMTYPE*) value.value);

        /// <inheritdoc cref="COFFGROUPSYM.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="COFFGROUPSYM.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="COFFGROUPSYM.cb"/>
        public int cb => value->cb;

        /// <inheritdoc cref="COFFGROUPSYM.characteristics"/>
        public IMAGE_SCN characteristics => value->characteristics;

        /// <inheritdoc cref="COFFGROUPSYM.off"/>
        public CV_uoff32_t off => value->off;

        /// <inheritdoc cref="COFFGROUPSYM.seg"/>
        public ushort seg => value->seg;

        /// <inheritdoc cref="COFFGROUPSYM.name"/>
        public SymString name => SymType.ReadString(value, value->name);

        #region PESpy

        public int? RelativeVirtualAddress => SymType.GetRelativeVirtualAddress(value, seg, off);

        internal SymString GetName(ICodeViewAccessor? codeViewAccessor) => SymType.ReadString(value, value->name, codeViewAccessor);

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int)    + //cb
            sizeof(int)    + //characteristics
            sizeof(uint)   + //off
            sizeof(short);   //seg

        private int BytesUsed() => FixedStructSize + name.Length + 1;

        internal CoffGroupSym(COFFGROUPSYM* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.COFFGROUPSYM, this, ViewKind.CoffGroupSym, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

        int IViewable.NumChildren() => StructWriter.GetNumChildrenAlign4(7, BytesUsed());

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
                    structWriter.WriteField(nameof(cb), cbOffset, cb);
                    break;

                case 3:
                    structWriter.WriteField(nameof(characteristics), characteristicsOffset, characteristics, sizeof(int));
                    break;

                case 4:
                    structWriter.WriteField(nameof(off), offOffset, off);
                    break;

                case 5:
                    structWriter.WriteField(nameof(seg), segOffset, seg);
                    break;

                case 6:
                    structWriter.WriteSymStringField(nameof(name), nameOffset, SymType.ReadString(value, value->name, structWriter.GetSymbolAccessor()));
                    break;

                case 7:
                    //Possible alignment
                    structWriter.AlignOrThrow(BytesUsed());
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
