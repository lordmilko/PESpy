#if WINFORMS
using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using PInvoke;

namespace PESpy.Controls
{
    /// <summary>
    /// Allows hosting a Win32 <see cref="Window"/> inside a WinForms <see cref="Control"/>.
    /// </summary>
    public class NativeToWinFormsHostControl : UserControl
    {
        protected override System.Windows.Forms.CreateParams CreateParams
        {
            get
            {
                if (App.IsInDesignMode(this))
                    return base.CreateParams;

                var p = base.CreateParams;

                p.Style |= (int) (WINDOW_STYLE.WS_CHILD | WINDOW_STYLE.WS_VISIBLE);

                if (Parent != null)
                    p.Parent = Parent.Handle;

                p.ClassName = _className;

                if (_gcHandle == null)
                {
                    //Once the handle has been free'd, it will be cleared, but the _window will still remain,
                    //which will protect us from re-creating the handle
                    if (_window == null)
                    {
                        _window = CreateWindow();
                        _gcHandle = GCHandle.Alloc(_window);
                    }
                }

                //This just returns the internal _handle field
                if (_gcHandle != null)
                    p.Param = GCHandle.ToIntPtr(_gcHandle.Value);

                return p;
            }
        }

        private static string _className;
        private static WindowClass _windowClass;

        private GCHandle? _gcHandle;
        protected NativeWindow? _window;

        static NativeToWinFormsHostControl()
        {
            //Create a dummy window class capable of dispatching to our window
            _windowClass = WindowClass.FindOrCreate(null, WNDCLASS_STYLES.CS_GLOBALCLASS);
            _className = _windowClass.UniqueClassName;
        }

        protected NativeToWinFormsHostControl()
        {
            //By default, Control's ctor sets UserPaint to true, which causes all painting to be redirected to
            //Control.WmPaint (unless overridden). We want to do all painting natively, so it's very important
            //that we clear this, else we won't get any paint messages!
            SetStyle(ControlStyles.UserPaint, false);
        }

        public NativeToWinFormsHostControl(NativeWindow window) : this()
        {
            _window = window;
            _gcHandle = GCHandle.Alloc(window);
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            base.OnHandleDestroyed(e);

            if (_gcHandle != null)
            {
                _gcHandle.Value.Free();
                _gcHandle = default;
            }
        }

        //If the caller passes in a window directly, use that. Otherwise,
        //derived classes are expected to override this method
        protected virtual NativeWindow CreateWindow() => _window!;
    }
}
#endif
