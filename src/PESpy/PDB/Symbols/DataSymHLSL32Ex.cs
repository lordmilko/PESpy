using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="DATASYMHLSL32_EX"/> structure.
    /// </summary>
    public readonly unsafe struct DataSymHLSL32Ex : IViewable
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly DATASYMHLSL32_EX* value;

        /// <inheritdoc cref="DATASYMHLSL32_EX.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="DATASYMHLSL32_EX.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <summary>
        /// Type index
        /// </summary>
        /// <inheritdoc cref="DATASYMHLSL32_EX.typind"/>
        public TypOrEnumType typind => new TypOrEnumType((byte*) value, value->typind);

        /// <inheritdoc cref="DATASYMHLSL32_EX.regID"/>
        public int regID => value->regID;

        /// <inheritdoc cref="DATASYMHLSL32_EX.dataoff"/>
        public int dataoff => value->dataoff;

        /// <inheritdoc cref="DATASYMHLSL32_EX.bindSpace"/>
        public int bindSpace => value->bindSpace;

        /// <inheritdoc cref="DATASYMHLSL32_EX.bindSlot"/>
        public int bindSlot => value->bindSlot;

        /// <inheritdoc cref="DATASYMHLSL32_EX.regType"/>
        public short regType => value->regType;

        /// <inheritdoc cref="DATASYMHLSL32_EX.name"/>
        public SymString name => SymType.ReadString(value, value->name);

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int)    + //typind
            sizeof(int)    + //regID
            sizeof(int)    + //dataoff
            sizeof(int)    + //bindSpace
            sizeof(int)    + //bindSlot
            sizeof(short);   //regType

        internal DataSymHLSL32Ex(DATASYMHLSL32_EX* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.DATASYMHLSL32_EX, this, ViewKind.DataSymHLSL32Ex, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(reclen), reclen);
            s.WriteField(nameof(rectyp), rectyp, sizeof(ushort));
            s.WriteField(nameof(typind), typind);
            s.WriteField(nameof(regID), regID);
            s.WriteField(nameof(dataoff), dataoff);
            s.WriteField(nameof(bindSpace), bindSpace);
            s.WriteField(nameof(bindSlot), bindSlot);
            s.WriteField(nameof(regType), regType);
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
