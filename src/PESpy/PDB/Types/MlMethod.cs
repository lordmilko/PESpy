using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="mlMethod"/> structure.
    /// </summary>
    public readonly unsafe struct MlMethod : IViewable
    {
        private const int attrOffset = 0;
        private const int pad0Offset = 2;
        private const int indexOffset = 4;
        private const int vbaseoffOffset = 8;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly mlMethod* value;

        /// <inheritdoc cref="mlMethod.attr"/>
        public CV_fldattr_t attr => value->attr;

        public short pad0 => value->pad0;

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
            sizeof(short) + //pad0
            sizeof(int); //index

        internal int StructSize =>
            FixedStructSize + (HasIntro() ? sizeof(int) : 0);

        internal MlMethod(mlMethod* value)
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
            writer.NewUnmanagedStruct(Strings.mlMethod, this, ViewKind.MlMethod, StructSize);

        int IViewable.NumChildren() => 3 + (HasIntro() ? 1 : 0);

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(attr), attrOffset, attr);
                    break;

                case 1:
                    structWriter.WriteField(nameof(pad0), pad0Offset, pad0);
                    break;

                case 2:
                    structWriter.WriteField(nameof(index), indexOffset, index);
                    break;

                case 3:
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
