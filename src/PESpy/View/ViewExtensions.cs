using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace PESpy.View
{
    public static class ViewExtensions
    {
        /// <summary>
        /// Gets whether the specified view is capable of being a container for other views;
        /// that is to say, whether the view is capable of having children.
        /// </summary>
        /// <param name="view"></param>
        /// <returns></returns>
        public static bool IsContainer(this IView view)
        {
            switch (view.ImplKind)
            {
                case ViewImplKind.File:
                case ViewImplKind.Header:
                case ViewImplKind.Section:
                case ViewImplKind.Overlay:
                case ViewImplKind.Struct:
                case ViewImplKind.LogicalRegion:
                case ViewImplKind.StructField:
                    return true;

                default:
                    return false;
            }
        }

        public static IEnumerable<IView> AncestorsAndSelf(this IView view)
        {
            yield return view;

            foreach (var parent in view.Ancestors())
                yield return parent;
        }

        public static IEnumerable<IView> Ancestors(this IView view)
        {
            var parent = view.Parent;

            while (parent != null)
            {
                yield return parent;

                parent = parent.Parent;
            }
        }

        public static IEnumerator<IView> DescendantsAndSelf(this IView view)
        {
            yield return view;

            foreach (var child in view.Descendants())
                yield return view;
        }

        public static IEnumerable<IView> Descendants(this IView view)
        {
            if (view.IsContainer())
            {
                foreach (var child in view.Children)
                {
                    yield return child;

                    foreach (var grandChild in child.Descendants())
                        yield return grandChild;
                }
            }
        }

        public static IView GetField(this IView view, string name)
        {
            foreach (var child in view.Children)
            {
                if (child.Kind == ViewKind.Field)
                {
                    var field = Unsafe.As<IFieldView>(child);

                    if (field.Name == name)
                        return field;
                }
                else if (child.Kind == ViewKind.BitField)
                {
                    var field = Unsafe.As<IBitFieldView>(child);

                    if (field.Name == name)
                        return field;
                }
            }

            throw new InvalidOperationException($"Failed to find a field or bitfield named '{name}'");
        }

        public static IView GetChild(this IView view, ViewKind kind)
        {
            foreach (var child in view.Children)
            {
                if (child.Kind == kind)
                    return child;
            }

            throw new InvalidOperationException($"Failed to find a child of kind '{kind}'");
        }

        public static bool Contains(this IView view, long offset) =>
            offset >= view.Offset && offset < view.Offset + view.Size;

        public static string ToString(this IView view, ViewFormatFlags flags) =>
            ViewFormatter.Format(view, flags);
    }
}
