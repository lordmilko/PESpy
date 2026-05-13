using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using PESpy.View;
using PInvoke;
using ReView;

namespace PESpy
{
    internal class App
    {
        [AllowNull]
        internal static MainWindow MainWindow;

        private static object _fatalErrorLock = new object();

        public static event EventHandler<FileOpenedEventArgs>? FileOpened;
        public static event EventHandler? FileClosed;

        public static event EventHandler<int>? PositionChanged;

        private static RefCounted<FileAccessor> FileAccessor = new();

        internal static FileAccessorHolder AcquireFileAccessor() => new FileAccessorHolder(FileAccessor.Acquire());

        internal static void ReleaseFileAccessor() => FileAccessor.Release();

        internal static void SetFileAccessor(FileAccessor fileAccessor)
        {
            Debug.Assert(FileAccessor.IsEmpty, "A CloseFileJob should have been dispatched prior to dispatching this OpenFileJob");
            FileAccessor.Set(fileAccessor);
        }

        internal static bool TryCloseFileAccessor() => FileAccessor.TryDispose();

        internal static void RaiseFileOpened(FileOpenedEventArgs eventArgs) => FileOpened?.Invoke(null, eventArgs);

        internal static void RaiseFileClosed() => FileClosed?.Invoke(null, EventArgs.Empty);

        public static IFileAnalyzerProgress? Progress { get; set; }

        private static Thread? _engineThread;

        private static Queue<IJob> _workQueue = new Queue<IJob>();
        private static object _workQueueLock = new object();
        private static ManualResetEventSlim _hasWork = new ManualResetEventSlim(false);
        private static CancellationTokenSource _threadCTS = new CancellationTokenSource();
        private static CancellationTokenSource _fileCTS = new CancellationTokenSource();

        static App()
        {
            NativeWindow.OnFatalError += (s, e) => FatalError(e);
        }

        public static void OpenFile(string fileName)
        {
            //Check whether the file exists on the UI thread

            if (!File.Exists(fileName))
                throw new FileNotFoundException($"Could not find file '{fileName}'");

            //Strictly speaking you could have slow storage, and so could argue we should do this on the background thread
            //as well, but we'll do this on the UI thread for now

            if (Detector.TryOpenFile(fileName, out var file))
            {
                EnsureThread();

                //Close the previous file (if one is open)
                _fileCTS.Cancel();
                _fileCTS = new CancellationTokenSource();

                lock (_workQueueLock)
                {
                    _workQueue.Enqueue(CloseFileJob.Instance);
                    _workQueue.Enqueue(new OpenFileJob(file, _fileCTS.Token));
                }

                FileOpened?.Invoke(null, new FileOpenedEventArgs(FileOpenedEventKind.OpenFile, file));

                _hasWork.Set();
            }
            else
            {
                RaiseError($"File '{fileName}' is not in a format understood by PESpy");
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
            var waitHandles = new WaitHandle[] { _hasWork.WaitHandle, _threadCTS.Token.WaitHandle };

            while (!_threadCTS.IsCancellationRequested)
            {
                WaitHandle.WaitAny(waitHandles);

                if (_threadCTS.IsCancellationRequested)
                    return;

                _hasWork.Reset();

                IJob? job;

                while (true)
                {
                    lock (_workQueueLock)
                    {
                        if (!_workQueue.TryDequeue(out job))
                            break;
                    }

                    job.Execute();
                }
            }
        }

        public static void RaiseError(Exception ex) => RaiseError(ex.Message);

        public static void RaiseError(string message)
        {
            User32.MessageBoxW(MainWindow?.NativeHandle ?? default, message, "PESpy", MESSAGEBOX_STYLE.MB_OK | MESSAGEBOX_STYLE.MB_ICONWARNING);
        }

        internal static void FatalError(Exception ex)
        {
            //If multiple background threads have fatal errors simultaneously, we don't want to spam the user with popups
            lock (_fatalErrorLock)
            {
#if DEBUG
                Debug.Assert(false, ex.ToString());
#else
                User32.MessageBoxW(MainWindow?.NativeHandle ?? default, ex.ToString(), "ReDbg Fatal Error", MESSAGEBOX_STYLE.MB_OK | MESSAGEBOX_STYLE.MB_ICONERROR);
#endif

                Environment.Exit(ex.HResult);
            }
        }
    }
}
