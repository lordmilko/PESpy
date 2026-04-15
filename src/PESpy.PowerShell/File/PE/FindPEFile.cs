using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using Microsoft.PowerShell.Commands;

namespace PESpy.PowerShell.PE
{
    [Cmdlet(VerbsCommon.Find, "PEFile")]
    public class FindPEFile : FileCmdlet
    {
        [Parameter(Mandatory = true, Position = 0)]
        public string Path { get; set; }

        [Parameter]
        public SwitchParameter Recurse { get; set; }

        [Parameter(Mandatory = true, Position = 1)]
        public ScriptBlock ScriptBlock { get; set; }

        [Parameter]
        public string[] Extensions { get; set; }

        protected override void ProcessRecord()
        {
            if (Extensions == null)
                Extensions = new[] { "*.dll", "*.exe", "*.sys" };
            else
                throw new System.NotImplementedException();

            var variablesToDefine = new List<PSVariable>();
            var dollarUnder = new PSVariable("_");

            //There's no way to tap into the FileSystemProvider and enumerate files, so we need to handle
            //enumerating files while avoiding unauthorized acces exceptions ourselves

            var all = SessionState.Provider.Get("FileSystem");

            foreach (var filePath in EnumerateFiles(Path, 0))
            {
                if (!Detector.TryOpenFile(filePath, out var file))
                    continue;

                var dispose = true;

                try
                {
                    if (file.Kind != FileKind.PE)
                        continue;

                    dollarUnder.Value = file;

                    //InvokeWithContext removes the special variables "this", "_", and "input", so we need to
                    //make sure we re-add $_ each time
                    variablesToDefine.Add(dollarUnder);
                    var result = ScriptBlock.InvokeWithContext(null, variablesToDefine);

                    if (result.Count == 0)
                        continue;
                    else if (result.Count > 1)
                    {
                        dispose = false;
                        WriteObject(file); //A bunch of stuff means they want it
                    }
                    else
                    {
                        if (result[0].BaseObject is bool b)
                        {
                            if (b)
                            {
                                dispose = false;
                                WriteObject(file);
                            }
                        }
                        else
                        {
                            //Some non-bool type; they want it
                            dispose = false;
                            WriteObject(file);
                        }
                    }
                }
                finally
                {
                    if (dispose)
                        file.Dispose();
                }
            }

            base.ProcessRecord();
        }

        private IEnumerable<string> EnumerateFiles(string path, int level)
        {
            if (Recurse)
            {
                ProgressRecord progressRecord = default;

                string[] dirs;

                try
                {
                    //Note that if we don't have access, an exception will be thrown even before
                    //we've started trying to enumerate
                    dirs = Directory.EnumerateDirectories(path).ToArray();
                }
                catch (UnauthorizedAccessException)
                {
                    yield break;
                }

                foreach (var ext in Extensions)
                {
                    var files = Directory.EnumerateFiles(path, ext);

                    foreach (var file in files)
                        yield return file;
                }

                for (var i = 0; i < dirs.Length; i++)
                {
                    var dir = dirs[i];

                    progressRecord = new ProgressRecord(level, MyInvocation.MyCommand.Name, path);

                    if (level > 0)
                        progressRecord.ParentActivityId = level - 1;

                    progressRecord.PercentComplete = (int) (((double) i / dirs.Length) * 100);
                    WriteProgress(progressRecord);

                    foreach (var child in EnumerateFiles(dir, level + 1))
                        yield return child;
                }

                if (progressRecord != null)
                {
                    progressRecord.RecordType = ProgressRecordType.Completed;
                    WriteProgress(progressRecord);
                }
            }
            else
            {
                foreach (var ext in Extensions)
                {
                    IEnumerable<string> files;

                    try
                    {
                        files = Directory.EnumerateFiles(path, ext);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        yield break;
                    }

                    foreach (var file in files)
                        yield return file;
                }
            }
        }
    }

    //public class GetPEFile_old : PECmdlet
    //{
    //    [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
    //    [Alias("PSPath")] //ValueFromPipelineByPropertyName applies to this, and FileInfo objects have a PSPath
    //    public string Path { get; set; }

    //    protected override void ProcessRecord()
    //    {
    //        //var l = LIBFile.FromFile("C:\\Program Files (x86)\\Microsoft Visual Studio\\2019\\Enterprise\\VC\\Tools\\MSVC\\14.29.30133\\lib\\onecore\\x64\\msobj140-msvcrt.lib");

