#if WINFORMS
using PInvoke;

namespace PESpy.Controls
{
    /// <summary>
    /// Allows hosting a <see cref="TextView"/> inside a WinForms <see cref="Control"/>
    /// </summary>
    public partial class TextViewHost : NativeToWinFormsHostControl
    {
        public TextViewHost()
        {
            InitializeComponent();
        }

        protected override NativeWindow CreateWindow()
        {
            return new TextView
            {
                Children =
                {
                    new NavBar
                    {
                        Location = new POINT(Bounds.X, Bounds.Y),
                        Size = new SIZE(Width, 30)
                    },
                }
            };
        }

        protected override void WndProc(ref System.Windows.Forms.Message m)
        {
            //WinForms is doing something to mess up my UISF_HIDEFOCUS flag
            //I set on my ComboBox, so we'll only pass certain allowed messages
            //to the WinForms WndProc
            if (_window != null && _window.IsHookInstalled)
                _window.WndProc(m.HWnd, m.Msg, m.WParam, m.LParam);
            else
                base.WndProc(ref m);
        }
    }
}
#endif
