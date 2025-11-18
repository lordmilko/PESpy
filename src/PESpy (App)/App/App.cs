using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
#if WINFORMS
using System.Windows.Forms;
#endif
using PESpy.Controls;
using PESpy.View;
using PInvoke;
using ReFlow.FileAnalysis;

#nullable disable

namespace PESpy
{
    internal static class App
    {
#if WINFORMS
        //WinForms
        public static MainForm MainForm;
#endif

        //Native
        public static NativeMainForm NativeMainForm;

        public static FileAccessor FileAccessor;

        public static event EventHandler<IFile> FileOpened;
        public static event EventHandler FileClosed;

        public static event EventHandler AnalysisCompleted;
        public static event EventHandler AnalysisFailed;

        public static event EventHandler<int> PositionChanged;

        private static Thread _engineThread;

        private static Queue<IFile> _workQueue = new Queue<IFile>();
        private static object _workQueueLock = new object();
        private static ManualResetEventSlim _hasWork = new ManualResetEventSlim(false);
        private static CancellationTokenSource _cts = new CancellationTokenSource();

        private static bool? isInDesignMode;

#if WINFORMS
        public static bool IsInDesignMode(Control control)
        {
            /* LicenseUsageMode.Designtime is not very reliable. Sometimes the context that it relies upon is not present,
             * and then Visual Studio starts crashing because something inside LoadLibraryW gets corrupted (I tried to debug
             * it and couldn't figure out what was up. Path of the user search path it uses gets corrupt). Our workaround will
             * be to use a more sophisticated approach to detecting whether or not we're in design mode. Note that in .NET 6
             * there is a Control.IsAncestorSiteInDesignMode property you can use, which basically seems to do the same thing
             * that we do here */
            if (!isInDesignMode.HasValue)
            {
                isInDesignMode = LicenseManager.UsageMode == LicenseUsageMode.Designtime;

                while (!isInDesignMode.Value && control != null)
                {
                    isInDesignMode = control.Site?.DesignMode ?? false;
                    control = control.Parent;
                }
            }

            return isInDesignMode.Value;
        }
#endif

        public static void OpenFile(string fileName)
        {
            if (!File.Exists(fileName))
                throw new FileNotFoundException($"Could not find file '{fileName}'");

            if (Detector.TryOpenFile(fileName, out var file))
            {
                EnsureThread();

                //Close the previous file (if one is open)
                CloseFile();

                lock (_workQueueLock)
                    _workQueue.Enqueue(file);

                FileOpened?.Invoke(null, file);

                _hasWork.Set();
            }
            else
            {
                //We need to specify the owner to make it modal
                User32.MessageBoxW(default, $"Failed to detect the file type of file '{fileName}'", "PESpy", MESSAGEBOX_STYLE.MB_OK | MESSAGEBOX_STYLE.MB_ICONERROR);
            }
        }

        private static void EnsureThread()
        {
            if (_engineThread != null)
                return;

            _engineThread = new Thread(ThreadProc);
            _engineThread.Name = "Engine Thread";
            _engineThread.IsBackground = true;

            _engineThread.Start();
        }

        private static void ThreadProc()
        {
            var waitHandles = new WaitHandle[] { _hasWork.WaitHandle, _cts.Token.WaitHandle };

            while (!_cts.IsCancellationRequested)
            {
                WaitHandle.WaitAny(waitHandles);

                if (_cts.IsCancellationRequested)
                    return;

                _hasWork.Reset();

                IFile file;

                lock (_workQueueLock)
                    file = _workQueue.Dequeue();

                Debug.Assert(file != null);

#if WINFORMS
                IFileAnalyzerProgress progress = MainForm;
#else
                IFileAnalyzerProgress progress = NativeMainForm;
#endif

                try
                {
                    FileAccessor = FileAnalyzer.Analyze(file, IntelFileDisassembler.Instance, progress);

#if NET7_0_OR_GREATER
                    //See the comments in FileAnalyzer.cs as to why we do this here, and why we need two GC's
                    GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                    GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, blocking: true, compacting: true);
                    GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, blocking: true, compacting: true);
#endif

                    AnalysisCompleted?.Invoke(null, EventArgs.Empty);
                }
                catch (Exception ex)
                {
                    file.Dispose();

                    AnalysisFailed?.Invoke(null, EventArgs.Empty);

#if WINFORMS
                    MainForm.BeginInvoke((Action) (() =>
                    {
                        MessageBox.Show(MainForm, ex.ToString(), null, MessageBoxButtons.OK);
                    }));
#else
                    User32.MessageBoxW(NativeMainForm.hWnd, ex.ToString(), null, MESSAGEBOX_STYLE.MB_OK);
#endif
                }
            }
        }

        private static void CloseFile()
        {
            if (FileAccessor != null)
            {
                FileAccessor.Dispose();
                FileAccessor = default;
                FileClosed.Invoke(null, EventArgs.Empty);
            }
        }

        //When one component updates its position, it will broadcast this change to all other components that listen to PositionChanged
        //events. Those components may update their positions, which in turn may induce them into raising their own position changed events.
        //As such, we need to guard against multiple levels of position changed events. Whatever we say the address is is what it is,
        //so we wouldn't expect somebody to be trying to "correct" what the address we should jump to is
        private static int _positionChangedReentrancyCount;

        public static void RaisePositionChanged(object sender, int newPosition)
        {
            if (_positionChangedReentrancyCount > 0)
                return;

            _positionChangedReentrancyCount++;

            PositionChanged?.Invoke(sender, newPosition);

            _positionChangedReentrancyCount--;
        }
    }
}
