using System;
using PInvoke;

namespace PESpy
{
    internal static class Program
    {
        [STAThread]
        unsafe static void Main()
        {
            /* In WinForms, there's several distinct but related properties pertaining to the bounds of a control
             * 
             * In a Form, the codebehind might say the size is 800, 450. However, the ClientSize does not actually display
             * in the designer: rather, only the Size property is shown. Windows automatically seems to perform DPI scaling,
             * so if you specify you want a width of 800, the actual size that you'll get will be slightly less than that.
             * I'm not sure if this is perhaps because there's actually a bunch of shadows hanging off the edge of the window,
             * which eat into your available space. To account for this, the WinForms designer will show you the "true" width
             * of the window, but will then convert this into "ClientSize" space. WinForms takes the client size and applies
             * user32!AdjustWindowRectEx to it in order to convert this logical size into a physical size, ensuring that the
             * actual amount of available client space matches what was requested
             * 
             * Width/Height
             * - ClientSize
             * - Size
             * 
             * X/Y
             * - DesktopLocation
             * - Location
             * 
             * Rectangle
             * - ClientRectangle
             * - DisplayRectangle
             * - Bounds
             * - DesktopBounds
             */

            //Note that there seems to be an issue with SDK style projects wherein the cursor shows for several seconds when you attempt to debug them in Visual Studio.
            //The selected .NET version doesn't matter
            App.MainForm = new MainForm();

            User32.ShowWindow(App.MainForm.hWnd, SHOW_WINDOW_CMD.SW_SHOW);

            while (User32.GetMessageW(out var msg, default, default, default))
            {
                User32.TranslateMessage(msg);
                User32.DispatchMessageW(msg);
            }
        }
    }
}
