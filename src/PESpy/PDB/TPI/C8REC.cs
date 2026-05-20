using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    //Used in PDB1
    public struct C8REC
    {
        public ushort hash;
        public TYPTYPE type;
    }

    //Managed representation of C8REC
    public unsafe struct C8Rec : IViewable
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly C8REC* value;

        public ushort hash => value->hash;

        public TypType type => &value->type;

        internal int StructSize => type.len + 4; //len + sizeof(hash) + sizeof(len)

        public C8Rec(C8REC* value)
        {
            this.value = value;
        }

        public static implicit operator C8Rec(C8REC* value) => new C8Rec(value);

        public override string ToString()
        {
            TypType type = &value->type;

            return type.ToString();
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //Write globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(this, ViewKind.C8REC, StructSize);

        int IViewable.NumChildren() => 2;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(hash), 0, hash);
                    break;

                case 1:
                    structWriter.WriteField(nameof(type), 2, type);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
