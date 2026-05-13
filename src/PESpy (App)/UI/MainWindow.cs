using System;
using PInvoke;
using ReView;

namespace PESpy
{
    internal class MainWindow : Window
    {
        private string _baseTitle;

        public MainWindow()
        {
            var isElevated = GetIsElevated();

            _baseTitle = isElevated ? "PESpy (Administrator)" : "PESpy";

            AllowDragDrop = true;

            Text = _baseTitle;
            Size = new SIZE(1133, 756);
        }

        protected override unsafe void WmDropFiles(HDROP hDrop)
        {
            ExecuteAction(() =>
            {
                try
                {
                    //We only support dropping a single file, so we don't need to query the count
                    var numChars = Shell32.DragQueryFileW(hDrop, 0, default, 0);

                    if (numChars != 0)
                    {
                        PWSTR buffer = stackalloc char[numChars + 1];

                        numChars = Shell32.DragQueryFileW(hDrop, 0, buffer, numChars + 1); //Yes this should include +1

                        if (numChars != 0)
                        {
                            App.OpenFile(buffer.ToString());
                        }
                    }
                }
                finally
                {
                    Shell32.DragFinish(hDrop);
                }
            });
        }

        //Protects against unhandled exceptions crashing the UI thread
        private static void ExecuteAction(Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                App.RaiseError(ex);
            }
        }

        private static unsafe bool GetIsElevated()
        {
            HANDLE tokenHandle;

            if (!Advapi32.OpenProcessToken(Kernel32.GetCurrentProcess(), TOKEN_ACCESS_MASK.TOKEN_QUERY, &tokenHandle))
                return false;

            try
            {
                TOKEN_ELEVATION tokenElevation;

                if (!Advapi32.GetTokenInformation(tokenHandle, TOKEN_INFORMATION_CLASS.TokenElevation, &tokenElevation, sizeof(TOKEN_ELEVATION), out _))
                    return false;

                return tokenElevation.TokenIsElevated != 0;
            }
            finally
            {
                Kernel32.CloseHandle(tokenHandle);
            }
            
        }
    }
}
