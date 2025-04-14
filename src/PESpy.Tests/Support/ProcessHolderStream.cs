using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using ChaosLib.Memory;

namespace PESpy.Tests
{
    class ProcessHolderStream : RelayStream
    {
        public static unsafe ProcessHolderStream New(string path)
        {
            if (IntPtr.Size != 8)
                throw new NotImplementedException();

            Environment.SetEnvironmentVariable("PESPY_TEST_PARENT_PID", Process.GetCurrentProcess().Id.ToString());

            var process = Process.Start(path);

            //Wait for MainModule to be loaded
            Thread.Sleep(100);

            var stream = new ProcessMemoryStream(process.Handle);
            stream.Position = (long) (void*) process.MainModule.BaseAddress;

            return new ProcessHolderStream(process, new RelativeToAbsoluteStream(stream, stream.Position));
        }

        private Process process;

        private ProcessHolderStream(Process process, Stream stream) : base(stream)
        {
            this.process = process;
        }

        protected override void Dispose(bool disposing)
        {
            process.Dispose();
        }
    }
}
