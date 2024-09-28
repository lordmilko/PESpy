namespace PESpy.View
{
    public abstract class PEViewVisitor<TResult>
    {
        public virtual TResult? Visit(IView? view)
        {
            if (view != null)
                return view.Accept(this);

            return default;
        }

        protected internal abstract TResult VisitAsm(IAsmView view);
        protected internal abstract TResult VisitBitField(IBitFieldView view);
        protected internal abstract TResult VisitByteBlob(ByteBlobView view);
        protected internal abstract TResult VisitField(IFieldView view);
        protected internal abstract TResult VisitHeader(HeaderView view);
        protected internal abstract TResult VisitLogicalReview(LogicalRegionView view);
        protected internal abstract TResult VisitOverlay(OverlayView view);
        protected internal abstract TResult VisitPEFile(PEFileView view);
        protected internal abstract TResult VisitSection(SectionView view);
        protected internal abstract TResult VisitStruct(StructView view);
        protected internal abstract TResult VisitValue(IValueView view);
    }
}
