using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management.Automation;
using System.Threading;

namespace PESpy.PowerShell
{
    class PowerShellLocatorProgress : ILocatorProgress, IDisposable
    {
        private ProgressRecord progressRecord;
        private PSCmdlet cmdlet;
        private object objLock = new();
        private Queue<Action> queue = new();
        private Stopwatch startTime;

        private int notificationIndex = 0;

        internal AutoResetEvent WaitHandle = new AutoResetEvent(false);

        internal PowerShellLocatorProgress(PSCmdlet cmdlet)
        {
            this.cmdlet = cmdlet;
        }

        public void Notify(LocatorProgressEventArgs eventArgs)
        {
            switch (eventArgs.Kind)
            {
                case LocatorProgressEventKind.BeginHttpRequest:
                    var beginRequestEventArgs = eventArgs.BeginHttpRequest;

                    lock (objLock)
                    {
                        queue.Enqueue(() =>
                        {
                            progressRecord = new ProgressRecord(0, "Get-PEFile", $"Requesting file '{beginRequestEventArgs.Uri}'");

                            cmdlet.WriteProgress(progressRecord);
                        });

                        WaitHandle.Set();
                    }
                    break;

                case LocatorProgressEventKind.GotHttpResponse:
                    startTime = Stopwatch.StartNew();
                    break;

                case LocatorProgressEventKind.CopyCascadeProgress:
                    var progressEventArgs = eventArgs.CopyCascadeProgress;

                    //Displaying progress on every single read massively slows reading down
                    if (notificationIndex++ % 50 != 0)
                        return;

                    lock (objLock)
                    {
                        queue.Enqueue(() =>
                        {
                            progressRecord.PercentComplete = (int) progressEventArgs.Percent;
                            progressRecord.CurrentOperation = $"Downloading file ({Math.Round((double) progressEventArgs.TotalRead / 1_000_000, 2)} MB/{Math.Round((double) progressEventArgs.Length / 1_000_000, 2)} MB)";

                            var bytesPerSecond = progressEventArgs.TotalRead / startTime.Elapsed.TotalSeconds;
                            var remainingBytes = progressEventArgs.Length - progressEventArgs.TotalRead;
                            progressRecord.SecondsRemaining = (int) (remainingBytes / bytesPerSecond);

                            cmdlet.WriteProgress(progressRecord);
                        });

                        WaitHandle.Set();
                    }
                    break;
            }
        }

        public void DrainQueue()
        {
            lock (objLock)
            {
                while (queue.Count > 0)
                {
                    var action = queue.Dequeue();

                    action();
                }
            }
        }

        public void Dispose()
        {
            WaitHandle.Dispose();
        }
    }
}
