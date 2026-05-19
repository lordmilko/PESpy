using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using PESpy.View;

namespace PESpy.OMF
{
    //Name is made up. Has no relation to well known OMF debug info types defined in Microsoft headers
    [DebuggerTypeProxy(typeof(OMFRecordProxy))]
    [DebuggerDisplay("{OMFRecordProxy.DebuggerDisplay(this),nq}")]
    public readonly unsafe struct OMFRecord : IViewable
    {
        //https://www.pcjs.org/documents/books/mspl13/msdos/dosref40/

        /* The format of each OMF Record is always as follows
         * - Record Type (1 byte)
         * - Record Length (2 bytes)
         * - Variable contents
         * - Checksum, which contains the two's complement of the sum (mod 256) of all other bytes in the record.
         *   Therefore, the sum (mod 256) of all bytes in the record is zero
         */

        private readonly byte* value;

        public OMFRecord(byte* value)
        {
            this.value = value;
        }

        public OMFRecordType RecordType => (OMFRecordType) (*value & ~1);

        public bool Is32Bit => (*value & 1) == 1;

        public byte RawRecordType => *value;

        public ushort RecordLength => *(ushort*) (value + 1);

        public NativeSpan<byte> Content => new NativeSpan<byte>(value + 3, RecordLength - (HasChecksum(RecordType) ? 1 : 0));

        //Invalid for LIBHDR and DICHDR
        public byte Checksum
        {
            get
            {
                if (HasChecksum(RecordType))
                    return *(value + RecordLength + 2);

                return 0;
            }
        }

        private static bool HasChecksum(OMFRecordType type)
        {
            switch (type)
            {
                case OMFRecordType.LIBHDR:
                case OMFRecordType.DICHDR:
                case OMFRecordType.LIBEXD:
                    return false;

                default:
                    return true;
            }
        }

        internal static ushort GetIndex(byte* pIndex, out int indexSize)
        {
            //https://www.azillionmonkeys.com/qed/Omfg.pdf p3

            var lo = *pIndex;

            if ((lo & 0x80) != 0)
            {
                indexSize = 2;
                return (ushort) ((lo & 0x7F) * 0x100 + *(pIndex + 1));
            }

            indexSize = 1;
            return lo;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int GetStructSize(byte* ptr) =>
            *(ushort*) (ptr + 1) + sizeof(byte) + sizeof(short);

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(this, ViewKind.OMFRecord, GetStructSize(value));

        int IViewable.NumChildren() => HasChecksum(RecordType) ? 4 : 3;

        void IViewable.WriteChild(int index, ref StructWriter structWriter) =>
            WriteDefaultChild(this, index, ref structWriter);

        internal static int GetDefaultNumChildren() => 4;

        internal static int GetDefaultNumChildrenNoChecksum() => 3;

        internal static void WriteDefaultChild(OMFRecord omfRecord, int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(RecordType), 0, omfRecord.RawRecordType);
                    break;

                case 1:
                    structWriter.WriteField(nameof(RecordLength), 1, omfRecord.RecordLength);
                    break;

                case 2:
                    structWriter.WriteField("Content", 3, omfRecord.Content);
                    break;

                case 3:
                    if (HasChecksum(omfRecord.RecordType))
                        structWriter.WriteField(nameof(Checksum), omfRecord.RecordLength + sizeof(short), omfRecord.Checksum);
                    else
                        throw new IndexOutOfRangeException();
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }

        public override string ToString()
        {
            if (value == default)
                return "<null>";

            return StringOMFRecordDispatcher.Instance.Dispatch(this);
        }

        public static implicit operator THEADR(OMFRecord record) => new THEADR(record.value);
        public static implicit operator LHEADR(OMFRecord record) => new LHEADR(record.value);
        public static implicit operator COMENT(OMFRecord record) => new COMENT(record.value);
        public static implicit operator MODEND(OMFRecord record) => new MODEND(record.value);
        public static implicit operator EXTDEF(OMFRecord record) => new EXTDEF(record.value);
        public static implicit operator PUBDEF(OMFRecord record) => new PUBDEF(record.value);
        public static implicit operator LINNUM(OMFRecord record) => new LINNUM(record.value);
        public static implicit operator LNAMES(OMFRecord record) => new LNAMES(record.value);
        public static implicit operator SEGDEF(OMFRecord record) => new SEGDEF(record.value);
        public static implicit operator GRPDEF(OMFRecord record) => new GRPDEF(record.value);
        public static implicit operator FIXUPP(OMFRecord record) => new FIXUPP(record.value);
        public static implicit operator FIXUP2(OMFRecord record) => new FIXUP2(record.value);
        public static implicit operator LEDATA(OMFRecord record) => new LEDATA(record.value);
        public static implicit operator LIDATA(OMFRecord record) => new LIDATA(record.value);
        public static implicit operator COMDEF(OMFRecord record) => new COMDEF(record.value);
        public static implicit operator BAKPAT(OMFRecord record) => new BAKPAT(record.value);
        public static implicit operator LEXTDEF(OMFRecord record) => new LEXTDEF(record.value);
        public static implicit operator LPUBDEF(OMFRecord record) => new LPUBDEF(record.value);
        public static implicit operator LCOMDEF(OMFRecord record) => new LCOMDEF(record.value);
        public static implicit operator CEXTDEF(OMFRecord record) => new CEXTDEF(record.value);
        public static implicit operator COMDAT(OMFRecord record) => new COMDAT(record.value);
        public static implicit operator LINSYM(OMFRecord record) => new LINSYM(record.value);
        public static implicit operator NBKPAT(OMFRecord record) => new NBKPAT(record.value);
        public static implicit operator LLNAMES(OMFRecord record) => new LLNAMES(record.value);

        public static implicit operator LIBHDR(OMFRecord record) => new LIBHDR(record.value);
        public static implicit operator DICHDR(OMFRecord record) => new DICHDR(record.value);
        public static implicit operator LIBEXD(OMFRecord record) => new LIBEXD(record.value);
    }
}
