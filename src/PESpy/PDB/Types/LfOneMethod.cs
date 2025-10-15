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

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(leaf), leaf, sizeof(ushort));
            s.WriteField(nameof(attr), attr);
            s.WriteField(nameof(index), index);

            if (HasIntro())
                s.WriteField(nameof(vbaseoff), vbaseoff);

            s.WriteSymStringField(nameof(name), GetName(viewWriter.GetSymbolAccessor()));

            //Do not align; the parent will apply padding

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}
