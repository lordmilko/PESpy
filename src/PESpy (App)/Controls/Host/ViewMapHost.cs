#if WINFORMS
namespace PESpy.Controls
{
    /// <summary>
    /// Allows hosting a <see cref="ViewMap"/> inside a WinForms <see cref="Control"/>
    /// </summary>
    public partial class ViewMapHost : NativeToWinFormsHostControl
    {
        public ViewMapHost()
        {
            InitializeComponent();
        }

        protected override NativeWindow CreateWindow() => new ViewMap();
    }
}
#endif
