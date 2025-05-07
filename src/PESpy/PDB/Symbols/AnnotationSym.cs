using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="ANNOTATIONSYM"/> structure.
    /// </summary>
    public readonly unsafe struct AnnotationSym
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly ANNOTATIONSYM* value;

        public ushort reclen => value->reclen;

        public SYM_ENUM_e rectyp => value->rectyp;

        public CV_uoff32_t off => value->off;

        public short seg => value->seg;

        public short csz => value->csz;

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

        public int RelativeVirtualAddress => SymType.GetRelativeVirtualAddress(value, seg, off);

        #endregion

        internal AnnotationSym(ANNOTATIONSYM* value)
        {
            this.value = value;
        }
    }
}

