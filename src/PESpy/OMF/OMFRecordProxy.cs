using System;
using System.Diagnostics;
using System.Text;

namespace PESpy.OMF
{
    class OMFRecordProxy
    {
        private OMFRecord omfRecord;

        public OMFRecordProxy(OMFRecord omfRecord)
        {
            this.omfRecord = omfRecord;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public object Value => GetValue(omfRecord);

        internal static object GetValue(in OMFRecord omfRecord)
        {
            switch (omfRecord.RecordType)
            {
                case OMFRecordType.THEADR:
                    return (THEADR) omfRecord;

                case OMFRecordType.LHEADR:
                    return (LHEADR) omfRecord;

                case OMFRecordType.COMENT:
                    return (COMENT) omfRecord;

                case OMFRecordType.MODEND: //8A
                case (OMFRecordType) 0x8B: //32-bit
                    return (MODEND) omfRecord;

                case OMFRecordType.EXTDEF:
                    return (EXTDEF) omfRecord;

                case OMFRecordType.PUBDEF: //90
                case (OMFRecordType) 0x91: //32-bit
                    return (PUBDEF) omfRecord;

                case OMFRecordType.LINNUM: //94
                case (OMFRecordType) 0x95:
                    return (LINNUM) omfRecord;

                case OMFRecordType.LNAMES:
                    return (LNAMES) omfRecord;

                case OMFRecordType.SEGDEF: //98
                case (OMFRecordType) 0x99:
                    return (SEGDEF) omfRecord;

                case OMFRecordType.GRPDEF:
                    return (GRPDEF) omfRecord;

                case OMFRecordType.FIXUPP:
                    return (FIXUPP) omfRecord;

                case OMFRecordType.FIXUP2:
                    return (FIXUP2) omfRecord;

                case OMFRecordType.LEDATA: //A0
                case (OMFRecordType) 0xA1: //32-bit
                    return (LEDATA) omfRecord;

                case OMFRecordType.LIDATA: //A3
                case (OMFRecordType) 0xA3: //32-bit
                    return (LIDATA) omfRecord;

                case OMFRecordType.COMDEF:
                    return (COMDEF) omfRecord;

                case OMFRecordType.BAKPAT: //B2
                case (OMFRecordType) 0xB3: //32-bit
                    return (BAKPAT) omfRecord;

                case OMFRecordType.LEXTDEF: //B4
                case (OMFRecordType) 0xB5: //32-bit
                    return (LEXTDEF) omfRecord;

                case OMFRecordType.LPUBDEF: //B6
                case (OMFRecordType) 0xB7:
                    return (LPUBDEF) omfRecord;

                case OMFRecordType.LCOMDEF:
                    return (LCOMDEF) omfRecord;

                case OMFRecordType.CEXTDEF:
                    return (CEXTDEF) omfRecord;

                case OMFRecordType.COMDAT: //C2
                    return (COMDAT) omfRecord;

                case OMFRecordType.LINSYM: //C4
                case (OMFRecordType) 0xC5: //32-bit
                    return (LINSYM) omfRecord;

                case OMFRecordType.NBKPAT: //C8
                case (OMFRecordType) 0xC9:
                    return (NBKPAT) omfRecord;

                case OMFRecordType.LLNAMES:
                    return (LLNAMES) omfRecord;

                case OMFRecordType.BLKDEF:
                case OMFRecordType.TYPDEF:
                case OMFRecordType.ALIAS:
                    throw new NotImplementedException($"Don't know how to handle {nameof(OMFRecordType)} of type '{omfRecord.RecordType}'");

                case OMFRecordType.LIBHDR:
                    return (LIBHDR) omfRecord;

                case OMFRecordType.DICHDR:
                    return (DICHDR) omfRecord;

                case OMFRecordType.LIBEXD:
                    return (LIBEXD) omfRecord;

                default:
                    throw new NotImplementedException($"Don't know how to handle {nameof(OMFRecordType)} of type '{omfRecord.RecordType}'");
            }
        }

        public static string GetString(OMFRecord omfRecord)
        {
            var underlying = GetValue(omfRecord);

            if (underlying is OMFRecord r)
                return r.RecordType.ToString();

            return underlying.ToString();
        }

        public static string DebuggerDisplay(OMFRecord omfRecord)
        {
            var builder = new StringBuilder();
            builder.Append("[").Append(omfRecord.RecordType).Append("]");

            var value = GetValue(omfRecord);

            var defaultStr = omfRecord.RecordType.ToString();

            var str = value.ToString();

            if (defaultStr != str)
                builder.Append(" ").Append(str);

            return builder.ToString();
        }
    }
}
