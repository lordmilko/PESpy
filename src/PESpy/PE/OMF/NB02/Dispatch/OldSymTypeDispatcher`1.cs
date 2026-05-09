using System;
using System.Diagnostics;
using static ClrDebug.OMF.OLDSYM;

namespace PESpy
{
    public abstract class OldSymTypeDispatcher<T>
    {
        public T Dispatch(OldSymType value)
        {
            switch (value.rectyp)
            {
                case S_BLOCK:
                    return BlkSymType((BlkSymType) value);

                case S_PROC:
                case S_ENTRY:
                    return ProcSymType((ProcSymType) value);

                case S_END:
                case S_NOOP: //Unknown
                case S_CODSEG: //Unknown
                case S_GLOBAL: //Unknown
                case S_GLOBPROC: //Unknown
                case S_LOCPROC: //Unknown
                case S_CHGMODEL: //Unknown
                case S_PUBLIC: //Unknown
                case S_SEARCH: //Unknown
                case S_CV4CHGMODEL: //Unknown
                case S_COMPILEFLAG: //Unknown
                    return OldSymType(value);

                case S_BPREL:
                    return BPSymType((BPSymType) value);

                case S_LOCAL:
                    return LocSymType((LocSymType) value);

                case S_LABEL:
                    return LabSymType((LabSymType) value);

                case S_WITH:
                    return WithSymType((WithSymType) value);

                case S_REG:
                    return RegSymType((RegSymType) value);

                case S_CONST:
                    return ConSymType((ConSymType) value);

                case S_TYPEDEF:
                    return TypeDefSymType((TypeDefSymType) value);

                case S_THUNK:
                    return ThunkSymType((ThunkSymType) value);

                case S_CV4BLOCK:
                    return CV4BlkSymType((CV4BlkSymType) value);

                case S_CV4WITH:
                    return CV4WithSymType((CV4WithSymType) value);

                case S_CV4LABEL:
                    return CV4LabSymType((CV4LabSymType) value);

                default:
                    Debug.Assert(false);
                    return OldSymType((OldSymType) value);
            }
        }

        protected abstract T OldSymType(OldSymType value);

        protected abstract T BlkSymType(BlkSymType value);

        protected abstract T ProcSymType(ProcSymType value);

        protected abstract T BPSymType(BPSymType value);

        protected abstract T LocSymType(LocSymType value);

        protected abstract T LabSymType(LabSymType value);

        protected abstract T WithSymType(WithSymType value);

        protected abstract T RegSymType(RegSymType value);

        protected abstract T ConSymType(ConSymType value);

        protected abstract T TypeDefSymType(TypeDefSymType value);

        protected abstract T ThunkSymType(ThunkSymType value);

        protected abstract T CV4BlkSymType(CV4BlkSymType value);

        protected abstract T CV4WithSymType(CV4WithSymType value);

        protected abstract T CV4LabSymType(CV4LabSymType value);
    }
}
