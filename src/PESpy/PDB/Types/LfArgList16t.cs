using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfArgList_16t"/> structure.
    /// </summary>
    public readonly unsafe struct LfArgList16t : IViewable
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfArgList_16t* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public short count => value->count;

        public TypOrEnumType[] arg
        {
            get
            {
                var raw = new Span<CV_typ16_t>(value->arg, count);
                var arr = new TypOrEnumType[raw.Length];

                for (var i = 0; i < arr.Length; i++)
                    arr[i] = new TypOrEnumType((byte*) value, raw[i]);

                return arr;
            }
        }

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            sizeof(short);   //count

        internal LfArgList16t(lfArgList_16t* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.lfArgList_16t, this, ViewKind.LfArgList16t, typlen + sizeof(short));

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(leaf), leaf, sizeof(ushort));
            s.WriteField(nameof(count), count);

            var arg = new NativeSpan<CV_typ16_t>(value->arg, count);

            s.WriteField(nameof(arg), arg);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }
    }
}
