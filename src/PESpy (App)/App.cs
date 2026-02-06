using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using PESpy.UI;
using PESpy.View;
using PInvoke;
using ReFlow.FileAnalysis;

namespace PESpy
{
    public enum FileOpenedEventKind
    {
        //Context is an IFile
        OpenFile = 1,

        //Context is a FileAccessor
        OpenAccessor,

        AnalysisComplete
    }

    public class FileOpenedEventArgs
    {
        public FileOpenedEventKind EventKind { get; }

        public IFile File { get; }

        public FileOpenedEventArgs(FileOpenedEventKind eventKind, IFile file)
        {
            EventKind = eventKind;
            File = file;
        }
    }

    internal class App
    {
        public static IFileAnalyzerProgress Progress { get; set; }

        private static MainForm _mainForm;
        private static HWND _hWnd;

        public static MainForm MainForm
        {
            get => _mainForm;
            set
            {
                _mainForm = value;

                //Need to stash the handle so we can access it on a non-UI thread later
                //todo: this is winforms specific
                _mainForm.HandleCreated = () => _hWnd = _mainForm.hWnd; //Don't force create the handle now
            }
        }

        public static event EventHandler<FileOpenedEventArgs> FileOpened;
        public static event EventHandler FileClosed;

        public static event EventHandler<int> PositionChanged;

        //Gets the file accessor of the currently active file
        public static FileAccessor? FileAccessor { get; private set; }

        private static Thread _engineThread;

        private static Queue<IFile> _workQueue = new Queue<IFile>();
        private static object _workQueueLock = new object();
        private static ManualResetEventSlim _hasWork = new ManualResetEventSlim(false);
        private static CancellationTokenSource _cts = new CancellationTokenSource();

        public static void OpenFile(string fileName)
        {
            //Check whether the file exists on the UI thread

            if (!File.Exists(fileName))
                throw new FileNotFoundException($"Could not find file '{fileName}'");

            //Strictly speaking you could have slow storage, and so could argue we should do this on the background thread
            //as well, but we'll do this on the UI thread for now

            if (Detector.TryOpenFile(fileName, out var file))
            {
                /* It's something we can handle. We now need to perform our two phase startup
                 * 1. Display something to the user as fast as possible (sidebar + overview)
                 * 2. Kickoff analysis which should run independently of everything else
                 * 
                 * During startup we can have a timer running on the ViewMap that periodically refreshes
                 * itself to show the progress of the analysis
                 */

                EnsureThread();

                //Close the previous file (if one is open)
                CloseFile();

                lock (_workQueueLock)
                    _workQueue.Enqueue(file);

                FileOpened?.Invoke(null, new FileOpenedEventArgs(FileOpenedEventKind.OpenFile, file));

                _hasWork.Set();
            }
            else
                RaiseError($"Failed to detect the file type of file '{fileName}'");
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

                //Ensure that the FileAccessor doesn't remain rooted in this frame so we can GC it
                //when the file is closed
                ProcessFile(file);
            }
        }

        private static void ProcessFile(IFile file)
        {
            Debug.Assert(file != null);

            FileAccessor fileAccessor = null;

            try
            {
                //To get things going for the user quickly, just begin by creating the FileAccessor (which will analyze
                //all of the various sections contained in the file) and then create a basic overview for the user. If
                //we need to lookup external symbols, defer this until after the analyzer has loaded them
                fileAccessor = FileAccessor.Create(file);
                _ = fileAccessor.Overview;

                FileAccessor = fileAccessor;
                FileOpened?.Invoke(null, new FileOpenedEventArgs(FileOpenedEventKind.OpenAccessor, file));

                FileAnalyzer.Analyze(fileAccessor, IntelFileDisassembler.Instance, Progress);

                FileOpened?.Invoke(null, new FileOpenedEventArgs(FileOpenedEventKind.AnalysisComplete, null));
            }
            catch (Exception ex)
            {
                CloseFile();

                RaiseError("Failed to open file: " + ex.ToString());
            }
        }

        public static void CloseFile()
        {
            //I think you'll have a ldloc to access the file accessor in order to dispose it
            //or something, which prevents us from then GC'ing it
            [MethodImpl(MethodImplOptions.NoInlining)]
            bool CloseFileInternal()
            {
                if (FileAccessor != null)
                {
                    FileAccessor.Dispose();
                    FileAccessor = default;
                    FileClosed.Invoke(null, EventArgs.Empty);

                    return true;
                }

                return false;
            }

            if (CloseFileInternal())
                FileAnalyzer.GCLargeObjectHeap();
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

        //Asynchronously invokes an action on the UI thread
        public static void BeginInvokeUI(Action action)
        {
            MainForm.BeginInvoke(action);
        }
        }

        public static void RaiseFatalError(Exception ex)
        {
            User32.MessageBoxW(_hWnd, $"A fatal error has occurred and PESpy must shutdown:{Environment.NewLine}{Environment.NewLine}{ex}", "PESpy: FATAL ERROR", MESSAGEBOX_STYLE.MB_OK | MESSAGEBOX_STYLE.MB_ICONERROR);
            Environment.Exit(ex.HResult);
        }
    }
}
