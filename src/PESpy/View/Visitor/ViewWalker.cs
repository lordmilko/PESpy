using System;

namespace PESpy.View
{
    public sealed class NullViewWalker : ViewWalker
    {
        public static readonly NullViewWalker Instance = new NullViewWalker();
    }

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

        protected internal override void VisitLogicalRegion(LogicalRegionView view) => VisitChildren(view);

        protected internal override void VisitOverlay(OverlayView view) => VisitChildren(view);
        protected internal override void VisitFile(FileView view) => VisitChildren(view);

        protected internal override void VisitSection(SectionView view) => VisitChildren(view);

        protected internal override void VisitStruct(IStructView view) => VisitChildren(view);

        protected internal override void VisitStructField(IStructFieldView view) => VisitChildren(view.Value);

        protected internal override void VisitStructArrayField(IStructArrayFieldView view)
        {
            foreach (var value in view.Value)
                VisitChildren(value);
        }

        protected internal override void VisitValue(IValueView view)
        {
            //No children
        }
    }
}
