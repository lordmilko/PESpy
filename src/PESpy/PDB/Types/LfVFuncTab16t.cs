using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfVFuncTab_16t"/> structure.
    /// </summary>
    public readonly unsafe struct LfVFuncTab16t : IViewable
    {
        private const int leafOffset = 0;
        private const int typeOffset = 2;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfVFuncTab_16t* value;

        //This type is only ever referenced from other records and so does not have a TYPTYPE.len

        public LEAF_ENUM_e leaf => value->leaf;

        public TypOrEnumType type => new TypOrEnumType((byte*) value, value->type);

        internal const int StructSize =
            sizeof(ushort) + //leaf
            sizeof(short);   //type

        internal LfVFuncTab16t(lfVFuncTab_16t* value)
        {
            this.value = value;
        }

        public static implicit operator LfEasy(LfVFuncTab16t easy) => new LfEasy((lfEasy*) (byte*) easy.value);

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(this, ViewKind.LfVFuncTab16t, StructSize);

        int IViewable.NumChildren() => 2;

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

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
