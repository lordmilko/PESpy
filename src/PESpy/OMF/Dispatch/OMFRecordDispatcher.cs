using System;
using System.Diagnostics;

namespace PESpy.OMF
{
    public abstract class OMFRecordDispatcher
    {
        public void Dispatch(OMFRecord omfRecord)
        {
            switch (omfRecord.RecordType)
            {
                case OMFRecordType.THEADR:
                    THEADR((THEADR) omfRecord);
                    break;

                case OMFRecordType.LHEADR:
                    LHEADR((LHEADR) omfRecord);
                    break;

                case OMFRecordType.COMENT:
                    COMENT((COMENT) omfRecord);
                    break;

                case OMFRecordType.MODEND: //8A
                case (OMFRecordType) 0x8B: //32-bit
                    MODEND((MODEND) omfRecord);
                    break;

                case OMFRecordType.EXTDEF:
                    EXTDEF((EXTDEF) omfRecord);
                    break;

                case OMFRecordType.PUBDEF: //90
                case (OMFRecordType) 0x91: //32-bit
                    PUBDEF((PUBDEF) omfRecord);
                    break;

                case OMFRecordType.LINNUM: //94
                case (OMFRecordType) 0x95:
                    LINNUM((LINNUM) omfRecord);
                    break;

                case OMFRecordType.LNAMES:
                    LNAMES((LNAMES) omfRecord);
                    break;

                case OMFRecordType.SEGDEF: //98
                case (OMFRecordType) 0x99:
                    SEGDEF((SEGDEF) omfRecord);
                    break;

                case OMFRecordType.GRPDEF:
                    GRPDEF((GRPDEF) omfRecord);
                    break;

                case OMFRecordType.FIXUPP:
                    FIXUPP((FIXUPP) omfRecord);
                    break;

                case OMFRecordType.FIXUP2:
                    FIXUP2((FIXUP2) omfRecord);
                    break;

                case OMFRecordType.LEDATA: //A0
                case (OMFRecordType) 0xA1: //32-bit
                    LEDATA((LEDATA) omfRecord);
                    break;

                case OMFRecordType.LIDATA: //A3
                case (OMFRecordType) 0xA3: //32-bit
                    LIDATA((LIDATA) omfRecord);
                    break;

                case OMFRecordType.COMDEF:
                    COMDEF((COMDEF) omfRecord);
                    break;

                case OMFRecordType.BAKPAT: //B2
                case (OMFRecordType) 0xB3: //32-bit
                    BAKPAT((BAKPAT) omfRecord);
                    break;

                case OMFRecordType.LEXTDEF: //B4
                case (OMFRecordType) 0xB5: //32-bit
                    LEXTDEF((LEXTDEF) omfRecord);
                    break;

                case OMFRecordType.LPUBDEF: //B6
                case (OMFRecordType) 0xB7:
                    LPUBDEF((LPUBDEF) omfRecord);
                    break;

                case OMFRecordType.LCOMDEF:
                    LCOMDEF((LCOMDEF) omfRecord);
                    break;

                case OMFRecordType.CEXTDEF:
                    CEXTDEF((CEXTDEF) omfRecord);
                    break;

                case OMFRecordType.COMDAT: //C2
                    COMDAT((COMDAT) omfRecord);
                    break;

                case OMFRecordType.LINSYM: //C4
                case (OMFRecordType) 0xC5: //32-bit
                    LINSYM((LINSYM) omfRecord);
                    break;

                case OMFRecordType.NBKPAT: //C8
                case (OMFRecordType) 0xC9:
                    NBKPAT((NBKPAT) omfRecord);
                    break;

                case OMFRecordType.LLNAMES:
                    LLNAMES((LLNAMES) omfRecord);
                    break;

                case OMFRecordType.BLKDEF:
                case OMFRecordType.TYPDEF:
                case OMFRecordType.ALIAS:
                    throw new NotImplementedException($"Don't know how to handle {nameof(OMFRecordType)} of type '{omfRecord.RecordType}'");

                case OMFRecordType.LIBHDR:
                    LIBHDR((LIBHDR) omfRecord);
                    break;

                case OMFRecordType.DICHDR:
                    DICHDR((DICHDR) omfRecord);
                    break;

                case OMFRecordType.LIBEXD:
                    LIBEXD((LIBEXD) omfRecord);
                    break;

                default:
                    Debug.Assert(false, $"Don't know how to handle {nameof(OMFRecordType)} of type '{omfRecord.RecordType}'");
                    OMFRecord(omfRecord);
                    break;
            }
        }

        protected abstract void OMFRecord(OMFRecord value);

        protected abstract void THEADR(THEADR value);
        protected abstract void LHEADR(LHEADR value);
        protected abstract void COMENT(COMENT value);
        protected abstract void MODEND(MODEND value);
        protected abstract void EXTDEF(EXTDEF value);
        protected abstract void PUBDEF(PUBDEF value);
        protected abstract void LINNUM(LINNUM value);
        protected abstract void LNAMES(LNAMES value);
        protected abstract void SEGDEF(SEGDEF value);
        protected abstract void GRPDEF(GRPDEF value);
        protected abstract void FIXUPP(FIXUPP value);
        protected abstract void FIXUP2(FIXUP2 value);
        protected abstract void LEDATA(LEDATA value);
        protected abstract void LIDATA(LIDATA value);
        protected abstract void COMDEF(COMDEF value);
        protected abstract void BAKPAT(BAKPAT value);
        protected abstract void LEXTDEF(LEXTDEF value);
        protected abstract void LPUBDEF(LPUBDEF value);
        protected abstract void LCOMDEF(LCOMDEF value);
        protected abstract void CEXTDEF(CEXTDEF value);
        protected abstract void COMDAT(COMDAT value);
        protected abstract void LINSYM(LINSYM value);
        protected abstract void NBKPAT(NBKPAT value);
        protected abstract void LLNAMES(LLNAMES value);
        protected abstract void LIBHDR(LIBHDR value);
        protected abstract void DICHDR(DICHDR value);
        protected abstract void LIBEXD(LIBEXD value);
    }
}
