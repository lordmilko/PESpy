namespace PESpy.OMF
{
    internal sealed class ObjectOMFRecordDispatcher : OMFRecordDispatcher<object>
    {
        public static readonly ObjectOMFRecordDispatcher Instance = new();

        protected override object OMFRecord(OMFRecord value) => value.RecordType;

        protected override object BAKPAT(BAKPAT value) => value;
        protected override object CEXTDEF(CEXTDEF value) => value;
        protected override object COMDAT(COMDAT value) => value;
        protected override object COMDEF(COMDEF value) => value;
        protected override object COMENT(COMENT value) => value;
        protected override object DICHDR(DICHDR value) => value;
        protected override object EXTDEF(EXTDEF value) => value;
        protected override object FIXUP2(FIXUP2 value) => value;
        protected override object FIXUPP(FIXUPP value) => value;
        protected override object GRPDEF(GRPDEF value) => value;
        protected override object LCOMDEF(LCOMDEF value) => value;
        protected override object LEDATA(LEDATA value) => value;
        protected override object LEXTDEF(LEXTDEF value) => value;
        protected override object LHEADR(LHEADR value) => value;
        protected override object LIBEXD(LIBEXD value) => value;
        protected override object LIBHDR(LIBHDR value) => value;
        protected override object LIDATA(LIDATA value) => value;
        protected override object LINNUM(LINNUM value) => value;
        protected override object LINSYM(LINSYM value) => value;
        protected override object LLNAMES(LLNAMES value) => value;
        protected override object LNAMES(LNAMES value) => value;
        protected override object LPUBDEF(LPUBDEF value) => value;
        protected override object MODEND(MODEND value) => value;
        protected override object NBKPAT(NBKPAT value) => value;
        protected override object PUBDEF(PUBDEF value) => value;
        protected override object SEGDEF(SEGDEF value) => value;
        protected override object THEADR(THEADR value) => value;
    }
}
