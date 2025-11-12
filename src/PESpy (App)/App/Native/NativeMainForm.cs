using System;
using PESpy.View;
using PInvoke;

namespace PESpy.Controls
{
    public class NativeMainForm : NativeForm, IFileAnalyzerProgress
    {
        public NativeMainForm()
        {
            ClientSize = new SIZE(800, 600);
            Text = "PESpy";

            App.FileOpened += (s, e) =>
            {
                Text = $"PESpy - {e.FileName}";
            };

            var xPad = 100;
            var yPad = 100;

            var dpi = User32.GetDpiForWindow(hWnd);

            var viewMapY = 30;
            var viewMapHeight = 45;

            Children.AddRange(
                new ViewMap
                {
                    Location = new POINT(0, viewMapY),
                    Size = new SIZE(ClientSize.Width, viewMapHeight)
                },
                new TextView
                {
                    Location = new POINT(15, viewMapY + viewMapHeight + 5),
                    Size = new SIZE(ClientSize.Width - 30, 505),

                    Children =
                    {
                        new NavBar
                        {
                            Location = new POINT(0, 0),
                            Size = new SIZE(ClientSize.Width, 30)
                        },
                    }
                }
            );
        }

        protected override unsafe void WmCreate(ref Message m, CREATESTRUCTW* pCreateStruct)
        {
            base.WmCreate(ref m, pCreateStruct);

            User32.GetClientRect(hWnd, out var rect);
        }

        protected override void WmShowWindow(ref Message m, bool show, int status)
        {
            base.WmShowWindow(ref m, show, status);
        #region IFileAnalyzerProgress

        void IFileAnalyzerProgress.NotifyPhase(FileAnalyzerProgressPhase phase)
        {
        }

        void ILocatorProgress.NotifyRequest(Uri uri)
        {
        }

        void ILocatorProgress.NotifyResponse(int httpResponseCode)
        {
        }

        void ILocatorProgress.NotifyProgress(double percent, int totalRead, int length)
        {
        }

        #endregion
    }
}
