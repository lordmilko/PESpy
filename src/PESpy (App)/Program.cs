using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using PInvoke;
using ReView;

namespace PESpy
{
    internal static class Program
    {
        [STAThread]
        unsafe static void Main()
        {
            //Note that there seems to be an issue with SDK style projects wherein the cursor shows for several seconds when you attempt to debug them in Visual Studio.
            //The selected .NET version doesn't matter

            try
            {
                var mainWindow = new MainWindow
                {
                    Visible = true
                };

                App.MainWindow = mainWindow;

                var hWnd = mainWindow.NativeHandle;

                while (User32.GetMessageW(out var msg, default, default, default))
                {
                    User32.TranslateMessage(msg);
                    User32.DispatchMessageW(msg);
                }
            }
            catch (Exception ex)
            {
                App.FatalError(ex);
            }
        }
    }
}
