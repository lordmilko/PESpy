using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using PESpy.View;
using ReFlow.FileAnalysis;

namespace PESpy
{
    /* When we cancel the current opening job, we can't close the file on the UI thread, as
     * we don't want to rugpull the analysis if it hasn't completed right away. As such, we
     * post a CloseFileJob immediately after cancelling to facilitate doing the actual work
     * of closing the current file once the worker thread is free (implying that there is no more
     * ongoing analysis) */
    class CloseFileJob : IJob
    {
        public static readonly CloseFileJob Instance = new();

        public void Execute()
        {
            //I think when you try and access the FileAccessor in order to dispose it this will
            //result in a ldloc, which will then prevent us from being able to dispose it. As such
            //we make sure we never touch it in the outer frame
            if (CloseFile())
                FileAnalyzer.GCLargeObjectHeap();
        }

        //We need to make sure the parent frame doesn't touch the FileAccessor so that we
        //can GC it properly
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool CloseFile()
        {
            var fileAccessor = App.FileAccessor;

            if (fileAccessor == null)
                return false;

            fileAccessor.Dispose();
            App.FileAccessor = null;
            return true;
        }
    }

    class OpenFileJob : IJob
    {
        private IFile _file;
        private CancellationToken _cancellationToken;

        internal OpenFileJob(IFile file, CancellationToken cancellationToken)
        {
            _file = file;
            _cancellationToken = cancellationToken;
        }

        public void Execute()
        {
            //Make sure we don't reference the FileAccessor in the current frame
            //so we can GC it in the event of an error
            if (!OpenFile())
                FileAnalyzer.GCLargeObjectHeap();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private bool OpenFile()
        {
            try
            {
                Debug.Assert(App.FileAccessor == null, "A CloseFileJob should have been dispatched prior to dispatching this OpenFileJob");

                App.FileAccessor = FileAccessor.Create(_file);

                FileAnalyzer.Analyze(App.FileAccessor, new FileAnalyzerOptions
                {
                    Disassembler = IntelFileDisassembler.Instance,
                    HttpPolicy = LocatorHttpPolicy.All,
                    Progress = App.Progress,
                    TrackXRefs = true,
                    CancellationToken = _cancellationToken
                });

                return true;
            }
            catch (Exception ex)
            {
                App.RaiseError(ex);
                return false;
            }
        }
    }

    interface IJob
    {
        void Execute();
    }
}
