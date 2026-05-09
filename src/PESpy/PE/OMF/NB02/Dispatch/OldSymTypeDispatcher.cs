using System.Diagnostics;
using static ClrDebug.OMF.OLDSYM;

namespace PESpy
{
    public abstract class OldSymTypeDispatcher
    {
        public void Dispatch(OldSymType value)
        {
            switch (value.rectyp)
            {
                case S_BLOCK:
                    BlkSymType((BlkSymType) value);
                    break;

                case S_PROC:
                case S_ENTRY:
                    ProcSymType((ProcSymType) value);
                    break;

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
                    OldSymType(value);
                    break;

                case S_BPREL:
                    BPSymType((BPSymType) value);
                    break;

                case S_LOCAL:
                    LocSymType((LocSymType) value);
                    break;

                case S_LABEL:
                    LabSymType((LabSymType) value);
                    break;

                case S_WITH:
                    WithSymType((WithSymType) value);
                    break;

                case S_REG:
                    RegSymType((RegSymType) value);
                    break;

                case S_CONST:
                    ConSymType((ConSymType) value);
                    break;

                case S_TYPEDEF:
                    TypeDefSymType((TypeDefSymType) value);
                    break;

                case S_THUNK:
                    ThunkSymType((ThunkSymType) value);
                    break;
                
                case S_CV4BLOCK:
                    CV4BlkSymType((CV4BlkSymType) value);
                    break;

                case S_CV4WITH:
                    CV4WithSymType((CV4WithSymType) value);
                    break;

                case S_CV4LABEL:
                    CV4LabSymType((CV4LabSymType) value);
                    break;

                default:
                    Debug.Assert(false);
                    OldSymType((OldSymType) value);
                    break;
            }
        }

        protected abstract void OldSymType(OldSymType value);

        protected abstract void BlkSymType(BlkSymType value);

        protected abstract void ProcSymType(ProcSymType value);

        protected abstract void BPSymType(BPSymType value);

        protected abstract void LocSymType(LocSymType value);

        protected abstract void LabSymType(LabSymType value);

        protected abstract void WithSymType(WithSymType value);

        protected abstract void RegSymType(RegSymType value);

        protected abstract void ConSymType(ConSymType value);

        protected abstract void TypeDefSymType(TypeDefSymType value);

        protected abstract void ThunkSymType(ThunkSymType value);

        protected abstract void CV4BlkSymType(CV4BlkSymType value);

        protected abstract void CV4WithSymType(CV4WithSymType value);

        protected abstract void CV4LabSymType(CV4LabSymType value);
    }
}

