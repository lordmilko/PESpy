using System;
using System.Runtime.CompilerServices;
using System.Threading;
using PESpy.View;
#if !DISABLE_REFLOW
using ReFlow.FileAnalysis;
#endif

namespace PESpy
{
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
                var fileAccessor = FileAccessor.Create(_file);
                App.SetFileAccessor(fileAccessor);

                App.RaiseFileOpened(new FileOpenedEventArgs(FileOpenedEventKind.OpenAccessor, fileAccessor.File));

                FileAnalyzer.Analyze(fileAccessor, new FileAnalyzerOptions
                {
#if !DISABLE_REFLOW
                    Disassembler = IntelFileDisassembler.Instance,
#endif
                    HttpPolicy = LocatorHttpPolicy.All,
                    Progress = App.Progress,
                    TrackXRefs = true,
                    CancellationToken = _cancellationToken
                });

                App.RaiseFileOpened(new FileOpenedEventArgs(FileOpenedEventKind.AnalysisComplete, null));

                return true;
            }
            catch (Exception ex)
            {
                App.RaiseError(ex);
                return false;
            }
        }
    }
}
