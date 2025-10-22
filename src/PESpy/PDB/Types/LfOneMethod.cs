using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfOneMethod"/> structure.
    /// </summary>
    public readonly unsafe struct LfOneMethod : IViewable
    {
        private const int leafOffset = 0;
        private const int attrOffset = 2;
        private const int indexOffset = 4;
        private const int vbaseoffOffset = 8;
        private const int nameWithVBaseOffset = 12;
        private const int nameWithoutVBaseOffset = 8;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfOneMethod* value;

        //This type is only ever referenced from other records and so does not have a TYPTYPE.len

        public LEAF_ENUM_e leaf => value->leaf;

        public CV_fldattr_t attr => value->attr;

        public TypOrEnumType index => new TypOrEnumType((byte*) value, value->index);

        public int vbaseoff
        {
            get
            {
                if (HasIntro())
                    return *value->vbaseoff;

                return 0;
            }
        }

        public SymString name => GetName(null);

        #region PESpy

        internal SymString GetName(ISymbolAccessor? symbolAccessor) =>
            TypType.ReadString(((byte*) value->vbaseoff) + (HasIntro() ? sizeof(int) : 0), symbolAccessor);

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            2              + //attr
            sizeof(int);     //index

        internal int StructSize => GetStructSize(null);

        internal int GetStructSize(ISymbolAccessor? symbolAccessor)
        {
            var strOff = 0;

            if (HasIntro())
                strOff = sizeof(int);

            var str = TypType.ReadString(((byte*) value->vbaseoff) + strOff);

            return FixedStructSize + strOff + str.Length + 1;
        }

        //NT 4 doesn't show to consider pureintro, but microsoft-pdb does
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool HasIntro() => attr.mprop == CV_methodprop_e.CV_MTintro || attr.mprop == CV_methodprop_e.CV_MTpureintro;

        internal LfOneMethod(lfOneMethod* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.lfOneMethod, this, ViewKind.LfOneMethod, GetStructSize(writer.GetSymbolAccessor()));

        int IViewable.NumChildren() => HasIntro() ? 5 : 4;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(leaf), leafOffset, leaf, sizeof(ushort));
                    break;

                case 1:
                    structWriter.WriteField(nameof(attr), attrOffset, attr);
                    break;

                case 2:
                    structWriter.WriteField(nameof(index), indexOffset, index);
                    break;

                case 3:
                    if (HasIntro())
                        structWriter.WriteField(nameof(vbaseoff), vbaseoffOffset, vbaseoff);
                    else
                        structWriter.WriteSymStringField(nameof(name), nameWithoutVBaseOffset, GetName(structWriter.GetSymbolAccessor()));

                    break;

                case 4:
                    if (HasIntro())
                        structWriter.WriteSymStringField(nameof(name), nameWithVBaseOffset, GetName(structWriter.GetSymbolAccessor()));
                    else
                        throw new IndexOutOfRangeException();

                    break;

                //Do not align; the parent will apply padding

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
