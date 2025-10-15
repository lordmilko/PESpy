using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="DATASYMHLSL32"/> structure.
    /// </summary>
    public readonly unsafe struct DataSymHLSL32 : IViewable
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly DATASYMHLSL32* value;

        /// <inheritdoc cref="DATASYMHLSL32.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="DATASYMHLSL32.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="DATASYMHLSL32.typind"/>
        public TypOrEnumType typind => new TypOrEnumType((byte*) value, value->typind);

        /// <inheritdoc cref="DATASYMHLSL32.dataslot"/>
        public int dataslot => value->dataslot;

        /// <inheritdoc cref="DATASYMHLSL32.dataoff"/>
        public int dataoff => value->dataoff;

        /// <inheritdoc cref="DATASYMHLSL32.texslot"/>
        public int texslot => value->texslot;

        /// <inheritdoc cref="DATASYMHLSL32.sampslot"/>
        public int sampslot => value->sampslot;

        /// <inheritdoc cref="DATASYMHLSL32.uavslot"/>
        public int uavslot => value->uavslot;

        /// <inheritdoc cref="DATASYMHLSL32.regType"/>
        public short regType => value->regType;

        /// <inheritdoc cref="DATASYMHLSL32.name"/>
        public SymString name => SymType.ReadString(value, value->name);

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int)    + //typind
            sizeof(int)    + //dataslot
            sizeof(int)    + //dataoff
            sizeof(int)    + //texslot
            sizeof(int)    + //sampslot
            sizeof(int)    + //uavslot
            sizeof(short);   //regType

        internal DataSymHLSL32(DATASYMHLSL32* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.DATASYMHLSL32, this, ViewKind.DataSymHLSL32, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(reclen), reclen);
            s.WriteField(nameof(rectyp), rectyp, sizeof(ushort));
            s.WriteField(nameof(typind), typind);
            s.WriteField(nameof(dataslot), dataslot);
            s.WriteField(nameof(dataoff), dataoff);
            s.WriteField(nameof(texslot), texslot);
            s.WriteField(nameof(sampslot), sampslot);
            s.WriteField(nameof(uavslot), uavslot);
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
