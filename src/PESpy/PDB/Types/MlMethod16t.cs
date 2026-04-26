using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="mlMethod_16t"/> structure.
    /// </summary>
    public readonly unsafe struct MlMethod16t : IViewable
    {
        private const int attrOffset = 0;
        private const int indexOffset = 2;
        private const int vbaseoffOffset = 4;

        //[DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly mlMethod_16t* value;

        /// <inheritdoc cref="mlMethod_16t.attr"/>
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

        internal const int FixedStructSize =
            sizeof(short) + //attr
            sizeof(short); //index

        internal int StructSize =>
            FixedStructSize + (HasIntro() ? sizeof(int) : 0);

        internal MlMethod16t(mlMethod_16t* value)
        {
            this.value = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool HasIntro() => attr.mprop == CV_methodprop_e.CV_MTintro || attr.mprop == CV_methodprop_e.CV_MTpureintro;

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(this, ViewKind.MlMethod16t, StructSize);

        int IViewable.NumChildren() => 2 + (HasIntro() ? 1 : 0);

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(attr), attrOffset, attr);
                    break;

                case 1:
                    structWriter.WriteField(nameof(index), indexOffset, value->index);
                    break;

                case 2:
                    if (HasIntro())
                        structWriter.WriteField(nameof(vbaseoff), vbaseoffOffset, vbaseoff);
                    else
                        throw new IndexOutOfRangeException();

                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
