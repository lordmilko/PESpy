using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using PInvoke;
using static PInvoke.Macros;

namespace PESpy.UI
{
    /// <summary>
    /// Represents a lightweight window capable of hosting a HWND and processing window messages.
    /// </summary>
    public abstract class NativeWindow
    {
        /// <summary>
        /// Gets the <see cref="HINSTANCE"/> that identifies the current module, that the HWND this class creates
        /// will be associated with.
        /// </summary>
        protected static HINSTANCE hInstance = Kernel32.GetModuleHandleW((PCWSTR) null);

        public NativeWindow()
        {
            //Default flags that should be true for all controls. Derived types may explicitly
            //clear these values to indicate that actually they should not be true by default
            //(e.g. a Form may not want to be visible by default)
            _windowState = WindowState.Enabled | WindowState.Visible;
        }

        #region Bookkeeping

        //General bookkeeping specific to the NativeWindow type itself

        private int _updateCount; //Number of times WM_SETREDRAW has been disabled
        private int _layoutSuspendCount; //Number of times layout has been suspended

        private static int s_globalLayoutSuspendCount;

        //Stores boolean flags required for various purposes
        private WindowState _windowState;

        protected bool HasState(WindowState state) => (_windowState & state) != 0;

        protected void SetState(WindowState state, bool value) =>
            _windowState = value ? _windowState | state : _windowState & ~state;

        private NativeWindowCollection? _children;

        /// <summary>
        /// Gets the child windows contained within this window.
        /// </summary>
        public NativeWindowCollection Children => _children ??= new NativeWindowCollection(this);

        #endregion
        #region Window

        //Code pertaining to the setup and management of the window itself

        internal bool IsHookInstalled => _defWndProc != default;

        public bool Focused => User32.GetFocus() == hWnd;

        public bool Border { get; set; }

        public NativeMenu? Menu { get; protected set; }

        #region Install

#if HOSTED
        //If we're hosting a control inside a NativeToWinFormsHost, if something in our NativeWindow world tries to create the underlying
        //hWnd, we need to tell the outer NativeToWinFormsHost that it's time for it to created its handle, and for _them_ to be the one
        //to create the handle, not us
        internal Action CreateHandleHook;
#endif

        internal unsafe void InstallWndProc(HWND hWnd, IntPtr defWndProc)
        {
            Debug.Assert(!IsHandleCreated);
            _hWnd = hWnd;
            Debug.Assert(_wndProc == null);
            _wndProc = WndProc;
            _defWndProc = defWndProc;

            var pWndProc = Marshal.GetFunctionPointerForDelegate(_wndProc);
            User32.SetWindowLongPtr(hWnd, WINDOW_LONG_PTR_INDEX.GWL_WNDPROC, pWndProc);
        }
        #region hWnd

        private HWND _hWnd;

        public HWND hWnd
        {
            get
            {
                if (_hWnd.IsNull)
                    CreateHandle();

                return _hWnd;
            }
        }

        public bool IsHandleCreated => !_hWnd.IsNull;
        protected virtual unsafe void CreateHandle()
        {
#if HOSTED
            if (CreateHandleHook != null)
            {
                CreateHandleHook();
                return;
            }
#endif

            //Requesting CreateParams may trigger the current control to force load the handle of its parent
            //which may in turn try and create the handle of the current child! Thus, we need to protect against
            //any recursion
            if (HasState(WindowState.CreatingHandle))
                return;

            SetState(WindowState.CreatingHandle, true);

            var createParams = CreateParams;

            var @class = WindowClass.FindOrCreate(createParams.ClassName, createParams.ClassStyle);

            var gcHandle = GCHandle.Alloc(this);
            var param = GCHandle.ToIntPtr(gcHandle);

            var menu = Menu;

            try
            {
                var hWnd = User32.CreateWindowExW(
                    createParams.ExStyle,
                    @class!.UniqueClassName,
                    createParams.Caption,
                    (WINDOW_STYLE) createParams.Style,
                    createParams.X,
                    createParams.Y,
                    createParams.Width,
                    createParams.Height,
                    createParams.Parent,
                    menu != null ? menu.Value.hMenu : HMENU.Null,
                    hInstance,
                    lpParam: (void*) (&param) //WinForms passes an indirected pointer, so we do the same to allow hosting within WinForms
                );

                if (hWnd.IsNull)
                {
                    throw new Win32Exception(); //This will get the last error
                }

                _hWnd = hWnd;
            }
            finally
            {
                SetState(WindowState.CreatingHandle, false);
                gcHandle.Free();
            }
        }

