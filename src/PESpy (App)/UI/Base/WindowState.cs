using System;

namespace PESpy.UI
{
    /// <summary>
    /// Describes boolean states that a given <see cref="NativeWindow"/> may be in.
    /// </summary>
    [Flags]
    public enum WindowState
    {
        TrackMouseEvent = 1,
        Visible = 2, //We want to be visible. If our parent is not visible, we won't actually be visible
        TopLevel = 4,
        AutoSize = 8,
        LayoutIsDirty = 16, //Shenanigans related to how WinForms manages layout
        LayoutDeferred = 32,
        CreatingHandle = 64
    }
}