    //        //todo: theres a bug with "C:\\Program Files (x86)\\Microsoft Visual Studio\\2019\\Enterprise\\VC\\Tools\\MSVC\\14.29.30133\\lib\\onecore\\x64\\msobj140-msvcrt.lib"
    //        //two values wrote to the same address

    //        //var v = l.GetView();

    //        //We need to write progress on the main thread, which may be blocked by waiting for the symbols to be downloaded.
    //        //So Plan B: do all this work on a background thread

    //        var path = SessionState.Path.GetResolvedProviderPathFromPSPath(Path, out var provider)?.FirstOrDefault();

    //        if (path == null)
    //            throw new NotImplementedException();

    //        using var progress = new PowerShellLocatorProgress(this);

    //        if (!Detector.TryOpenFile(path, out var file))
    //        {
    //            WriteWarning($"Cannot detect type of file '{path}'");
    //            return;
    //        }

    //        using (file)
    //        {
    //            var task = Task.Run<object>(() =>
    //            {
    //                using (var symbolAccessor = file.GetSymbolAccessor(progress: progress))
    //                {
    //                    return file.Kind switch
    //                    {
    //                        FileKind.PE          => new PEFileOverview((PEFile) file, symbolAccessor),
    //                        FileKind.NE          => new NEFileOverview((NEFile) file, symbolAccessor),
    //                        FileKind.LE          => new LEFileOverview((LEFile) file, symbolAccessor),
    //                        FileKind.DOS         => new DOSFileOverview((DOSFile) file, symbolAccessor),
    //                        FileKind.DBG         => new DBGFileOverview((DBGFile) file, symbolAccessor),
    //                        FileKind.PDB         => new PDBFileOverview((PDBFile) file, symbolAccessor),
    //                        FileKind.PortablePDB => new PortablePDBFileOverview((PortablePDBFile) file, symbolAccessor),
    //                        FileKind.OBJ         => new OBJFileOverview((OBJFile) file, symbolAccessor),
    //                        FileKind.LIB         => new LIBFileOverview((LIBFile) file, symbolAccessor),
    //                        FileKind.OMF         => new OMFFileOverview((OMFFile) file, symbolAccessor),
    //                        FileKind.OMFLIB      => new OMFLIBFileOverview((OMFLIBFile) file, symbolAccessor),
    //                    };
    //                }
    //            });

    //            var waitHandles = new[] { ((IAsyncResult) task).AsyncWaitHandle, progress.WaitHandle };

    //            while (true)
    //            {
    //                var result = WaitHandle.WaitAny(waitHandles);

    //                if (result == 0)
    //                    break;

    //                progress.DrainQueue();
    //            }

    //            WriteObject(task.Result);

    //            return;
    //        }

    //        using (var peFile = PEFile.FromFile(Path))
    //        using (var symbolAccessor = peFile.GetSymbolAccessor(progress: new PowerShellLocatorProgress(this)))
    //        {
    //            var overview = new PEFileOverview(peFile, symbolAccessor);

    //            WriteObject(overview);

    //            return;

    //            /* https://learn.microsoft.com/en-us/cpp/overview/compiler-versions?view=msvc-170
    //             * 
    //             * Note that the rich header versions follow the MSVC version, _not_ the Visual Studio version
    //             * 
    //             * _MSC_VER stores MMNN (major minor)
    //             * _MSC_FULL_VER stores MMNNBBBBB (major minor build)
    //             * From VS6 - VS2015, for major releases
    //             * - _MSC_VER increases by 100 and
    //             * - _MSC_FULL_VER increases by 10,000,000
    //             * 
    //             * and for minor releases
    //             * - _MSC_VER increases by 100 and
    //             * - _MSC_FULL_VER increases by 1,000,000
    //             * 
    //             * For example, VS2013 has
    //             * 
    //             * _MSC_VER: 1800
    //             * _MSC_VER_FULL: 180021005
    //             * 
    //             * VS2015 has
    //             * 
    //             * - _MSC_VER: 1900
    //             * - _MSC_VER_FULL: 190023026
    //             * 
    //             * the second digit from the left increased by 1
    //             * 
    //             * conversely, Visual Studio 2010 goes from 1000 to 1010 for a minor update.
    //             * 
    //             * 
    //             * 
    //             * 
    //             */

    //            //

    //            //
    //            //

    //            //
    //        }
    //    }
    //}
}
