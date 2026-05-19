using System.Runtime.CompilerServices;
using PESpy.View;

namespace PESpy.OMF
{
    public sealed class ViewOMFRecordDispatcher : OMFRecordDispatcher<IView>
    {
        private ViewWriter viewWriter;

        internal ViewOMFRecordDispatcher(ViewWriter viewWriter)
        {
            this.viewWriter = viewWriter;
        }

        protected override IView OMFRecord(OMFRecord value) => NewStruct(value);

        protected override IView BAKPAT(BAKPAT value) => NewStruct(value);
        protected override IView CEXTDEF(CEXTDEF value) => NewStruct(value);
        protected override IView COMDAT(COMDAT value) => NewStruct(value);
        protected override IView COMDEF(COMDEF value) => NewStruct(value);
        protected override IView COMENT(COMENT value) => NewStruct(value);
        protected override IView DICHDR(DICHDR value) => NewStruct(value);
        protected override IView EXTDEF(EXTDEF value) => NewStruct(value);
        protected override IView FIXUP2(FIXUP2 value) => NewStruct(value);
        protected override IView FIXUPP(FIXUPP value) => NewStruct(value);
        protected override IView GRPDEF(GRPDEF value) => NewStruct(value);
        protected override IView LCOMDEF(LCOMDEF value) => NewStruct(value);
        protected override IView LEDATA(LEDATA value) => NewStruct(value);
        protected override IView LEXTDEF(LEXTDEF value) => NewStruct(value);
        protected override IView LHEADR(LHEADR value) => NewStruct(value);
        protected override IView LIBEXD(LIBEXD value) => NewStruct(value);
        protected override IView LIBHDR(LIBHDR value) => NewStruct(value);
        protected override IView LIDATA(LIDATA value) => NewStruct(value);
        protected override IView LINNUM(LINNUM value) => NewStruct(value);
        protected override IView LINSYM(LINSYM value) => NewStruct(value);
        protected override IView LLNAMES(LLNAMES value) => NewStruct(value);
        protected override IView LNAMES(LNAMES value) => NewStruct(value);
        protected override IView LPUBDEF(LPUBDEF value) => NewStruct(value);
        protected override IView MODEND(MODEND value) => NewStruct(value);
        protected override IView NBKPAT(NBKPAT value) => NewStruct(value);
        protected override IView PUBDEF(PUBDEF value) => NewStruct(value);
        protected override IView SEGDEF(SEGDEF value) => NewStruct(value);
        protected override IView THEADR(THEADR value) => NewStruct(value);

        //Prevent boxing
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private IView NewStruct<T>(T value) where T : IViewable =>
            value.WriteStruct(viewWriter);
    }
}
