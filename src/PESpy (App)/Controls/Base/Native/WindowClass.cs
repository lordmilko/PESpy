using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using PInvoke;

namespace PESpy.Controls
{
    internal class WindowClass //Must be a class to prevent the WNDPROC delegate being collected
    {
        private static IntPtr s_defWndProc;

        public string? ClassName { get; }

        public string UniqueClassName { get; private set; }

        public WNDCLASS_STYLES ClassStyle { get; }

        //Prevent the GC from collecting the delegate to the WndProc
        private WNDPROC? _wndProc;
        private IntPtr _defWndProc; //Either actual DefWndProc or the underlying proc for the system control that we're subclassing

        private static List<WindowClass> registeredClasses = new();

        static WindowClass()
        {
            var user32 = Kernel32.GetModuleHandleW("user32.dll");

            s_defWndProc = Kernel32.GetProcAddress(user32, "DefWindowProcW");
        }

        internal static WindowClass FindOrCreate(string? className, WNDCLASS_STYLES classStyle)
        {
            WindowClass wc = null;

            if (className == null)
            {
                //This is a custom window. WinForms will look for an existing window class with the same style.
                //We're not going to do that
            }
            else
            {
                foreach (var windowClass in registeredClasses)
                {
                    if (windowClass.ClassName == className)
                        wc = windowClass;
                }
            }

            if (wc == null)
            {
                wc = new WindowClass(className, classStyle);
                registeredClasses.Add(wc);
            }

            return wc;
        }

        private WindowClass(string? className, WNDCLASS_STYLES classStyle)
        {
            ClassName = className;
            ClassStyle = classStyle;

            RegisterClass();
        }

        private unsafe void RegisterClass()
        {
            WNDCLASSW wc = default;

            if (ClassName == null)
            {
                //This is a custom control

                //A trick used by WinForms to prevent flicker
                wc.hbrBackground = (HBRUSH) (IntPtr) Gdi32.GetStockObject(GET_STOCK_OBJECT_FLAGS.NULL_BRUSH);
                wc.hCursor = User32.LoadCursorW(default, (PCWSTR) (char*) new IntPtr(32512));
                wc.style = ClassStyle;

                _defWndProc = s_defWndProc;
            }
            else
            {
                //This is a system control, and we're going to subclass it

                if (!User32.GetClassInfoW(default, ClassName, out wc))
                    throw new InvalidOperationException($"Failed to get info for class '{ClassName}'");

                _defWndProc = wc.lpfnWndProc;
            }            

            //If the caller is asking to use global style, it means they want this to be hostable in things like WinForms, where the class will behave like
            //a native control. Native controls don't specify their hInstance and use CS_GLOBALCLASS, and so WinForms won't specify a hInstance when it
            //asks for them
            if ((ClassStyle & WNDCLASS_STYLES.CS_GLOBALCLASS) == 0)
            {
                wc.hInstance = Kernel32.GetModuleHandleW((PCWSTR) null);

                //Prevent the delegate from being GC'd
                _wndProc = WndProc;
            }
            else
            {
                //Prevent the delegate from being GC'd
                _wndProc = WndProc_Hosted;
            }

            wc.lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_wndProc);

            UniqueClassName = GetUniqueClassName();

            fixed (char* c = UniqueClassName)
            {
                wc.lpszClassName = c;

                if (User32.RegisterClassW(wc) == 0)
                    throw new NotImplementedException();
            }
        }

        private string GetUniqueClassName()
        {
            if (ClassName == null)
                return Guid.NewGuid().ToString();

            return $"{ClassName}_{Guid.NewGuid()}";
        }

        private unsafe LRESULT WndProc_Hosted(HWND hWnd, int uMsg, WPARAM wParam, LPARAM lParam)
        {
            /* If we're being hosted in WinForms, they've subclassed _us_, which means the current WndProc is actually _them_. If we
             * install our hooked window procedure as the WndProc, and defer to the default DefWndProc, we've now completely cut WinForms
             * out of the picture! Woops! WinForms already captured the fact that this is the previous window procedure, so our options
             * are two-fold:
             * 
             * 1. Get the current WndProc (which is WinForms' WndProc) and make it our DefWndProc, and install ourselves as the primary WndProc
             * 2. Install a GCHandle to our window in user data, and always relay to the hooked window whenever we get a message
             * 
             * #1 does not work, because if you send a message to WinForms it's going to want to send it right back to us if it wants to forward
             * to the DefWndProc. so the only option is #2
             */

            var msg = (WM) uMsg;

            var pGCHandle = User32.GetWindowLongPtr(hWnd, WINDOW_LONG_PTR_INDEX.GWLP_USERDATA);

            NativeWindow window;

            if (pGCHandle != default)
            {
                window = (NativeWindow) GCHandle.FromIntPtr(pGCHandle).Target!;

                if (window == null)
                    return (LRESULT) ((delegate* unmanaged[Stdcall]<nint, int, nint, nint, nint>) _defWndProc)(hWnd, uMsg, wParam, lParam);

                return window.WndProc(hWnd, uMsg, wParam, lParam);
            }

            //The first message can be WM_GETMINMAXINFO
            if (msg != WM.WM_NCCREATE)
                return (LRESULT) ((delegate* unmanaged[Stdcall]<nint, int, nint, nint, nint>) _defWndProc)(hWnd, uMsg, wParam, lParam);

            var pCreateStruct = (CREATESTRUCTW*) (IntPtr) lParam;

            pGCHandle = *(IntPtr*) pCreateStruct->lpCreateParams;

            //WinForms passes an indirected pointer, so we need to make sure we do the same to allow hosting
            window = (NativeWindow) GCHandle.FromIntPtr(pGCHandle).Target!;

            User32.SetWindowLongPtr(hWnd, WINDOW_LONG_PTR_INDEX.GWLP_USERDATA, pGCHandle);

            window.InstallHostedWndProc(hWnd, _defWndProc);

            return window.WndProc(hWnd, uMsg, wParam, lParam);
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
                return (LRESULT) ((delegate* unmanaged[Stdcall]<nint, int, nint, nint, nint>) _defWndProc)(hWnd, uMsg, wParam, lParam);

            var pCreateStruct = (CREATESTRUCTW*) (IntPtr) lParam;

            //WinForms passes an indirected pointer, so we need to make sure we do the same to allow hosting
            var window = (NativeWindow) GCHandle.FromIntPtr(*(IntPtr*) pCreateStruct->lpCreateParams).Target!;
            //Replace the class WndProc with the instance WndProc
            window.InstallWndProc(hWnd, _defWndProc);

            //Call the instance WndProc. The class WndProc won't be called again after this
            return window.WndProc(hWnd, uMsg, wParam, lParam);
        }
    }
}
