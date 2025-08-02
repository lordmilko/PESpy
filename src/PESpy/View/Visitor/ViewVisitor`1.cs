namespace PESpy.View
{
    public abstract class ViewVisitor<TResult>
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
        protected internal abstract TResult VisitLogicalRegion(LogicalRegionView view);
        protected internal abstract TResult VisitOverlay(OverlayView view);
        protected internal abstract TResult VisitFile(FileView view);
        protected internal abstract TResult VisitSection(SectionView view);
        protected internal abstract TResult VisitStruct(IStructView view);
        protected internal abstract TResult VisitValue(IValueView view);
    }
}
