using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="EXPORTSYM"/> structure.
    /// </summary>
    public readonly unsafe struct ExportSym : IViewable
    {
        private const int reclenOffset = 0;
        private const int rectypOffset = 2;
        private const int ordinalOffset = 4;
        private const int dataOffset = 6;
        private const int nameOffset = 8;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly EXPORTSYM* value;

        public static implicit operator SymType(ExportSym value) => new SymType((SYMTYPE*) value.value);

        /// <inheritdoc cref="EXPORTSYM.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="EXPORTSYM.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="EXPORTSYM.ordinal"/>
        public short ordinal => value->ordinal;

        /// <inheritdoc cref="EXPORTSYM.fConstant"/>
        public bool fConstant => value->fConstant;

        /// <inheritdoc cref="EXPORTSYM.fData"/>
        public bool fData => value->fData;

        /// <inheritdoc cref="EXPORTSYM.fPrivate"/>
        public bool fPrivate => value->fPrivate;

        /// <inheritdoc cref="EXPORTSYM.fNoName"/>
        public bool fNoName => value->fNoName;

        /// <inheritdoc cref="EXPORTSYM.fOrdinal"/>
        public bool fOrdinal => value->fOrdinal;

        /// <inheritdoc cref="EXPORTSYM.fForwarder"/>
        public bool fForwarder => value->fForwarder;

        /// <inheritdoc cref="EXPORTSYM.reserved"/>
        public short reserved => value->reserved;

        /// <inheritdoc cref="EXPORTSYM.name"/>
        public SymString name => SymType.ReadString(value, value->name);

        #region PESpy

        public SymString GetName(ICodeViewAccessor? codeViewAccessor) => SymType.ReadString(value, value->name, codeViewAccessor);

        public SymType Parent => GetParent(null);

        public SymType GetParent(ICodeViewModuleAccessor? codeViewModuleAccessor) => SymType.GetParent((SYMTYPE*) value, codeViewModuleAccessor);

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(short)  + //ordinal
            sizeof(short);   //data

        private int BytesUsed() => FixedStructSize + name.Length + 1;

        internal ExportSym(EXPORTSYM* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(this, ViewKind.ExportSym, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

        int IViewable.NumChildren() => StructWriter.GetNumChildrenAlign4(11, BytesUsed());

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
                    structWriter.WriteField(nameof(ordinal), ordinalOffset, ordinal);
                    break;

                #region BitField

                case 3:
                    structWriter.WriteBitField(nameof(fConstant), dataOffset, fConstant, sizeof(short), 1);
                    break;

                case 4:
                    structWriter.WriteBitField(nameof(fData), dataOffset, fData, sizeof(short), 1);
                    break;

                case 5:
                    structWriter.WriteBitField(nameof(fPrivate), dataOffset, fPrivate, sizeof(short), 1);
                    break;

                case 6:
                    structWriter.WriteBitField(nameof(fNoName), dataOffset, fNoName, sizeof(short), 1);
                    break;

                case 7:
                    structWriter.WriteBitField(nameof(fOrdinal), dataOffset, fOrdinal, sizeof(short), 1);
                    break;

                case 8:
                    structWriter.WriteBitField(nameof(fForwarder), dataOffset, fForwarder, sizeof(short), 1);
                    break;

                case 9:
                    structWriter.WriteBitField(nameof(reserved), dataOffset, reserved, sizeof(short), 10);
                    break;

                #endregion

                case 10:
                    structWriter.WriteSymStringField(nameof(name), nameOffset, SymType.ReadString(value, value->name, structWriter.GetSymbolAccessor()));
                    break;

                case 11:
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
