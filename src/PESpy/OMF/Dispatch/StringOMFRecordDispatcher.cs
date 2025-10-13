namespace PESpy.OMF
{
    internal sealed class StringOMFRecordDispatcher : OMFRecordDispatcher<string>
    {
        public static readonly StringOMFRecordDispatcher Instance = new();

        protected override string OMFRecord(OMFRecord value) => value.RecordType.ToString();

        protected override string BAKPAT(BAKPAT value) => value.ToString();
        protected override string CEXTDEF(CEXTDEF value) => value.ToString();
        protected override string COMDAT(COMDAT value) => value.ToString();
        protected override string COMDEF(COMDEF value) => value.ToString();
        protected override string COMENT(COMENT value) => value.ToString();
        protected override string DICHDR(DICHDR value) => value.ToString();
        protected override string EXTDEF(EXTDEF value) => value.ToString();
        protected override string FIXUP2(FIXUP2 value) => value.ToString();
        protected override string FIXUPP(FIXUPP value) => value.ToString();
        protected override string GRPDEF(GRPDEF value) => value.ToString();
        protected override string LCOMDEF(LCOMDEF value) => value.ToString();
        protected override string LEDATA(LEDATA value) => value.ToString();
        protected override string LEXTDEF(LEXTDEF value) => value.ToString();
        protected override string LHEADR(LHEADR value) => value.ToString();
        protected override string LIBEXD(LIBEXD value) => value.ToString();
        protected override string LIBHDR(LIBHDR value) => value.ToString();
        protected override string LIDATA(LIDATA value) => value.ToString();
        protected override string LINNUM(LINNUM value) => value.ToString();
        protected override string LINSYM(LINSYM value) => value.ToString();
        protected override string LLNAMES(LLNAMES value) => value.ToString();
        protected override string LNAMES(LNAMES value) => value.ToString();
        protected override string LPUBDEF(LPUBDEF value) => value.ToString();
        protected override string MODEND(MODEND value) => value.ToString();
        protected override string NBKPAT(NBKPAT value) => value.ToString();
        protected override string PUBDEF(PUBDEF value) => value.ToString();
        protected override string SEGDEF(SEGDEF value) => value.ToString();
        protected override string THEADR(THEADR value) => value.ToString();
    }
}
