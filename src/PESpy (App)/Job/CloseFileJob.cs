using PESpy.View;

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
            if (App.TryCloseFileAccessor())
                FileAnalyzer.GCLargeObjectHeap();

            App.RaiseFileClosed();
        }
    }
}