        #endregion
        #region Parent

        private NativeWindow? _parent;

        public NativeWindow? Parent
        {
            get => _parent;
            set
            {
                if (_parent == value)
                    return;

                /* Windows can be connected together by either setting the Parent on the child,
                 * or adding a child to the parent. Either way, the way that they're actually glued
                 * together is that both pathways lead to doing Children.Add(), which then calls
                 * the internal AssignParent method to do the actual work of hooking us up */

                //Add or remove this node from the parent's children. This will do the work of clearing out _parent

                //If we add support for this we would first do _parent.Children.Remove(this);
                Debug.Assert(value != null, "Detaching the current parent is not supported");
                Debug.Assert(_parent == null, "Changing parents is not supported");

                value.Children.Add(this);
            }
        }

        internal virtual void AssignParent(NativeWindow newParent)
        {
            _parent = newParent;
        }

        #endregion
        #region Text

        private string? _text;

        public string? Text
        {
            get => _text;
            set
            {
                if (_text == value)
                    return;

                if (IsHandleCreated)
                {
                    User32.SetWindowTextW(hWnd, value);
                }

                _text = value;
            }
        }

        #endregion
        #region WndProc

        //Stores the instance WndProc that replaces the class WndProc
        private WNDPROC? _wndProc;
        private IntPtr _defWndProc;

        internal unsafe LRESULT WndProc(HWND hWnd, int uMsg, WPARAM wParam, LPARAM lParam)
        {
            var m = new Message(hWnd, (WM) uMsg, wParam, lParam);

            try
            {
                //NativeAOT does not allow stack unwinding through reverse P/Invoke frames, which means if we throw an exception
                //inside our WndProc, it won't propagate up to the top level exception handler. As such, we need to register
                //an exception handler for the App to be called in the event an unhandled exception occurs
                //https://github.com/dotnet/runtime/blob/3b637728da9d5dc1dc625640bb5d7e436cf0f12a/src/coreclr/nativeaot/Runtime.Base/src/System/Runtime/ExceptionHandling.cs#L762

                WndProc(ref m);

                return m.Result;
            }
            catch (Exception ex)
            {
                App.RaiseFatalError(ex);

                return default;
            }
        }

        protected virtual unsafe void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                #region Lifecycle

                case WM.WM_CREATE:
                    WmCreate(ref m, (CREATESTRUCTW*) (IntPtr) m.lParam);
                    break;

                case WM.WM_NCDESTROY:
                    WmNcDestroy(ref m);
                    break;

                case WM.WM_ACTIVATE:
                    WmActivate(ref m);
                    break;

                #endregion
                #region Focus

                case WM.WM_SETFOCUS:
                    WmSetFocus(ref m);
                    break;

                case WM.WM_KILLFOCUS:
                    WmKillFocus(ref m);
                    break;

                #endregion
                #region Paint

                case WM.WM_PAINT:
                    WmPaint(ref m);
                    break;

                case WM.WM_ERASEBKGND:
                    m.Result = 1;
                    break;

                case WM.WM_NCPAINT:
                    WmNcPaint(ref m);
                    break;

                case WM.WM_DRAWITEM:
                    WmDrawItem(ref m, (nint) m.wParam, (DRAWITEMSTRUCT*) (nint) m.lParam); //Not sure what the type of the wParam is
                    break;

                case WM.WM_MEASUREITEM:
                    WmMeasureItem(ref m, (uint) m.wParam, (MEASUREITEMSTRUCT*) (nint) m.lParam);
                    break;

                #endregion
                #region Layout

                case WM.WM_SHOWWINDOW:
                    WmShowWindow(ref m, m.wParam, m.lParam);
                    break;

                case WM.WM_VSCROLL:
                    Debug.Assert(m.lParam == default);
                    WmVScroll(ref m, (SB) LOWORD((uint) m.wParam));
                    break;

                case WM.WM_HSCROLL:
                    Debug.Assert(m.lParam == default);
                    WmHScroll(ref m, (SB) LOWORD((uint) m.wParam));
                    break;

                case WM.WM_WINDOWPOSCHANGED:
                    WmWindowPosChanged(ref m);
                    break;

