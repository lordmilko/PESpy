using System;
using System.Diagnostics;

namespace PESpy.OMF
{
    public abstract class OMFRecordDispatcher<T>
    {
        public T Dispatch(OMFRecord omfRecord)
        {
            switch (omfRecord.RecordType)
            {
                case OMFRecordType.THEADR:
                    return THEADR((THEADR) omfRecord);

                case OMFRecordType.LHEADR:
                    return LHEADR((LHEADR) omfRecord);

                case OMFRecordType.COMENT:
                    return COMENT((COMENT) omfRecord);

                case OMFRecordType.MODEND: //8A
                case (OMFRecordType) 0x8B: //32-bit
                    return MODEND((MODEND) omfRecord);

                case OMFRecordType.EXTDEF:
                    return EXTDEF((EXTDEF) omfRecord);

                case OMFRecordType.PUBDEF: //90
                case (OMFRecordType) 0x91: //32-bit
                    return PUBDEF((PUBDEF) omfRecord);

                case OMFRecordType.LINNUM: //94
                case (OMFRecordType) 0x95:
                    return LINNUM((LINNUM) omfRecord);

                case OMFRecordType.LNAMES:
                    return LNAMES((LNAMES) omfRecord);

                case OMFRecordType.SEGDEF: //98
                case (OMFRecordType) 0x99:
                    return SEGDEF((SEGDEF) omfRecord);

                case OMFRecordType.GRPDEF:
                    return GRPDEF((GRPDEF) omfRecord);

                case OMFRecordType.FIXUPP:
                    return FIXUPP((FIXUPP) omfRecord);

                case OMFRecordType.FIXUP2:
                    return FIXUP2((FIXUP2) omfRecord);

                case OMFRecordType.LEDATA: //A0
                case (OMFRecordType) 0xA1: //32-bit
                    return LEDATA((LEDATA) omfRecord);

                case OMFRecordType.LIDATA: //A3
                case (OMFRecordType) 0xA3: //32-bit
                    return LIDATA((LIDATA) omfRecord);

                case OMFRecordType.COMDEF:
                    return COMDEF((COMDEF) omfRecord);

                case OMFRecordType.BAKPAT: //B2
                case (OMFRecordType) 0xB3: //32-bit
                    return BAKPAT((BAKPAT) omfRecord);

                case OMFRecordType.LEXTDEF: //B4
                case (OMFRecordType) 0xB5: //32-bit
                    return LEXTDEF((LEXTDEF) omfRecord);

                case OMFRecordType.LPUBDEF: //B6
                case (OMFRecordType) 0xB7:
                    return LPUBDEF((LPUBDEF) omfRecord);

                case OMFRecordType.LCOMDEF:
                    return LCOMDEF((LCOMDEF) omfRecord);

                case OMFRecordType.CEXTDEF:
                    return CEXTDEF((CEXTDEF) omfRecord);

                case OMFRecordType.COMDAT: //C2
                    return COMDAT((COMDAT) omfRecord);

                case OMFRecordType.LINSYM: //C4
                case (OMFRecordType) 0xC5: //32-bit
                    return LINSYM((LINSYM) omfRecord);

                case OMFRecordType.NBKPAT: //C8
                case (OMFRecordType) 0xC9:
                    return NBKPAT((NBKPAT) omfRecord);

                case OMFRecordType.LLNAMES:
                    return LLNAMES((LLNAMES) omfRecord);

                case OMFRecordType.BLKDEF:
                case OMFRecordType.TYPDEF:
                case OMFRecordType.ALIAS:
                    throw new NotImplementedException($"Don't know how to handle {nameof(OMFRecordType)} of type '{omfRecord.RecordType}'");

                case OMFRecordType.LIBHDR:
                    return LIBHDR((LIBHDR) omfRecord);

                case OMFRecordType.DICHDR:
                    return DICHDR((DICHDR) omfRecord);

                case OMFRecordType.LIBEXD:
                    return LIBEXD((LIBEXD) omfRecord);

                default:
                    Debug.Assert(false, $"Don't know how to handle {nameof(OMFRecordType)} of type '{omfRecord.RecordType}'");
                    return OMFRecord(omfRecord);
            }
        }

        protected abstract T OMFRecord(OMFRecord value);

        protected abstract T THEADR(THEADR value);
        protected abstract T LHEADR(LHEADR value);
        protected abstract T COMENT(COMENT value);
        protected abstract T MODEND(MODEND value);
        protected abstract T EXTDEF(EXTDEF value);
        protected abstract T PUBDEF(PUBDEF value);
        protected abstract T LINNUM(LINNUM value);
        protected abstract T LNAMES(LNAMES value);
        protected abstract T SEGDEF(SEGDEF value);
        protected abstract T GRPDEF(GRPDEF value);
        protected abstract T FIXUPP(FIXUPP value);
        protected abstract T FIXUP2(FIXUP2 value);
        protected abstract T LEDATA(LEDATA value);
        protected abstract T LIDATA(LIDATA value);
        protected abstract T COMDEF(COMDEF value);
        protected abstract T BAKPAT(BAKPAT value);
        protected abstract T LEXTDEF(LEXTDEF value);
        protected abstract T LPUBDEF(LPUBDEF value);
        protected abstract T LCOMDEF(LCOMDEF value);
        protected abstract T CEXTDEF(CEXTDEF value);
        protected abstract T COMDAT(COMDAT value);
        protected abstract T LINSYM(LINSYM value);
        protected abstract T NBKPAT(NBKPAT value);
        protected abstract T LLNAMES(LLNAMES value);
        protected abstract T LIBHDR(LIBHDR value);
        protected abstract T DICHDR(DICHDR value);
        protected abstract T LIBEXD(LIBEXD value);
    }
}
