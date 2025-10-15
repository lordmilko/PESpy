using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="LOCALDPCGROUPSHAREDSYM"/> structure.
    /// </summary>
    public readonly unsafe struct LocalDPCGroupSharedSym : IViewable
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly LOCALDPCGROUPSHAREDSYM* value;

        /// <inheritdoc cref="LOCALDPCGROUPSHAREDSYM.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="LOCALDPCGROUPSHAREDSYM.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="LOCALDPCGROUPSHAREDSYM.typind"/>
        public TypOrEnumType typind => new TypOrEnumType((byte*) value, value->typind);

        /// <inheritdoc cref="LOCALDPCGROUPSHAREDSYM.flags"/>
        public CV_LVARFLAGS flags => value->flags;

        /// <inheritdoc cref="LOCALDPCGROUPSHAREDSYM.dataslot"/>
        public short dataslot => value->dataslot;

        /// <inheritdoc cref="LOCALDPCGROUPSHAREDSYM.dataoff"/>
        public short dataoff => value->dataoff;

        /// <inheritdoc cref="LOCALDPCGROUPSHAREDSYM.name"/>
        public SymString name => SymType.ReadString(value, value->name);

        #region PESpy

        internal SymString GetName(ISymbolAccessor? symbolAccessor) => SymType.ReadString(value, value->name, symbolAccessor);

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int)    + //typind
            2              + //flags
            sizeof(short)  + //dataslot
            sizeof(short);   //dataoff

        internal LocalDPCGroupSharedSym(LOCALDPCGROUPSHAREDSYM* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.LOCALDPCGROUPSHAREDSYM, this, ViewKind.LocalDPCGroupSharedSym, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(reclen), reclen);
            s.WriteField(nameof(rectyp), rectyp, sizeof(ushort));
            s.WriteField(nameof(typind), typind);
            s.WriteField(nameof(flags), flags);
            s.WriteField(nameof(dataslot), dataslot);
            s.WriteField(nameof(dataoff), dataoff);
            s.WriteSymStringField(nameof(name), SymType.ReadString(value, value->name, viewWriter.GetSymbolAccessor()));

            s.Align(4);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}
