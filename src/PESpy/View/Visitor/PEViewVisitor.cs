namespace PESpy.View
{
    public abstract class PEViewVisitor
    {
        public virtual void Visit(IView view) => view.Accept(this);

        protected internal abstract void VisitAsm(IAsmView view);
        protected internal abstract void VisitBitField(IBitFieldView view);
        protected internal abstract void VisitByteBlob(ByteBlobView view);
        protected internal abstract void VisitField(IFieldView view);
        protected internal abstract void VisitHeader(HeaderView view);
        protected internal abstract void VisitLogicalReview(LogicalRegionView view);
        protected internal abstract void VisitOverlay(OverlayView view);
        protected internal abstract void VisitPEFile(PEFileView view);
        protected internal abstract void VisitSection(SectionView view);
        protected internal abstract void VisitStruct(StructView view);
        protected internal abstract void VisitValue(IValueView view);

        protected virtual void VisitChildren(IContainerView view)
        {
            foreach (var child in view.Children)
                Visit(child);
        }
    }
}
