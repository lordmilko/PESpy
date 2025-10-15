using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="DATASYMHLSL"/> structure.
    /// </summary>
    public readonly unsafe struct DataSymHLSL : IViewable
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly DATASYMHLSL* value;

        /// <inheritdoc cref="DATASYMHLSL.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="DATASYMHLSL.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="DATASYMHLSL.typind"/>
        public TypOrEnumType typind => new TypOrEnumType((byte*) value, value->typind);

        /// <inheritdoc cref="DATASYMHLSL.regType"/>
        public short regType => value->regType;

        /// <inheritdoc cref="DATASYMHLSL.dataslot"/>
        public short dataslot => value->dataslot;

        /// <inheritdoc cref="DATASYMHLSL.dataoff"/>
        public short dataoff => value->dataoff;

        /// <inheritdoc cref="DATASYMHLSL.texslot"/>
        public short texslot => value->texslot;

        /// <inheritdoc cref="DATASYMHLSL.sampslot"/>
        public short sampslot => value->sampslot;

        /// <inheritdoc cref="DATASYMHLSL.uavslot"/>
        public short uavslot => value->uavslot;

        /// <inheritdoc cref="DATASYMHLSL.name"/>
        public SymString name => SymType.ReadString(value, value->name);

        internal SymString GetName(ISymbolAccessor? symbolAccessor) => SymType.ReadString(value, value->name, symbolAccessor);

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int)    + //typind
            sizeof(short)  + //regType
            sizeof(short)  + //dataslot
            sizeof(short)  + //dataoff
            sizeof(short)  + //texslot
            sizeof(short)  + //sampslot
            sizeof(short);   //uavslot

        internal DataSymHLSL(DATASYMHLSL* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.DATASYMHLSL, this, ViewKind.DataSymHLSL, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(reclen), reclen);
            s.WriteField(nameof(rectyp), rectyp, sizeof(ushort));
            s.WriteField(nameof(typind), typind);
            s.WriteField(nameof(regType), regType);
            s.WriteField(nameof(dataslot), dataslot);
            s.WriteField(nameof(dataoff), dataoff);
            s.WriteField(nameof(texslot), texslot);
            s.WriteField(nameof(sampslot), sampslot);
            s.WriteField(nameof(uavslot), uavslot);
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
