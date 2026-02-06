using System;
using System.Collections;
using System.Diagnostics;
using PInvoke;

namespace PESpy.UI
{
    internal class MainForm : NativeForm
    {
        private string _baseTitle;

        private ViewMap _viewMap;
        private LoadingPanel _loadingPanel;
        private TextView _textView;
        private NativePanel _textAndNavPanel;

        private OverviewPanel _overviewPanel;
        private DetailsView _detailsView;

        private LogicalView _logicalView;

        private NativeImageList _imageList;

        public unsafe MainForm()
        {
            //This all inlines into the raw commands in NativeAOT
            Menu = new NativeMenu(User32.CreateMenu())
            {
                {
                    "&File", new NativeMenu
                    {
                        { "&Open File...\tCtrl+O", WellKnownCommand.OpenFile },
                        { "C&lose File\tCtrl+W", WellKnownCommand.CloseFile },
                        { "E&xit", WellKnownCommand.Exit },
                    }
                },
                {
                    "&Search", new NativeMenu
                    {
                        { "Go to...\tCtrl+G", WellKnownCommand.Goto }
                    }
                }
            };

            NativeWindow.SuspendGlobalLayout();

            var imageList = CreateImageList();
            _imageList = imageList;

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
                        new ViewMap(out _viewMap)
                        {
                            Dock = DockStyle.Top,
                            Visible = false,
                            Size = new SIZE(0, 48)
                        },
                        new LoadingPanel(out _loadingPanel)
                        {
                            Dock = DockStyle.Fill,
                            Visible = false
                        },
                        new NativePanel(out _textAndNavPanel)
                        {
                            Dock = DockStyle.Fill,
                            Children =
                            {
                                new NavBar
                                {
                                    Dock = DockStyle.Top,
                                },
                                new TextView(out _textView)
                                {
                                    Dock = DockStyle.Fill,
                                }
                            },
                            Visible = false
                        }
                    }
                },
                Panel2 =
                {
                    Children =
                    {
                        new OverviewPanel(out _overviewPanel)
                        {
                            Dock = DockStyle.Fill,
                            Visible = false
                        },
                        new DetailsView(out _detailsView)
                        {
                            Dock = DockStyle.Fill,
                            Visible = false
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
                        new LogicalView(out _logicalView)
                        {
                            Dock = DockStyle.Fill,
                            ImageList = imageList,
                            Size = new SIZE(301, 778),
                            Visible = false
                        }
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

            _logicalView.AfterSelect += logicalView_AfterSelect;

            Size = new SIZE(1333, 856);
            Children.Add(splitLeftAndRight);

            NativeWindow.ResumeGlobalLayout();
            var isElevated = true; //temp

            _baseTitle = isElevated ? "PESpy (Administrator)" : "PESpy";

            Text = _baseTitle;

            App.FileOpened += App_FileOpened;
            App.FileClosed += App_FileClosed;
        }
        private void App_FileClosed(object? sender, EventArgs e)
        {
            App.BeginInvokeUI(() =>
            {
                SuspendLayout();

                User32.SendMessageW(hWnd, (int) WM.WM_SETREDRAW, 0, default);

                //Restore the original title
                Text = _baseTitle;

                //Clear all panes
                _logicalView.Visible = false;
                _textAndNavPanel.Visible = false;
                _detailsView.Visible = false;
                _overviewPanel.Visible = false;
                _viewMap.Visible = false;
                _loadingPanel.Visible = false;
                User32.SendMessageW(hWnd, (int) WM.WM_SETREDRAW, 1, default);
                Invalidate(true);

                ResumeLayout();
            });
        }

        private void UpdateBottomPanel(LogicalTreeNode node, bool refresh)
        {
            while (!node.HasContent)
            {
                if (node.Nodes.Count == 0)
            if (node is SpecialPaneNode s)
            {
                SuspendLayout();

                Debug.Assert(s.Kind == LogicalTreeNodeKind.Overview);

                _overviewPanel.Visible = true;
                _detailsView.Visible = false;
                ResumeLayout();
            }
            else
            {
                SuspendLayout();
                _overviewPanel.Visible = false;
                _detailsView.Visible = true;
                ResumeLayout();
            }
        }

        #endregion
        #region Invoke

        /* Invoke in WinForms works as follows
         * - Invoke/BeginInvoke calls FindMarshalingControl and then calls MarshaledInvoke, specifying whether the invoke is
         *   synchronous or not
         * - If GetWindowThreadProcessId == GetCurrentThreadId, and we're synchronous, we invoke immediately. Apparently even if
         *   we're on the UI thread, if it's an async request, we'll deadlockattempting to wait
         * - The action to execute is stored in a callback list
         * - RegisterWindowMessage is called if it hasn't been called previously to generate the unique message that is used
         *   to receive callbacks
         * - If we're the same thread, call InvokeMarshaledCallbacks immediately
         * - Else, post the message returned fromRegisterWindowMessage
         * - If we're synchronous, and the action did not complete instantly, wait for the wait handle on the payload
         *   and rethrow its exception if applicable
         * - In the default case of the WndProc there's a special casing to check whether the message is the message
         *   that we registered. If so we call InvokeMarshaledCallbacks
         */

        private static int s_invokeCallbackMessage;
        private static Queue s_invokeCallbackList; //I'm curious if this might save space in NativeAOT

        public unsafe void BeginInvoke(Action action)
        {
            Debug.Assert(IsHandleCreated);

            //We don't support synchronous invokes

            lock (this)
            {
                if (s_invokeCallbackList == default)
                    s_invokeCallbackList = new Queue();

                if (s_invokeCallbackMessage == default)
                    s_invokeCallbackMessage = User32.RegisterWindowMessageW("PESpyInvokeCallbackMessage");

                s_invokeCallbackList.Enqueue(action);
            }

            User32.PostMessageW(hWnd, s_invokeCallbackMessage, default, default);
        }

        private void InvokeCallbacks()
        {
            var list = s_invokeCallbackList;

            while (true)
            {
                Action current;

                lock (this)
                {
                    if (list.Count == 0)
                        return;

                    current = (Action) list.Dequeue()!;
                }

                current();
            }
        }

        #endregion

        protected unsafe override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WM.WM_UAHDRAWMENU:
                    WmUahDrawMenu(ref m, (UAHMENU*) (nint) m.lParam);
                    break;

                case WM.WM_UAHDRAWMENUITEM:
                    WmUahDrawMenuItem(ref m, (UAHDRAWMENUITEM*) (nint) m.lParam);
                    break;

                case WM.WM_UAHMEASUREMENUITEM:
                    WmUahMeasureMenuItem(ref m, (UAHMEASUREMENUITEM*) (nint) m.lParam);
                    break;

                default:
                    if ((int) m.Msg == s_invokeCallbackMessage && s_invokeCallbackMessage != 0)
                        InvokeCallbacks();
                    else
                        base.WndProc(ref m);
                    break;
            }
        }
}
