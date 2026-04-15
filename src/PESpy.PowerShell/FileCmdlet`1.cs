using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management.Automation;
using System.Reflection;
using System.Threading;

namespace PESpy.PowerShell
{
    public abstract class FileCmdlet<T> : FileCmdlet where T : class, IFile
    {
        private static Dictionary<string, WeakReference<T>> _activeFiles;

        static FileCmdlet()
        {
            _activeFiles = new Dictionary<string, WeakReference<T>>(StringComparer.OrdinalIgnoreCase);
        }

        private string _path;

        //The user specifies an Index, and it gets converted into a Key
        [Parameter(Mandatory = true, ParameterSetName = ParameterSet.FromKey)]
        public SymStoreKey Index { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = ParameterSet.FromFile)]
        public T File { get; private set; }

        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, ParameterSetName = ParameterSet.FromPath)]
        [Alias("PSPath")] //ValueFromPipelineByPropertyName applies to this, and FileInfo objects have a PSPath
        public string Path
        {
            get
            {
                if (_path == null)
                    return null;

                //When you pipe in a file, the format will be like Microsoft.PowerShell.Core\FileSystem::C:\windows\system32\ntdll.dll
                //This method trims that
                if (SessionState.Path.IsProviderQualified(_path))
                {
                    var result = SessionState.Path.GetResolvedProviderPathFromPSPath(_path, out var provider);

                    Debug.Assert(result.Count == 1);

                    var path = result[0];
                    _path = path;

                    return path;
                }
                else
                {
                    if (System.IO.File.Exists(_path))
                        return _path;

                    //Try and find the specified file on the PATH
                    var paths = Environment.GetEnvironmentVariable("PATH").Split(System.IO.Path.PathSeparator);

                    foreach (var path in paths)
                    {
                        var combined = System.IO.Path.Combine(path, _path);

                        if (System.IO.File.Exists(combined))
                        {
                            _path = combined;
                            return combined;
                        }
                    }

                    //Return it as is; it's up to the caller to handle it
                    return _path;
                }
            }
            set => _path = value;
        }

        /// <summary>
        /// A cancellation token source to use with long running tasks that may need to be interrupted by Ctrl+C.
        /// </summary>
        private readonly CancellationTokenSource TokenSource = new CancellationTokenSource();

        internal CancellationToken CancellationToken => TokenSource.Token;

        private FileKind _requiredKind;
        private bool _dispose; //If we emit the file from this cmdlet, we can't dispose it

        protected FileCmdlet()
        {
            _requiredKind = typeof(T).Name switch
            {
                nameof(PEFile) => FileKind.PE,
                nameof(PDBFile) => FileKind.PDB
            };
        }

        protected override void ProcessRecord()
        {
            string path = null;

            switch (ParameterSetName)
            {
                case ParameterSet.FromKey:
                    var progress = new PowerShellLocatorProgress(this);

                    if (Locator.TryLocate(Index, out path, progress: progress, cancellationToken: CancellationToken))
                    {
                        goto case ParameterSet.FromPath;
                    }
                    else
                    {
                        WriteWarning($"Failed to resolve index '{Index}'");
                        return;
                    }

                case ParameterSet.FromPath:
                    if (!GetFileFromPath(path ?? Path))
                        return;
                    break;

                case ParameterSet.FromFile:
                    break;

                default:
                    throw new NotImplementedException($"Don't know how to handle parameter set '{ParameterSetName}'");
            }

            ProcessRecordEx();
        }

        private bool GetFileFromPath(string path)
        {
            if (_activeFiles.TryGetValue(path, out var weakRef))
            {
                if (weakRef.TryGetTarget(out var existingFile))
                {
                    File = existingFile;
                    return true;
                }
            }

            if (!Detector.TryDetectFile(path, out var kind, out var subKind))
            {
                WriteWarning($"Cannot detect type of file '{path}'");
                return false;
            }

            var originalPath = path;

            if (kind != _requiredKind)
            {
                //If the user specified a PEFile for a PDB, try and locate the PDB
                if (!TryResolveFileKind(ref path, kind))
                {
                    WriteWarning($"[{kind}] Ignoring {path}");
                    return false;
                }
                else
                {
                    //Try again with the new path
                    if (GetFileFromPath(path))
                    {
                        //If the user specified an *.exe file for their *.pdb file, map that as well
                        //so we don't have to keep looking up the real file path. This is safe because FileCmdlet
                        //is a generic type, so FileCmdlet<PEFile> will have its own _activeFiles list
                        _activeFiles[originalPath] = new WeakReference<T>(File);
                        return true;
                    }

                    return false;
                }
            }

            //It's up to GC to close the file
            File = (T) Detector.OpenFile(path);

            _activeFiles[path] = new WeakReference<T>(File);

            return true;
        }

        protected bool TryResolveFileKind(ref string path, FileKind actualKind)
        {
            if (_requiredKind == FileKind.PDB)
            {
                switch (actualKind)
                {
                    case FileKind.DOS:
                    case FileKind.NE:
                    case FileKind.LE:
                    case FileKind.PE:
                    case FileKind.DBG:
                        var progress = new PowerShellLocatorProgress(this);

                        if (Locator.TryLocatePDB(path, out path, progress: progress))
                            return true;

                        break;
                }
            }

            return false;
        }

        protected abstract void ProcessRecordEx();

        protected override void StopProcessing()
        {
            TokenSource.Cancel();

            //EndProcessing does not run on Ctrl+C
            if (_dispose)
                File.Dispose();
        }

        protected override void EndProcessing()
        {
            //Note that this does _not_ run on Ctrl+C

            if (_dispose)
                File.Dispose();
        }

        public new void WriteObject(object sendToPipeline, bool enumerateCollection)
        {
            _dispose = false;
            base.WriteObject(sendToPipeline, enumerateCollection);
        }

        public new void WriteObject(object sendToPipeline)
        {
            _dispose = false;
            base.WriteObject(sendToPipeline);
        }
    }
}