                #endregion
                #region Mouse

                case WM.WM_LBUTTONDOWN:
                case WM.WM_RBUTTONDOWN:
                    WmMouseDown(ref m, (short) LOWORD(m.lParam), (short) HIWORD(m.lParam));
                    break;

                case WM.WM_LBUTTONUP:
                case WM.WM_RBUTTONUP:
                    WmMouseUp(ref m, (short) LOWORD(m.lParam), (short) HIWORD(m.lParam));
                    break;

                case WM.WM_MOUSEMOVE:
                    HookMouseEvent();
                    WmMouseMove(ref m, (short) LOWORD(m.lParam), (short) HIWORD(m.lParam));
                    break;

                case WM.WM_MOUSELEAVE:
                    UnhookMouseEvent();
                    WmMouseLeave(ref m);
                    break;

                case WM.WM_MOUSEWHEEL:
                    //WinForms Control.ControlNativeWindow.WndProc does this for WM_MOUSEWHEEL
                    if (HasState(WindowState.TrackMouseEvent))
                    {
                        UnhookMouseEvent();
                        HookMouseEvent();
                    }

                    WmMouseWheel(ref m, (short) HIWORD((uint) m.wParam));
                    break;

                case WM.WM_MOUSEHOVER:
                    WmMouseHover(ref m);
                    break;

                #endregion
                #region Keyboard

                case WM.WM_KEYDOWN:
                case WM.WM_SYSKEYDOWN:
                case WM.WM_CHAR:
                    WmKeyDown(ref m, (VIRTUAL_KEY) (int) m.wParam);
                    break;

                case WM.WM_KEYUP:
                case WM.WM_SYSKEYUP:
                    WmKeyUp(ref m, (VIRTUAL_KEY) (int) m.wParam);
                    break;

                #endregion
                #region Event

                case WM.WM_COMMAND:
                    WmCommand(ref m, HIWORD((uint) m.wParam), LOWORD((uint) m.wParam), (nint) m.lParam);
                    break;

                case WM.WM_NOTIFY:
                    WmNotify(ref m, (NMHDR*) (nint) m.lParam);
                    break;

                //WM_REFLECT_NOTIFY is essentially a "pseudo-message" caused by a parent window receiving
                //a WM_NOTIFY event, and it then "reflecting it" back towards its child
                case WM.WM_REFLECT_NOTIFY:
                    WmReflectNotify(ref m, (NMHDR*) (nint) m.lParam);
                    break;

                case WM.WM_TIMER:
                    WmTimer(ref m);
                    break;

                #endregion

