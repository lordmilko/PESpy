#if DEBUG
using System;
using System.IO;
using System.Management.Automation;
using PESpy.ISO;
using PESpy.View;

namespace PESpy.PowerShell
{
    [Cmdlet(VerbsDiagnostic.Test, "PESpy")]
    public class TestPESpy : FileCmdlet
    {
        [Parameter(Mandatory = true, Position = 0)]
        public string Path { get; set; }

        protected override void ProcessRecord()
        {
            if (File.Exists(Path))
                ProcessFile();
            else if (Directory.Exists(Path))
                ProcessDirectory();
            else
                throw new InvalidOperationException($"Path '{Path}' does not exist");
        }

        private void ProcessFile()
        {
            using var file = Detector.TryOpenFile(Path);

            if (file == null)
                return;

            var progressRecord = new ProgressRecord(0, MyInvocation.MyCommand.Name, Path);
            WriteProgress(progressRecord);

            TestFile(file);

            progressRecord.RecordType = ProgressRecordType.Completed;
            WriteProgress(progressRecord);
        }

        private void ProcessDirectory()
        {
            var exts = new[]
            {
                "*.exe",
                "*.dll",
                "*.sys",
                "*.drv",
                "*.ocx",
                "*.pdb",
                "*.dbg",
                "*.sym"
            };

            var progressRecord = new ProgressRecord(0, MyInvocation.MyCommand.Name, "default");

            foreach (var fileName in Directory.EnumerateFiles(Path))
            {
                using var file = Detector.TryOpenFile(fileName);

                if (file == null)
                    continue;

                progressRecord.StatusDescription = fileName;
                WriteProgress(progressRecord);

                TestFile(file);

                progressRecord.RecordType = ProgressRecordType.Completed;
                WriteProgress(progressRecord);
            }

            foreach (var subDIr in Directory.EnumerateDirectories(Path))
            {
                progressRecord.StatusDescription = subDIr;
                WriteProgress(progressRecord);

                foreach (var ext in exts)
                {
                    foreach (var fileName in Directory.EnumerateFiles(subDIr, ext, SearchOption.AllDirectories))
                    {
                        using var file = Detector.TryOpenFile(fileName);

                        if (file == null)
                            continue;

                        progressRecord.CurrentOperation = "-> " + fileName;
                        WriteProgress(progressRecord);

                        TestFile(file);

                        progressRecord.CurrentOperation = default;
                        WriteProgress(progressRecord);
                    }
                }

                foreach (var fileName in Directory.EnumerateFiles(subDIr, "*.iso", SearchOption.AllDirectories))
                {
                    using var reader = new ISOReader(fileName);

                    foreach (var embeddedFile in reader.Root.EnumerateFiles(true))
                    {
                        if (Detector.TryOpenFile(embeddedFile, out var file))
                        {
                            progressRecord.CurrentOperation = "-> " + fileName + " -> " + embeddedFile.FullPath;
                            WriteProgress(progressRecord);

                            try
                            {
                                TestFile(file);
                            }
                            finally
                            {
                                file.Dispose();
                            }

                            progressRecord.CurrentOperation = default;
                            WriteProgress(progressRecord);
                        }
                    }
                }
            }

            progressRecord.RecordType = ProgressRecordType.Completed;
            WriteProgress(progressRecord);
        }

        private void TestFile(IFile file)
        {
            //Haven't implemented support for processing SYM files yet
            if (file.Kind == FileKind.SYM)
                return;

            var view = file.GetView();
            view.Accept(NullViewWalker.Instance);
        }
    }
}
#endif
