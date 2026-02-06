using System;
using System.Diagnostics;
using System.Drawing;
using PInvoke;

namespace PESpy.UI
{
    internal class DefaultLayout
    {
        internal static DefaultLayout Instance = new();

        internal bool Layout(NativeWindow container)
        {
            return TryCalculatePreferredSize(container, measureOnly: false, preferredSize: out SIZE _);
        }

        private static bool TryCalculatePreferredSize(NativeWindow container, bool measureOnly, out SIZE preferredSize)
        {
            var children = container.Children;
            preferredSize = default;

            if (!measureOnly && children.Count == 0)
                return container.AutoSize;

            var dock = false;
            var anchor = false;
            var autoSize = false;

            for (var i = children.Count - 1; i >= 0; i--)
            {
                var child = children[i];

                if (!dock && child.Dock != DockStyle.None)
                    dock = true;
            SIZE preferredSizeForDocking = default;

            if (dock)
            {
                preferredSizeForDocking = LayoutDockedControls(container, measureOnly);
            }

            if (anchor && !measureOnly)
                throw new NotImplementedException();

            if (!measureOnly)
                ApplyCachedBounds(container);
            else
            {
                throw new NotImplementedException();
            }

            return container.AutoSize;
        }
        internal static void SetDock(NativeWindow element, DockStyle value)
        {
            //I think WinForms records the original docked state prior to changing it

            var oldDockMode = element.Dock; //The mode gets set after we return

            if (value == DockStyle.None)
            {
                //We don't want t be docked anymore
                throw new NotImplementedException();
            }
            else
            {
                //WinForms calls GetSpecifiedBounds. I believe this gets the last bounds that were explicitly specified
                //for the control. But we don't support changing our fundamental layout once it's been established so I don't think you need to do this
            }
        }

        private static SIZE LayoutDockedControls(NativeWindow parent, bool measureOnly)
        {
            //WinForms uses the display rect. We don't support that (it just takes stuff like the location of the
            //taskbar into consideration)
            Rectangle remainingBounds = measureOnly ? default : parent.ClientRectangle;
            Size preferredSize = default;

            var children = parent.Children;

            for (var i = children.Count - 1; i >= 0; i--)
            {
                var element = children[i];

                switch (element.Dock)
                {
                    case DockStyle.None:
                        break;

                    case DockStyle.Top:
                        {
                            Size elementSize = GetVerticalDockedSize(element, remainingBounds.Size, measureOnly);
                            Rectangle newElementBounds = new(remainingBounds.X, remainingBounds.Y, elementSize.Width, elementSize.Height);

                            TryCalculatePreferredSizeDockedControl(element, newElementBounds, measureOnly, ref preferredSize, ref remainingBounds);

                            // What we are really doing here: top += control.Bounds.Height;
                            remainingBounds.Y += element.Bounds.Height;
                            remainingBounds.Height -= element.Bounds.Height;
                            break;
                        }

                    case DockStyle.Bottom:
                        {
                            Size elementSize = GetVerticalDockedSize(element, remainingBounds.Size, measureOnly);
                            Rectangle newElementBounds = new(remainingBounds.X, remainingBounds.Bottom - elementSize.Height, elementSize.Width, elementSize.Height);

                            TryCalculatePreferredSizeDockedControl(element, newElementBounds, measureOnly, ref preferredSize, ref remainingBounds);

                            // What we are really doing here: bottom -= control.Bounds.Height;
                            remainingBounds.Height -= element.Bounds.Height;

                            break;
                        }

                    case DockStyle.Left:
                        {
                            Size elementSize = GetHorizontalDockedSize(element, remainingBounds.Size, measureOnly);
                            Rectangle newElementBounds = new(remainingBounds.X, remainingBounds.Y, elementSize.Width, elementSize.Height);

                            TryCalculatePreferredSizeDockedControl(element, newElementBounds, measureOnly, ref preferredSize, ref remainingBounds);

                            // What we are really doing here: left += control.Bounds.Width;
                            remainingBounds.X += element.Bounds.Width;
                            remainingBounds.Width -= element.Bounds.Width;
                            break;
                        }

                    case DockStyle.Right:
                        {
                            Size elementSize = GetHorizontalDockedSize(element, remainingBounds.Size, measureOnly);
                            Rectangle newElementBounds = new(remainingBounds.Right - elementSize.Width, remainingBounds.Y, elementSize.Width, elementSize.Height);

                            TryCalculatePreferredSizeDockedControl(element, newElementBounds, measureOnly, ref preferredSize, ref remainingBounds);

                            // What we are really doing here: right -= control.Bounds.Width;
                            remainingBounds.Width -= element.Bounds.Width;
                            break;
                        }

                    case DockStyle.Fill:
                        {
                            Size elementSize = remainingBounds.Size;
                            Rectangle newElementBounds = new(remainingBounds.X, remainingBounds.Y, elementSize.Width, elementSize.Height);

                            TryCalculatePreferredSizeDockedControl(element, newElementBounds, measureOnly, ref preferredSize, ref remainingBounds);
                        }
                        break;
                }
            }

            return preferredSize;
        }

        private static void TryCalculatePreferredSizeDockedControl(NativeWindow element, Rectangle newElementBounds, bool measureOnly, ref Size preferredSize, ref Rectangle remainingBounds)
        {
            if (measureOnly)
                throw new NotImplementedException();
            else
            {
                //The SetBounds method that WinForms calls has some hooks in it for design mode that we don't need
                element.SetBoundsCore(newElementBounds.X, newElementBounds.Y, newElementBounds.Width, newElementBounds.Height, BoundsSpecified.None);
            }
        }

        private static Size GetHorizontalDockedSize(NativeWindow element, SIZE remainingSize, bool measureOnly)
        {
            Size newSize = xGetDockedSize(element, /* constraints = */ new Size(1, remainingSize.Height));
            if (!measureOnly)
            {
                newSize.Height = remainingSize.Height;
            }
            else
            {
                newSize.Height = Math.Max(newSize.Height, remainingSize.Height);
            }

            Debug.Assert((measureOnly && (newSize.Height >= remainingSize.Height)) || (newSize.Height == remainingSize.Height),
                "Error detected in GetHorizontalDockedSize: Dock size computed incorrectly during layout.");
            return newSize;
        }

        private static Size GetVerticalDockedSize(NativeWindow element, Size remainingSize, bool measureOnly)
        {
            Size newSize = xGetDockedSize(element, /* constraints = */ new Size(remainingSize.Width, 1));
            if (!measureOnly)
            {
                newSize.Width = remainingSize.Width;
            }
            else
            {
                newSize.Width = Math.Max(newSize.Width, remainingSize.Width);
            }

            Debug.Assert((measureOnly && (newSize.Width >= remainingSize.Width)) || (newSize.Width == remainingSize.Width),
                "Error detected in GetVerticalDockedSize: Dock size computed incorrectly during layout.");
            return newSize;
        }

        private static Size xGetDockedSize(NativeWindow nativeWindow, Size constraints)
        {
            if (nativeWindow.AutoSize)

            return nativeWindow.Bounds.Size;
        }
    }
}
