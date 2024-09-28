using System.Collections.Generic;
#if !DEBUG_POSITION
#endif

namespace PESpy.View
{
    public static class ViewExtensions
    {
        public static IEnumerable<IView> Descendants(this IView view)
        {
            if (view is IContainerView v)
            {
                foreach (var child in v.Children)
                {
                    yield return child;

                    foreach (var grandChild in child.Descendants())
                        yield return grandChild;
                }
            }
        }
    }
}
