namespace PESpy.View
{
    public abstract class ViewWalker : ViewVisitor
    {
        protected internal override void VisitAsm(IAsmView view)
        {
            //No children
        }

        protected internal override void VisitBitField(IBitFieldView view)
        {
            //No children
        }

        protected internal override void VisitByteBlob(ByteBlobView view)
        {
            //No children
        }

        protected internal override void VisitField(IFieldView view)
        {
            //No children
        }

        protected internal override void VisitHeader(HeaderView view) => VisitChildren(view);

        protected internal override void VisitLogicalReview(LogicalRegionView view) => VisitChildren(view);

        protected internal override void VisitOverlay(OverlayView view) => VisitChildren(view);
        protected internal override void VisitFile(FileView view) => VisitChildren(view);

        protected internal override void VisitSection(SectionView view) => VisitChildren(view);

        protected internal override void VisitStruct(IStructView view) => VisitChildren(view);

        protected internal override void VisitValue(IValueView view)
        {
            //No children
        }
    }
}
