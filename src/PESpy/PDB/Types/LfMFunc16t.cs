using System;
using System.Diagnostics;
using ClrDebug.DIA;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfMFunc_16t"/> structure.
    /// </summary>
    public readonly unsafe struct LfMFunc16t : IViewable
    {
        private const int typlenOffset = 0;
        private const int leafOffset = 2;
        private const int rvtypeOffset = 4;
        private const int classtypeOffset = 6;
        private const int thistypeOffset = 8;
        private const int calltypeOffset = 10;
        private const int funcattrOffset = 11;
        private const int parmcountOffset = 12;
        private const int arglistOffset = 14;
        private const int thisadjustOffset = 16;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfMFunc_16t* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public TypOrEnumType rvtype => new TypOrEnumType((byte*) value, value->rvtype);

        public TypOrEnumType classtype => new TypOrEnumType((byte*) value, value->classtype);

        public TypOrEnumType thistype => new TypOrEnumType((byte*) value, value->thistype);

        public CV_call_e calltype => (CV_call_e) value->calltype;

        public CV_funcattr_t funcattr => value->funcattr;

        public short parmcount => value->parmcount;

        public TypOrEnumType arglist => new TypOrEnumType((byte*) value, value->arglist);

        public int thisadjust => value->thisadjust;

        internal const int StructSize =
            sizeof(ushort) + //leaf
            sizeof(short)  + //rvtype
            sizeof(short)  + //classtype
            sizeof(short)  + //thistype
            sizeof(byte)   + //calltype
            1              + //funcattr
            sizeof(short)  + //parmcount
            sizeof(short)  + //arglist
            sizeof(int);     //thisadjust

        internal LfMFunc16t(lfMFunc_16t* value)
        {
            this.value = value;
        }

        public static implicit operator LfEasy(LfMFunc16t easy) => new LfEasy((lfEasy*) (byte*) easy.value);

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.lfMFunc_16t, this, ViewKind.LfMFunc16t, typlen + sizeof(short));

        int IViewable.NumChildren() => 10;

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
                    structWriter.WriteField(nameof(classtype), classtypeOffset, value->classtype);
                    break;

                case 4:
                    structWriter.WriteField(nameof(thistype), thistypeOffset, value->thistype);
                    break;

                case 5:
                    structWriter.WriteField(nameof(calltype), calltypeOffset, calltype, sizeof(byte));
                    break;

                case 6:
                    structWriter.WriteField(nameof(funcattr), funcattrOffset, funcattr);
                    break;

                case 7:
                    structWriter.WriteField(nameof(parmcount), parmcountOffset, parmcount);
                    break;

                case 8:
                    structWriter.WriteField(nameof(arglist), arglistOffset, value->arglist);
                    break;

                case 9:
                    structWriter.WriteField(nameof(thisadjust), thisadjustOffset, thisadjust);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
