using System;
using System.Diagnostics;
using ClrDebug.DIA;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfProc_16t"/> structure.
    /// </summary>
    public readonly unsafe struct LfProc16t : IViewable
    {
        private const int typlenOffset = 0;
        private const int leafOffset = 2;
        private const int rvtypeOffset = 4;
        private const int calltypeOffset = 6;
        private const int funcattrOffset = 7;
        private const int parmcountOffset = 8;
        private const int arglistOffset = 10;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfProc_16t* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public TypOrEnumType rvtype => new TypOrEnumType((byte*) value, value->rvtype);

        public CV_call_e calltype => (CV_call_e) value->calltype;

        public CV_funcattr_t funcattr => value->funcattr;

        public short parmcount => value->parmcount;

        public TypOrEnumType arglist => new TypOrEnumType((byte*) value, value->arglist);

        internal const int StructSize =
            sizeof(ushort) + //leaf
            sizeof(short)  + //rvtype
            sizeof(byte)   + //calltype
            1              + //funcattr
            sizeof(short)  + //parmcount
            sizeof(short);   //arglist

        internal LfProc16t(lfProc_16t* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.lfProc_16t, this, ViewKind.LfProc16t, typlen + sizeof(short));

        int IViewable.NumChildren() => 7;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(typlen), typlenOffset, typlen);
                    break;

                case 1:
                    structWriter.WriteField(nameof(leaf), leafOffset, leaf, sizeof(ushort));
                    break;

                case 2:
                    structWriter.WriteField(nameof(rvtype), rvtypeOffset, value->rvtype);
                    break;

                case 3:
                    structWriter.WriteField(nameof(calltype), calltypeOffset, calltype, sizeof(byte));
                    break;

                case 4:
                    structWriter.WriteField(nameof(funcattr), funcattrOffset, funcattr);
                    break;

                case 5:
                    structWriter.WriteField(nameof(parmcount), parmcountOffset, parmcount);
                    break;

                case 6:
                    structWriter.WriteField(nameof(arglist), arglistOffset, value->arglist);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
