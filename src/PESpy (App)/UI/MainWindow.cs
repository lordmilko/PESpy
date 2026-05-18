using System;
using PESpy.ListView;
using PESpy.NavBar;
using PESpy.Overview;
using PESpy.TextView;
using PESpy.TreeView;
using PESpy.ViewMap;
using PInvoke;
using ReView;

namespace PESpy
{
    internal partial class MainWindow : Window
    {
        private string _baseTitle;

        private TreeViewPanel _treeView;

        private ViewMapPanel _viewMap;

        private NavBarPanel _navBar;
        private TextViewPanel _textView;
        private NativePanel _textAndNavPanel;

        private OverviewPanel _overviewPanel;
        private ListViewPanel _listView;

        public MainWindow()
        {
            var isElevated = GetIsElevated();

            _baseTitle = isElevated ? "PESpy (Administrator)" : "PESpy";

            AllowDragDrop = true;

            Text = _baseTitle;
            Size = new SIZE(1133, 756);

            App.FileOpened += App_FileOpened;
            App.FileClosed += App_FileClosed;

            //This won't render if it's added during suspend global layout
            //for some reason
            Children.Add(new CustomToolStrip
            {
                Dock = DockStyle.Top,
                Height = 29 + CustomToolStrip.Padding,
                Children =
                {
                    new ToolStripDropDownButton
                    {
                        Text = "File",
                        Size = new SIZE(36, 26),
                    }
                }
            });

            var splitTopAndBottom = new NativeSplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 440,
                Panel1 =
                {
                    Border = true,
                    Children =
                    {
                        new ViewMapPanel(out _viewMap),
                        new NativePanel(out _textAndNavPanel)
                        {
                            Dock = DockStyle.Fill,
                            Children =
                            {
                                new NavBarPanel(out _navBar),
                                new TextViewPanel(out _textView)
                            },
                        }
                    }
                },
                Panel2 =
                {
                    Children =
                    {
                        new OverviewPanel(out _overviewPanel)
                        {
                            Visible = true
                        }
                    }
                }
            };

            var splitLeftAndRight = new NativeSplitContainer
            {
                Dock = DockStyle.Fill,
                SplitterDistance = 301,
                FixedPanel = FixedPanel.Panel1,
                Panel1 =
                {
                    Children =
                    {
                        new TreeViewPanel(out _treeView)
                    }
                },
                Panel2 =
                {
                    Children =
                    {
                        splitTopAndBottom
                    }
                }
            };

            Children.Add(splitLeftAndRight);
        }

        private void App_FileOpened(object? sender, FileOpenedEventArgs e)
        {
            switch (e.EventKind)
            {
                case FileOpenedEventKind.OpenFile:
                    BeginInvoke(() =>
                    {
                        var file = e.File;

                        /* When you hover over the app icon in the taskbar, you're often only
                         * going to get a few characters; as such, displaying just the name of the file
                         * is important. But once the window is actually opened, it's more important to be
                         * able to see the actual path to the file that we've got open; hence, we show both */
                        Text = $"{file!.Name} {file.FileName} - {_baseTitle}";
                    });
                    break;

                case FileOpenedEventKind.OpenAccessor:
                    //With our accessor in hand, we've now got all we need to display most of the UI.
                    //Several different components listen for this message and will do their own setup
                    //in response to this
                    BeginInvoke(() =>
                    {
                        SuspendLayout();

                        ResumeLayout();
                    });
                    break;

                case FileOpenedEventKind.AnalysisComplete:
                    BeginInvoke(() =>
                    {
                        SuspendLayout();

                        ResumeLayout();
                    });
                    break;
            }
        }

        private void App_FileClosed(object? sender, EventArgs e)
        {
            BeginInvoke(() =>
            {
                SuspendLayout();

                //Restore the original title
                Text = _baseTitle;

                //Clear all panes

                ResumeLayout();
            });
        }

        protected override void OnHandleCreated()
        {
            base.OnHandleCreated();

            OpenTestFile();
        }

        partial void OpenTestFile();

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
