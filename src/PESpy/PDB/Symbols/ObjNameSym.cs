using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="OBJNAMESYM"/> structure.
    /// </summary>
    public readonly unsafe struct ObjNameSym : IViewable
    {
        private const int reclenOffset = 0;
        private const int rectypOffset = 2;
        private const int signatureOffset = 4;
        private const int nameOffset = 8;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly OBJNAMESYM* value;

        public static implicit operator SymType(ObjNameSym value) => new SymType((SYMTYPE*) value.value);

        /// <inheritdoc cref="OBJNAMESYM.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="OBJNAMESYM.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="OBJNAMESYM.signature"/>
        public int signature => value->signature;

        /// <inheritdoc cref="OBJNAMESYM.name"/>
        public SymString name => SymType.ReadString(value, value->name);

        #region PESpy

        public SymString GetName(ICodeViewAccessor? codeViewAccessor) => SymType.ReadString(value, value->name, codeViewAccessor);

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int);     //signature

        private int BytesUsed() => FixedStructSize + name.Length + 1;

        internal ObjNameSym(OBJNAMESYM* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.OBJNAMESYM, this, ViewKind.ObjNameSym, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

        int IViewable.NumChildren() => StructWriter.GetNumChildrenAlign4(4, BytesUsed());

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
                    structWriter.WriteField(nameof(signature), signatureOffset, signature);
                    break;

                case 3:
                    structWriter.WriteSymStringField(nameof(name), nameOffset, SymType.ReadString(value, value->name, structWriter.GetSymbolAccessor()));
                    break;

                case 4:
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
