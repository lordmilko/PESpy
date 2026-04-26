using System;
using System.Diagnostics;
using ClrDebug;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="SECTIONSYM"/> structure.
    /// </summary>
    public readonly unsafe struct SectionSym : IViewable
    {
        private const int reclenOffset = 0;
        private const int rectypOffset = 2;
        private const int isecOffset = 4;
        private const int alignOffset = 6;
        private const int bReservedOffset = 7;
        private const int rvaOffset = 8;
        private const int cbOffset = 12;
        private const int characteristicsOffset = 16;
        private const int nameOffset = 20;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly SECTIONSYM* value;

        public static implicit operator SymType(SectionSym value) => new SymType((SYMTYPE*) value.value);

        /// <inheritdoc cref="SECTIONSYM.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="SECTIONSYM.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="SECTIONSYM.isec"/>
        public short isec => value->isec;

        /// <inheritdoc cref="SECTIONSYM.align"/>
        public byte align => value->align;

        /// <inheritdoc cref="SECTIONSYM.bReserved"/>
        public byte bReserved => value->bReserved;

        /// <inheritdoc cref="SECTIONSYM.rva"/>
        public int rva => value->rva;

        /// <inheritdoc cref="SECTIONSYM.cb"/>
        public int cb => value->cb;

        /// <inheritdoc cref="SECTIONSYM.characteristics"/>
        public IMAGE_SCN characteristics => value->characteristics;

        /// <inheritdoc cref="SECTIONSYM.name"/>
        public SymString name => SymType.ReadString(value, value->name);

        #region PESpy

        public SymString GetName(ICodeViewAccessor? codeViewAccessor) => SymType.ReadString(value, value->name, codeViewAccessor);

        public SymType Parent => GetParent(null);

        public SymType GetParent(ICodeViewModuleAccessor? codeViewModuleAccessor) => SymType.GetParent((SYMTYPE*) value, codeViewModuleAccessor);

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(short)  + //isec
            sizeof(byte)   + //align
            sizeof(byte)   + //bReserved
            sizeof(int)    + //rva
            sizeof(int)    + //cb
            sizeof(int);     //characteristics

        private int BytesUsed() => FixedStructSize + name.Length + 1;

        internal SectionSym(SECTIONSYM* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(this, ViewKind.SectionSym, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

        int IViewable.NumChildren() => StructWriter.GetNumChildrenAlign4(9, BytesUsed());

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
                    structWriter.WriteField(nameof(isec), isecOffset, isec);
                    break;

                case 3:
                    structWriter.WriteField(nameof(align), alignOffset, align);
                    break;

                case 4:
                    structWriter.WriteField(nameof(bReserved), bReservedOffset, bReserved);
                    break;

                case 5:
                    structWriter.WriteField(nameof(rva), rvaOffset, rva);
                    break;

                case 6:
                    structWriter.WriteField(nameof(cb), cbOffset, cb);
                    break;

                case 7:
                    structWriter.WriteField(nameof(characteristics), characteristicsOffset, characteristics, sizeof(int));
                    break;

                case 8:
                    structWriter.WriteSymStringField(nameof(name), nameOffset, SymType.ReadString(value, value->name, structWriter.GetSymbolAccessor()));
                    break;

                case 9:
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
