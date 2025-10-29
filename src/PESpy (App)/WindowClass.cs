using System;
using System.Runtime.InteropServices;
using PInvoke;

namespace PESpy
{
    internal class WindowClass //Must be a class to prevent the WNDPROC delegate being collected
    {
        public string ClassName { get; }

        public WNDCLASS_STYLES ClassStyle { get; }

        //Prevent the GC from collecting the delegate to the WndProc
        private WNDPROC? _wndProc;

        public WindowClass(string? className, WNDCLASS_STYLES classStyle)
        {
            ClassName = className ?? Guid.NewGuid().ToString();
            ClassStyle = classStyle;

            RegisterClass();
        }

        private unsafe void RegisterClass()
        {
            var wc = new WNDCLASSW();

            //A trick used by WinForms to prevent flicker
            wc.hbrBackground = (HBRUSH) (IntPtr) Gdi32.GetStockObject(GET_STOCK_OBJECT_FLAGS.NULL_BRUSH);
            wc.hCursor = User32.LoadCursorW(default, (PCWSTR) (char*) new IntPtr(32512));
            wc.style = ClassStyle;

            //Prevent the delegate from being GC'd
            _wndProc = WndProc;
            wc.lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_wndProc);
            wc.hInstance = Kernel32.GetModuleHandleW((PCWSTR) null);

            var name = ClassName;

            fixed (char* c = name)
            {
                wc.lpszClassName = c;

                if (User32.RegisterClassW(wc) == 0)
                    throw new NotImplementedException();
            }
        }

        private unsafe LRESULT WndProc(HWND hWnd, int uMsg, WPARAM wParam, LPARAM lParam)
        {
            /* When we register a window class, we must specify the window procedure to use
             * for that class. However, once an instance of the class is actually created,
             * we need a way to refer to "this" and manage the state of the window instance.
             * There are two ways to do that
             * 1. Stash a pointer to "this" in the GWLP_USERDATA. This technique is commonly used in
             *    native code
             * 2. Update the WndProc of the window instance to refer to a bethod that belongs to "this".
             *    This technique is the way to go in managed code
             */

            var msg = (WM) uMsg;

            //The first message can be WM_GETMINMAXINFO
            if (msg != WM.WM_NCCREATE)
                return User32.DefWindowProcW(hWnd, uMsg, wParam, lParam);

            var pCreateStruct = (CREATESTRUCTW*) (IntPtr) lParam;

            var window = (Window) GCHandle.FromIntPtr((IntPtr) pCreateStruct->lpCreateParams).Target!;

            //Replace the class WndProc with the instance WndProc
            window.InstallWndProc(hWnd);

            //Call the instance WndProc. The class WndProc won't be called again after this
            return window.WndProc(hWnd, uMsg, wParam, lParam);
        }
    }
}
