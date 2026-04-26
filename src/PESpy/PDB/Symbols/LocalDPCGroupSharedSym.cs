using System;
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
        private const int reclenOffset = 0;
        private const int rectypOffset = 2;
        private const int typindOffset = 4;
        private const int flagsOffset = 8;
        private const int dataslotOffset = 8;
        private const int dataoffOffset = 10;
        private const int nameOffset = 12;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly LOCALDPCGROUPSHAREDSYM* value;

        public static implicit operator SymType(LocalDPCGroupSharedSym value) => new SymType((SYMTYPE*) value.value);

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

        public SymString GetName(ICodeViewAccessor? codeViewAccessor) => SymType.ReadString(value, value->name, codeViewAccessor);

        public SymType Parent => GetParent(null);

        public SymType GetParent(ICodeViewModuleAccessor? codeViewModuleAccessor) => SymType.GetParent((SYMTYPE*) value, codeViewModuleAccessor);

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int)    + //typind
            2              + //flags
            sizeof(short)  + //dataslot
            sizeof(short);   //dataoff

        private int BytesUsed() => FixedStructSize + name.Length + 1;

        internal LocalDPCGroupSharedSym(LOCALDPCGROUPSHAREDSYM* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(this, ViewKind.LocalDPCGroupSharedSym, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

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
                    structWriter.WriteField(nameof(typind), typindOffset, value->typind);
                    break;

                case 3:
                    structWriter.WriteField(nameof(flags), flagsOffset, flags);
                    break;

                case 4:
                    structWriter.WriteField(nameof(dataslot), dataslotOffset, dataslot);
                    break;

                case 5:
                    structWriter.WriteField(nameof(dataoff), dataoffOffset, dataoff);
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
