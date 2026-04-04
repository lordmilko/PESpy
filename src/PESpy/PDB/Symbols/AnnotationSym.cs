using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="ANNOTATIONSYM"/> structure.
    /// </summary>
    public readonly unsafe struct AnnotationSym : IViewable
    {
        private const int reclenOffset = 0;
        private const int rectypOffset = 2;
        private const int offOffset = 4;
        private const int segOffset = 8;
        private const int cszOffset = 10;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly ANNOTATIONSYM* value;

        public static implicit operator SymType(AnnotationSym value) => new SymType((SYMTYPE*) value.value);

        /// <inheritdoc cref="ANNOTATIONSYM.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="ANNOTATIONSYM.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="ANNOTATIONSYM.off"/>
        public CV_uoff32_t off => value->off;

        /// <inheritdoc cref="ANNOTATIONSYM.seg"/>
        public ISECT seg => value->seg;

        /// <inheritdoc cref="ANNOTATIONSYM.csz"/>
        public short csz => value->csz;

        /// <inheritdoc cref="ANNOTATIONSYM.rgsz"/>
        public AnsiString[] rgsz
        {
            get
            {
                //It's unfortunate but we don't currently have anywhere to store this array. This type
                //is just meant to be a lightweight wrapper aound a SYMTYPE pointer. Potentially
                //in the future we can have some sort of "storage" mechanism on the PDBFile that we can
                //lookup (that is associated with our address range) and cache this object there
                var items = new AnsiString[csz];

                var ptr = value->rgsz;

                for (var i = 0; i < items.Length; i++)
                {
                    var str = new AnsiString(ptr);
                    items[i] = str;
                    ptr += str.Length + 1;
                }

                return items;
            }
        }

        #region PESpy

        public int? RelativeVirtualAddress => SymType.GetRelativeVirtualAddress(value, seg, off);

        public SymType Parent => GetParent(null);

        public SymType GetParent(ICodeViewModuleAccessor? codeViewModuleAccessor) => SymType.GetParent((SYMTYPE*) value, codeViewModuleAccessor);

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(uint)   + //off
            sizeof(short)  + //seg
            sizeof(short);   //csz

        internal AnnotationSym(ANNOTATIONSYM* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.ANNOTATIONSYM, this, ViewKind.AnnotationSym, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

        int IViewable.NumChildren() => 5;

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
                    structWriter.WriteField(nameof(off), offOffset, off);
                    break;

                case 3:
                    structWriter.WriteField(nameof(seg), segOffset, seg);
                    break;

                case 4:
                    structWriter.WriteField(nameof(csz), cszOffset, csz);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
