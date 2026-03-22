using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfVFuncOff"/> structure.
    /// </summary>
    public readonly unsafe struct LfVFuncOff : IViewable
    {
        private const int leafOffset = 0;
        private const int pad0Offset = 2;
        private const int typeOffset = 4;
        private const int offsetOffset = 8;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfVFuncOff* value;

        //This type is only ever referenced from other records and so does not have a TYPTYPE.len

        public LEAF_ENUM_e leaf => value->leaf;

        public short pad0 => value->pad0;

        public TypOrEnumType type => new TypOrEnumType((byte*) value, value->type);

        public CV_off32_t offset => value->offset;

        internal const int StructSize =
            sizeof(ushort) + //leaf
            sizeof(short)  + //pad0
            sizeof(int)    + //type
            sizeof(int);     //offset

        internal LfVFuncOff(lfVFuncOff* value)
        {
            this.value = value;
        }

        public static implicit operator LfEasy(LfVFuncOff easy) => new LfEasy((lfEasy*) (byte*) easy.value);

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.lfVFuncOff, this, ViewKind.LfVFuncOff, StructSize);

        int IViewable.NumChildren() => 4;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(leaf), leafOffset, leaf, sizeof(ushort));
                    break;

                case 1:
                    structWriter.WriteField(nameof(pad0), pad0Offset, pad0);
                    break;

                case 2:
                    structWriter.WriteField(nameof(type), typeOffset, value->type);
                    break;

                case 3:
                    structWriter.WriteField(nameof(offset), offsetOffset, offset);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
