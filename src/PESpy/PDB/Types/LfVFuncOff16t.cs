using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfVFuncOff_16t"/> structure.
    /// </summary>
    public readonly unsafe struct LfVFuncOff16t : IViewable
    {
        private const int leafOffset = 0;
        private const int typeOffset = 2;
        private const int offsetOffset = 4;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfVFuncOff_16t* value;

        //This type is only ever referenced from other records and so does not have a TYPTYPE.len

        public LEAF_ENUM_e leaf => value->leaf;

        public TypOrEnumType type => new TypOrEnumType((byte*) value, value->type);

        public CV_off32_t offset => value->offset;

        internal const int StructSize =
            sizeof(ushort) + //leaf
            sizeof(short)  + //type
            sizeof(int);     //offset

        internal LfVFuncOff16t(lfVFuncOff_16t* value)
        {
            this.value = value;
        }

        public static implicit operator LfEasy(LfVFuncOff16t easy) => new LfEasy((lfEasy*) (byte*) easy.value);

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(this, ViewKind.LfVFuncOff16t, StructSize);

        int IViewable.NumChildren() => 3;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(leaf), leafOffset, leaf, sizeof(ushort));
                    break;

                case 1:
                    structWriter.WriteField(nameof(type), typeOffset, value->type);
                    break;

                case 2:
                    structWriter.WriteField(nameof(offset), offsetOffset, offset);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