                default:
                    DefWndProc(ref m);
                    break;
            }
        }

        protected unsafe void DefWndProc(ref Message m)
        {
            /* Apparently you're meant to call CallWindowProcW for subclassed windows rather than just
             * invoke the function pointer directly. The documentation for CallWindowProcW says
             * 
             *     If this value is obtained by calling the GetWindowLong function with the nIndex parameter set to
             *     GWL_WNDPROC or DWL_DLGPROC, it is actually either the address of a window or dialog box procedure,
             *     or a special internal value meaningful only to CallWindowProc.
             * 
             * I'm not sure under what circumstances we would receive one of these "special internal values", but in any case
             * it does sound like that's sufficient reason for us to not just call the function pointer directly
             */

            m.Result = User32.CallWindowProcW((delegate* unmanaged[Stdcall]<HWND, int, WPARAM, LPARAM, LRESULT>) _defWndProc, m.hWnd, (int) m.Msg, m.wParam, m.lParam);
        }

        #endregion
        #endregion
        #region Lifecycle

        protected virtual unsafe void WmCreate(ref Message m, CREATESTRUCTW* pCreateStruct)
        {
            //Remove the dotted line around the control when focused
            User32.SendMessageW(m.hWnd, (int) WM.WM_CHANGEUISTATE, Macros.MAKELONG((ushort) UIS.UIS_SET, (ushort) UISF.UISF_HIDEFOCUS), 0);

            DefWndProc(ref m);

            UpdateBounds();

            OnHandleCreated();
        }

        private void WmNcDestroy(ref Message m)
        {
            //WinForms does not handle WM_NCDESTROY, only WM_DESTROY, wherein it calls OnHandleDestroyed.
            OnHandleDestroyed();

            Menu?.Dispose();

            User32.PostQuitMessage(0);
            DefWndProc(ref m);
        }

        protected virtual void WmActivate(ref Message m) => DefWndProc(ref m);

        internal void CreateControl()
        {
            if (!Visible)
                return;

            if (!IsHandleCreated)
                CreateHandle();

            if (_children != null)
            {
                //Create all child handles as well
                foreach (var child in _children)
                    child.CreateControl();
            }
        }

        protected virtual void OnHandleCreated()
        {
            HandleCreated?.Invoke();
        }

        protected virtual void OnHandleDestroyed()
        {
        }

        #endregion
        #region Focus

        protected virtual void WmSetFocus(ref Message m) => DefWndProc(ref m);

        protected virtual void WmKillFocus(ref Message m) => DefWndProc(ref m);

        #endregion
        #region Paint

        //F0F0F0
        internal static HBRUSH DefaultBackgroundBrush = User32.GetSysColorBrush(SYS_COLOR_INDEX.COLOR_3DFACE);

        internal unsafe void Invalidate(bool invalidateChildren = false)
        {
            //Per WinForms, it's safe to call InvalidateRect across threads
            if (IsHandleCreated)
            {
                if (invalidateChildren)
                    User32.RedrawWindow(hWnd, (RECT*) default, default, REDRAW_WINDOW_FLAGS.RDW_INVALIDATE | REDRAW_WINDOW_FLAGS.RDW_ERASE | REDRAW_WINDOW_FLAGS.RDW_ALLCHILDREN);
                else
                    User32.InvalidateRect(hWnd, (RECT*) default, true);
            }
        }

        internal void Invalidate(RECT rect)
        {
            if (rect.IsEmpty)
                Invalidate();
            else if (IsHandleCreated)
            {
                //WinForms says this is safe to do from any thread

                User32.InvalidateRect(hWnd, rect, true);
            }
        }

        protected void BeginUpdate()
        {
            if (!IsHandleCreated)
                return;

            if (_updateCount == 0)
                User32.SendMessageW(hWnd, (int) WM.WM_SETREDRAW, 0, default);

            _updateCount++;
        }

        protected void EndUpdate()
        {
            if (_updateCount > 0)
            {
                _updateCount--;

                if (_updateCount == 0)
                    User32.SendMessageW(hWnd, (int) WM.WM_SETREDRAW, 1, default);
            }
        }

        protected virtual void WmPaint(ref Message m)
        {
            User32.BeginPaint(hWnd, out var ps);
            WmPaint(ps.hdc);
            User32.EndPaint(hWnd, ps);
        }

        protected virtual void WmPaint(HDC hdc)
        {
            //No default painting
        }

        protected virtual void WmNcPaint(ref Message m) => DefWndProc(ref m);

        protected unsafe virtual void WmDrawItem(ref Message m, nint id, DRAWITEMSTRUCT* drawItemStruct) => DefWndProc(ref m);

        protected unsafe virtual void WmMeasureItem(ref Message m, uint ctlId, MEASUREITEMSTRUCT* measureItemStruct) => DefWndProc(ref m);

        #endregion
        #region Layout
        #region X / Left

        private int _x;

        public int Left
        {
            get => _x;
            set => SetBounds(value, _y, _width, _height, BoundsSpecified.X);
        }

        #endregion
        #region Y / Top

        private int _y;

        public int Top
        {
            get => _y;
            set => SetBounds(_x, value, _width, _height, BoundsSpecified.Y);
        }

        #endregion
        #region Width / Right

        private int _width;

        public int Width
        {
            get => _width;
            set => SetBounds(_x, _y, value, _height, BoundsSpecified.Width);
        }

        public int Right => _x + _width;

        #endregion
        #region Height / Bottom

        private int _height;

        public int Height
        {
            get => _height;
            set => SetBounds(_x, _y, _width, value, BoundsSpecified.Height);
        }

        public int Bottom => _y + _height;

        #endregion
        #region ClientSize

        //Set by SetClientSizeCore / UpdateBounds
        private int _clientWidth;
        private int _clientHeight;

        public SIZE ClientSize
        {
            get => new SIZE(_clientWidth, _clientHeight);
            set => SetClientSizeCore(value);
        }

        private void SetClientSizeCore(SIZE size)
        {
            Size = SizeFromClientSize(size);
            _clientWidth = size.cx;
            _clientHeight = size.cy;
        }
        #region Size

        public SIZE Size
        {
            get => new SIZE(_width, _height);
            set => SetBounds(_x, _y, value.Width, value.Height, BoundsSpecified.Size);
        }

        #endregion
        #region Location

        public POINT Location
        {
            get => new POINT(_x, _y);
            set => SetBounds(value.x, value.y, _width, _height, BoundsSpecified.Location);
        }

        #endregion
        #region Dock

        private DockStyle _dock;

        public DockStyle Dock
        {
            get => _dock;
            set
            {
                if (value != Dock)
                {
                    SuspendLayout();
                    DefaultLayout.SetDock(this, value);
                    _dock = value;
                    ResumeLayout();
                }
            }
        }

        #endregion
        #region Enabled

        public bool Enabled
        {
            get
            {
                if (!HasState(WindowState.Enabled))
                    return false;

                if (Parent == null)
                    return true;

                return Parent.Enabled;
            }
            set
            {
                var oldValue = Enabled;

                SetState(WindowState.Enabled, value);

                if (oldValue != value)
                    OnEnabledChanged();
            }
        }

        private void OnEnabledChanged()
        {
            if (IsHandleCreated)
            {
                User32.EnableWindow(hWnd, Enabled);
            }
        #region Visible

        public bool Visible
        {
            get
            {
                //There's two aspects to visibility: whether _we_ want to be visible,
                //and whether we _are_ actually visible, based on whether or not our
                //parent is actually visibles

                if (!HasState(WindowState.Visible))
                    return false;

                return Parent == null || Parent.Visible;
            }
            set => SetVisibleCore(value);
        }
                //Not sure why WinForms handles this in a funny way
                var fireEvent = false;

                if (HasState(WindowState.TopLevel))
                {
                    //WinForms has a virtual ShowParams property, which among other things is overridden in a Form
                    //to allow showing minimized or maximized (not sure if that relates to the show mode specified
                    //in the STARTUPINFO when the process is launched
                    if (IsHandleCreated || value)
                        User32.ShowWindow(hWnd, value ? SHOW_WINDOW_CMD.SW_SHOW : SHOW_WINDOW_CMD.SW_HIDE); //This will handle updating the Visible status
                }
                else if (IsHandleCreated && value && Parent?.IsHandleCreated == true)
                {
                    SetState(WindowState.Visible, value);
                    fireEvent = true;

                    if (value)
                        CreateControl();

                    User32.SetWindowPos(hWnd, default, 0, 0, 0, 0, SET_WINDOW_POS_FLAGS.SWP_NOSIZE | SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOZORDER | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE | (value ? SET_WINDOW_POS_FLAGS.SWP_SHOWWINDOW : SET_WINDOW_POS_FLAGS.SWP_HIDEWINDOW));
                }

                if (value != Visible)
                {
                    SetState(WindowState.Visible, value);
                    fireEvent = true;
                }

                if (fireEvent)
                {
                    OnVisibleChanged();
                    Parent?.PerformLayout();
                }
            }
        }

        protected virtual void OnVisibleChanged()
        {
            var visible = Visible;

            if (visible)
                UnhookMouseEvent();

            foreach (var child in Children)
            {
                if (child.Visible)
                    child.OnParentVisibleChanged();

                if (!visible)
                    child.OnParentBecameInvisible();
            }
        }

        private void OnParentVisibleChanged()
        {
            if (HasState(WindowState.Visible))
                OnVisibleChanged();
        }

        private void OnParentBecameInvisible()
        {
            if (HasState(WindowState.Visible))
            {
                foreach (var child in Children)
                    child.OnParentBecameInvisible();
            }
        }

        #endregion
        #region ClientRectangle / Bounds

        public RECT ClientRectangle => new RECT(0, 0, _clientWidth, _clientHeight);

        public RECT Bounds
        {
            get => new RECT(_x, _y, _x + _width, _y + _height);
            set => SetBounds(value.X, value.Y, value.Width, value.Height, BoundsSpecified.All);
        }

        //WinForms has a whole convoluted property system. We don't need that for storing simple boolean values
        public bool AutoSize => HasState(WindowState.AutoSize);

        private void SetBounds(int x, int y, int width, int height, BoundsSpecified specified)
        {
            //For any values not specified by the caller, use the existing values stored on the window
            if ((specified & BoundsSpecified.X) != BoundsSpecified.X)
                x = _x;

            if ((specified & BoundsSpecified.Y) != BoundsSpecified.Y)
                y = _y;

            if ((specified & BoundsSpecified.Width) != BoundsSpecified.Width)
                width = _width;

            if ((specified & BoundsSpecified.Height) != BoundsSpecified.Height)
                height = _height;

            //Only do something if any of the values have changed

            if (_x != x || _y != y || _width != width || _height != height)
            {
                SetBoundsCore(x, y, width, height, specified);
                DoLayout(Parent, this);
            }
            else
            {
                //WinForms updates the scaling, but it doesn't seem like we ever used that
            }
        }

        internal void SetBoundsCore(int x, int y, int width, int height, BoundsSpecified specified)
        {
            if (_x == x && _y == y && _width == width && _height == height)
                return;

            //WinForms updates the "specified bounds" which enables remembering the old bounds e.g. when you toggle
            //between dock fill and back to normal. We don't need that. It then calls ApplyBoundsConstraints which
            //gives the control an opportunity to apply a minimum or maximum size

            if (IsHandleCreated)
            {
                //We need to update the existing control

                var flags = SET_WINDOW_POS_FLAGS.SWP_NOZORDER | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE;

                if (_x == x && _y == y)
                    flags |= SET_WINDOW_POS_FLAGS.SWP_NOMOVE;

                if (_width == width && _height == height)
                    flags |= SET_WINDOW_POS_FLAGS.SWP_NOSIZE;

                User32.SetWindowPos(hWnd, default, x, y, width, height, flags);
            }
            else
            {
                //Just update our bookkeeping
                UpdateBounds(x, y, width, height);
            }
        private unsafe void UpdateBounds()
        {
            User32.GetClientRect(hWnd, out var rect);

            var clientWidth = rect.right; //Left would be 0
            var clientHeight = rect.bottom; //Top would be 0

            User32.GetWindowRect(hWnd, out rect);

            if (!HasState(WindowState.TopLevel))
            {
                User32.MapWindowPoints(default, User32.GetParent(hWnd), (POINT*) (&rect), 2);
            }

            UpdateBounds(rect.left, rect.top, rect.Width, rect.Height, clientWidth, clientHeight);
        }

        internal virtual void UpdateBounds(int x, int y, int width, int height)
        {
            //Need to calculate the client width and height

            RECT rect = default;

            var createParams = CreateParams;
            var clientWidth = width - rect.Width;
            var clientHeight = height - rect.Height;

            UpdateBounds(x, y, width, height, clientWidth, clientHeight);
        }

        private void UpdateBounds(int x, int y, int width, int height, int clientWidth, int clientHeight)
        {
            var newLocation = _x != x || _y != y;
            var newSize = Width != width || Height != height || _clientWidth != clientWidth || _clientHeight != clientHeight;

            _x = x;
            _y = y;
            _width = width;
            _height = height;
            _clientWidth = clientWidth;
            _clientHeight = clientHeight;

            //If newLocation, WinForms calls OnLocationChanged

            if (newSize)
        internal void SuspendLayout()
        {
            _layoutSuspendCount++;
        }

        internal static void SuspendGlobalLayout()
        {
            s_globalLayoutSuspendCount++;
        }

        internal void ResumeLayout() => ResumeLayout(true);

        //Does not provide the option of performing layout on resume
        internal static void ResumeGlobalLayout()
        {
            s_globalLayoutSuspendCount--;
        }

        internal void ResumeLayout(bool performLayout)
        {
            if (_layoutSuspendCount > 0)
            {
                _layoutSuspendCount--;

                if (_layoutSuspendCount == 0 && HasState(WindowState.LayoutDeferred) && performLayout)
                    PerformLayout();
            }
        private void PerformLayout() => PerformLayout(null);

        private void PerformLayout(NativeWindow? affectedControl)
        {
            if (_layoutSuspendCount > 0 || s_globalLayoutSuspendCount > 0)
            {
                SetState(WindowState.LayoutDeferred, true);

                return;
            }

            _layoutSuspendCount = 1;

            OnLayout();

            SetState(WindowState.LayoutDeferred | WindowState.LayoutIsDirty, false);
            _layoutSuspendCount = 0;

            if (Parent != null && Parent.HasState(WindowState.LayoutIsDirty))
                DoLayout(Parent, this);
        }

        protected virtual void OnLayout()
        {
            if (DefaultLayout.Instance.Layout(this) && Parent != null)
                SetState(WindowState.LayoutIsDirty, true);
        }

        #endregion

        protected virtual void WmShowWindow(ref Message m, bool show, int status)
        {
            DefWndProc(ref m);

            if (show)
            {
                SetState(WindowState.Visible, true);
                CreateControl();
                OnVisibleChanged();
            }
            else
            {
                if (HasState(WindowState.TopLevel))
                {
                    SetState(WindowState.Visible, false);
                    OnVisibleChanged();
                }
            }
        }

        protected virtual void WmVScroll(ref Message m, SB sb) => DefWndProc(ref m);

        protected virtual void WmHScroll(ref Message m, SB sb) => DefWndProc(ref m);

        protected virtual void WmWindowPosChanged(ref Message m)
        #endregion
        #region Mouse

        public bool Capture
        {
            get => IsHandleCreated && User32.GetCapture() == hWnd;
            set
            {
                if (value)
                    User32.SetCapture(hWnd);
                else
                    User32.ReleaseCapture();
            }
        }

        //You don't get WM_MOUSELEAVE unless you're hooking mouse events

        private unsafe void HookMouseEvent()
        {
            if (!HasState(WindowState.TrackMouseEvent))
            {
                SetState(WindowState.TrackMouseEvent, true);

                var trackMouseEvent = new TRACKMOUSEEVENT
                {
                    cbSize = sizeof(TRACKMOUSEEVENT),
                    dwFlags = TRACKMOUSEEVENT_FLAGS.TME_LEAVE | TRACKMOUSEEVENT_FLAGS.TME_HOVER,
                    hwndTrack = hWnd,
                    dwHoverTime = 100
                };

                User32.TrackMouseEvent(ref trackMouseEvent);
            }
        }

        private void UnhookMouseEvent()
        {
            //The tracked event unregisters itself when the mouse leave event is sent, so we just need to update our internal bookkeeping
            SetState(WindowState.TrackMouseEvent, false);
        }

        protected virtual void WmMouseDown(ref Message m, int x, int y)
        {
            DefWndProc(ref m);

            //Our comboboxes won't open properly if we capture the mouse
            if (this is not SystemWindow)
                Capture = true;
        }

        protected virtual void WmMouseUp(ref Message m, int x, int y)
        {
            DefWndProc(ref m);

            if (this is not SystemWindow)
                Capture = false;
        }

        protected virtual void WmMouseMove(ref Message m, int x, int y) => DefWndProc(ref m);

        protected virtual void WmMouseLeave(ref Message m) => DefWndProc(ref m);

        protected virtual void WmMouseWheel(ref Message m, int delta) => DefWndProc(ref m);

        protected virtual void WmMouseHover(ref Message m) => DefWndProc(ref m);

        #endregion
        #region Keyboard

        protected virtual void WmKeyDown(ref Message m, VIRTUAL_KEY key) => DefWndProc(ref m);

        protected virtual void WmKeyUp(ref Message m, VIRTUAL_KEY key) => DefWndProc(ref m);

        #endregion
        #region Event

        protected virtual void WmCommand(ref Message m, int notificationCode, int controlId, HWND control) => DefWndProc(ref m);

        protected unsafe virtual void WmNotify(ref Message m, NMHDR* nmhdr)
        {
            //Parent controls receive WM_NOTIFY events for the children; since all of our children
            //are subclassed, we let the children handle their own events, rather than us

            if (!ReflectMessage(nmhdr->hwndFrom, ref m))
                DefWndProc(ref m);
        }

        protected unsafe virtual void WmReflectNotify(ref Message m, NMHDR* nmhdr) => DefWndProc(ref m);

        protected virtual void WmTimer(ref Message m) => DefWndProc(ref m);

        private bool ReflectMessage(HWND hWnd, ref Message m)
        {
            var children = _children;

            if (children != null)
            {
                for (var i = 0; i < children.Count; i++)
                {
                    var child = children[i];

                    if (child.IsHandleCreated && child.hWnd == hWnd)
                    {
                        User32.SendMessageW(hWnd, (int) (WM.WM_REFLECT | m.Msg), m.wParam, m.lParam);
                        return true;
                    }
                }
            }

            return false;
        }

        #endregion
    }
}
