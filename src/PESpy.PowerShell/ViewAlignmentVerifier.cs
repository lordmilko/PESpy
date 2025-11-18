using System;
using PESpy.View;

namespace PESpy.PowerShell
{
    public class ViewAlignmentVerifier : ViewWalker
    {
        protected override void VisitChildren(IContainerView view)
        {
            for (var i = 0; i < view.Children.Count; i++)
            {
                if (i < view.Children.Count - 1)
                {
                    //The end of current is Offset + Size - 1. We ignore that for the purposes of this comparison,
                    //since we want to know that next immediately follows current (which is true if their start and end are the same
                    //(offset + Size - 1 + 1 == next.Offset))
                    var current = view.Children[i];
                    var next = view.Children[i + 1];

                    var currentEnd = current.Offset + current.Size;

                    //If the parent is a split view, it's expected you may have unaligned children
                    if ((current is IBitFieldView && next is IBitFieldView) || view is ISplitView)
                        continue;

                    if (currentEnd != next.Offset)
                        throw new InvalidOperationException($"Expected: 0x{currentEnd:X}. Actual: 0x{next.Offset:X}. Subviews were not aligned");
                }

                Visit(view.Children[i]);
            }
        }
    }
}
