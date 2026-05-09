using System;
using System.Diagnostics;
using System.Management.Automation;
using PESpy.View;

namespace PESpy.PowerShell
{
    class PowerShellLocatorProgress : IFileAnalyzerProgress
    {
        private ProgressRecord _progressRecord;
        private PSCmdlet _cmdlet;
        private Stopwatch _startTime;
        private FileAnalyzerProgressPhase _lastProgressPhase;

        private int _notificationIndex = 0;

        internal PowerShellLocatorProgress(PSCmdlet cmdlet)
        {
            _cmdlet = cmdlet;
        }

        public void Notify(LocatorProgressEventArgs eventArgs)
        {
            switch (eventArgs.Kind)
            {
                case LocatorProgressEventKind.BeginHttpRequest:
                    var beginRequestEventArgs = eventArgs.BeginHttpRequest;

                    _progressRecord = new ProgressRecord(0, _cmdlet.MyInvocation.MyCommand.Name, $"Requesting file '{beginRequestEventArgs.Uri}'");

                    _cmdlet.WriteProgress(_progressRecord);
                    break;

                case LocatorProgressEventKind.GotHttpResponse:
                    _startTime = Stopwatch.StartNew();
                    break;

                case LocatorProgressEventKind.CopyCascadeProgress:
                    var progressEventArgs = eventArgs.CopyCascadeProgress;

                    //Displaying progress on every single read massively slows reading down
                    if (_notificationIndex++ % 50 != 0)
                        return;

                    _progressRecord.PercentComplete = (int) progressEventArgs.Percent;
                    _progressRecord.CurrentOperation = $"Downloading file ({Math.Round((double) progressEventArgs.TotalRead / 1_000_000, 2)} MB/{Math.Round((double) progressEventArgs.Length / 1_000_000, 2)} MB)";

                    var bytesPerSecond = progressEventArgs.TotalRead / _startTime.Elapsed.TotalSeconds;
                    var remainingBytes = progressEventArgs.Length - progressEventArgs.TotalRead;
                    _progressRecord.SecondsRemaining = (int) (remainingBytes / bytesPerSecond);

                    _cmdlet.WriteProgress(_progressRecord);
                    break;
            }
        }

        public void NotifyPhase(FileAnalyzerProgressPhase phase)
        {
            //If this assert fails, we've messed up our ordering in the enum, perhaps as a result of having
            //shuffled various steps around
            Debug.Assert(phase >= _lastProgressPhase);
            _lastProgressPhase = phase;
        }

        public void PhaseComplete(FileAnalyzerProgressPhase phase, long elapsed)
        {
        }
    }
}
