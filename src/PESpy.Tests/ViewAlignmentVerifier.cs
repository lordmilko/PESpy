using Microsoft.VisualStudio.TestTools.UnitTesting;
using PESpy.View;

namespace PESpy.Tests
{
    class ViewAlignmentVerifier : ViewWalker
    {
        protected override void VisitChildren(IContainerView view)
        {
            for (var i = 0; i < view.Children.Length; i++)
            {
                if (i < view.Children.Length - 1)
                {
                    //The end of current is Offset + Size - 1. We ignore that for the purposes of this comparison,
                    //since we want to know that next immediately follows current (which is true if their start and end are the same
                    //(offset + Size - 1 + 1 == next.Offset))
                    var current = view.Children[i];
                    var next = view.Children[i + 1];

                    var currentEnd = current.Offset + current.Size;

                    if (current is IBitFieldView && next is IBitFieldView)
                        continue;

                    if (currentEnd != next.Offset)
                        Assert.AreEqual("0x" + currentEnd.ToString("X"), "0x" + next.Offset.ToString("X"), "Subviews were not aligned");
                }

                Visit(view.Children[i]);
            }
        }
    }
}
