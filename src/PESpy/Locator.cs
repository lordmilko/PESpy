using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PESpy
{
    /* Locator that attempts to implement the same logic as microsoft-pdb/DIA
     *
     * There are three kinds of symbols that can be used with PE files
     * - COFF
     * - CodeView (OMF)
     * - PDB
     *
     * Microsoft refers to OMF-style CodeView symbols as simply "CodeView", however that is very confusing to modern developers who typically consider
     * IMAGE_DEBUG_TYPE_CODEVIEW to be synonymous with PDBs. As such, we will refer to these types of symbols as OMF
     *
     * All three of these symbol types may exist in the PE File's debug table, or may have been split out into a separate *.dbg file.
     *
     * The principal purpose of microsoft-pdb's locator is to locate PDBs. There are three steps involved in locating a PDB
     *
     * 1. CrackExeOrDbg - analyze the headers of an *.exe or *.dbg file to find an IMAGE_DEBUG_TYPE_CODEVIEW record that points to a *.pdb file, or in the case of an *.exe file only,
     *                    find an IMAGE_DEBUG_TYPE_MISC that points to a *.dbg file (that we're hoping may in turn point to a *.pdb file)
     *
     *                    Modern versions of LOCATOR also include support for IMAGE_DEBUG_TYPE_EMBEDDED_PORTABLE_PDB
     *
     * 2. LocateDbg     - attempt to find the *.dbg file that was pointed to by the *.exe file. If the original input file was a *.dbg file, or the *.exe file did not
     *                    point to a *.dbg file, there's nothing to do here
     * 3. LocatePdb     - attempt to find the *.pdb file that was pointed to by either the *.exe file (from Step 1) or the *.dbg file (from either Step 1 or Step 2)
     *
     * Note that while LOCATOR may support passing in either an *.exe or *.dbg file, IDiaDataSource::loadDataForExe does not, because it's going to try
     * and validate the found PDB against the headers of the input file, which is not an exe
     *
     * The rules for locating *.dbg files are as follows
     *
     * 1. Is the target in the same folder as its parent (directory of parent + filename of parent + ".dbg"). Observe that the misc path is ignored, and that unlike PDBs
     *    DBG files do not ever have full paths listed, so we don't check the full original path
     * 3a. Is the target in any sub-string of the user supplied search path, %_NT_ALT_SYMBOL_PATH%, %_NT_SYMBOL_PATH% or %SystemRoot% at
     *
     *     If misc path is present:
     *
     *         C:\foo\symbols\<miscPath>
     *         C:\foo\<miscPath>
     *
     *         i.e. if misc path is exe\Test.dbg, this will try
     *
     *             C:\foo\symbols\exe\Test.dbg
     *             C:\foo\exe\Test.dbg
     *
     *         Note that we don't try and be "smart" and look at C:\foo\symbols\Text.dbg. We look directly at the misc path we're given. Special logic
     *         for handling the original EXE file extension does not apply
     *
     *     if misc path is not present:
     *
     *         C:\foo\symbols\<peFileExt>\<exeFileName>.dbg
     *         C:\foo\<peFileExt>\<exeFileName>.dbg
     *         C:\foo\exeFileName>.dbg
     *
     * The rules for locating *.pdb files are as follows
     *
     * 1. Is the target in the same folder as its parent (directory of parent + filename + extension listed in the IMAGE_DEBUG_TYPE_CODEVIEW record)
     * 2. Is the target at the literal path pointed to by the IMAGE_DEBUG_TYPE_CODEVIEW record? Sometimes this is an absolute path, sometimes relative
     * 3s. Is the target in any sub-string of the user supplied search path, %_NT_ALT_SYMBOL_PATH%, %_NT_SYMBOL_PATH% or %SystemRoot% at
     *     C:\foo\symbols\<ext>\<pdbFileName> (where <ext> is the file extension of the PE File if one was specified)
     *     C:\foo\<ext>\\<pdbFileName
     *     C:\foo\<pdbFileName>
     *
     *     where <pdbFileName> is the file name and extension listed in the IMAGE_DEBUG_TYPE_CODEVIEW record
     *
     * 3b. Or, if a sub-string of a user supplied search path begins with srv*, symsrv* or cache*:
     *
     *     Does the name + extension specified in the IMAGE_DEBUG_TYPE_CODEVIEW record exist on the symbol server? Any leading directory in the path should be stripped
     *
     * _NT_ALT_SYMBOL_PATH _is_ meant to come before _NT_SYMBOL_PATH; the purpose of it is to override the normal symbol path to point to private symbols that aren't available
     * in the normal _NT_SYMBOL_PATH. (Also note that while there is a method LOCATOR::FLocateDbgServer, this method is now empty. All of the symsrv handling logic is carried out in steps 2-5)
     */

    public static class Locator
    {
        private static string[] environmentNames =
        {
            "_NT_ALT_SYMBOL_PATH",
            "_NT_SYMBOL_PATH",
            "SystemRoot"
        };

        #region String -> Artifacts

        //Locate all artifacts associated with a given *.exe or *.dbg file
        public static bool TryLocate(
            string exeOrDbgPath,
            out Artifacts result,
            out SymStoreKey? symStoreKey,
            string? searchPath = null,
            ILocatorProgress progress = null)
        {
            Artifacts? artifacts;
            (artifacts, symStoreKey) = TryLocateAsync(exeOrDbgPath, searchPath, progress).ConfigureAwait(false).GetAwaiter().GetResult();

            if (artifacts != null)
            {
                result = artifacts.Value;
                return true;
            }

            result = default;
            return false;
        }

        public static ValueTask<(Artifacts? artifacts, SymStoreKey? symStoreKey)> TryLocateAsync(string exeOrDbgPath, string? searchPath = null, ILocatorProgress progress = null, CancellationToken cancellationToken = default) =>
            LocateInternalAsync(exeOrDbgPath, null, SearchFlags.All, searchPath, progress, cancellationToken);

        #endregion
        #region IFile -> Artifacts

        public static bool TryLocate(
            IFile exeOrDbgFile,
            out Artifacts result,
            out SymStoreKey? symStoreKey, //If the file was found on the symbol store, contains the key that was used to identify the file
            string? searchPath = null,
            ILocatorProgress? progress = null)
        {
            Artifacts? artifacts;
            (artifacts, symStoreKey) = TryLocateAsync(exeOrDbgFile, searchPath, progress).ConfigureAwait(false).GetAwaiter().GetResult();

            if (artifacts != null)
            {
                result = artifacts.Value;
                return true;
            }

            result = default;
            return false;
        }

        public static ValueTask<(Artifacts? artifacts, SymStoreKey? symStoreKey)> TryLocateAsync(
            IFile exeOrDbgFile,
            string? searchPath = null,
            ILocatorProgress? progress = null,
            CancellationToken cancellationToken = default)
        {
            if (exeOrDbgFile == null)
                throw new ArgumentNullException(nameof(exeOrDbgFile));

            return LocateInternalAsync(null, exeOrDbgFile, SearchFlags.All, searchPath, progress, cancellationToken);
        }

        #endregion
        #region SymStoreKey -> String

        public static string Locate(SymStoreKey key, ILocatorProgress progress = null)
        {
            if (!TryLocate(key, out var result, progress))
                throw new FileNotFoundException($"Failed to locate the file associated with {nameof(SymStoreKey)} '{key}'");

            return result;
        }

        //Locate a file with a given key from the symbol server
        public static bool TryLocate(SymStoreKey key, out string? result, ILocatorProgress progress = null)
        {
            result = TryLocateAsync(key, progress).ConfigureAwait(false).GetAwaiter().GetResult();

            return result != null;
        }

        public static async ValueTask<string?> TryLocateAsync(
            SymStoreKey key,
            ILocatorProgress? progress = null,
            CancellationToken cancellationToken = default)
        {
            var nameToLocate = Path.GetFileNameWithoutExtension(key.Index);
            var extToUse = Path.GetExtension(key.Index);

            foreach (var environmentName in environmentNames)
            {
                var environmentPath = Environment.GetEnvironmentVariable(environmentName);

                var result = await LocateFileInPathAsync(nameToLocate, environmentPath, extToUse, key, null, progress, cancellationToken).ConfigureAwait(false);

                if (result.fileInPath != null)
                    return result.fileInPath;
            }

            return default;
        }

        #endregion
        #region String -> String (DBG)

        //Locate the *.dbg file associated with a given *.exe, or returns the *.dbg file itself
        public static bool TryLocateDBG(
            string exeOrDbgPath,
            out string? result,
            string? searchPath = null,
            ILocatorProgress? progress = null,
            CancellationToken cancellationToken = default)
        {
            result = TryLocateDBGAsync(exeOrDbgPath, searchPath, progress, cancellationToken).ConfigureAwait(false).GetAwaiter().GetResult();

            return result != null;
        }

        public static async ValueTask<string?> TryLocateDBGAsync(
            string exeOrDbgPath,
            string? searchPath = null,
            ILocatorProgress? progress = null,
            CancellationToken cancellationToken = default)
        {
            var (artifacts, _) = await LocateInternalAsync(exeOrDbgPath, null, SearchFlags.DBG, searchPath, progress, cancellationToken).ConfigureAwait(false);

            if (artifacts != null)
                return artifacts.Value.DBGPath;

            return default;
        }

        #endregion
        #region String -> String (PDB)

        //Locate the *.pdb associated with a given *.exe or *.dbg file

        public static string LocatePDB(SymStoreKey exeOrDbgKey, ILocatorProgress? progress = null)
        {
            var exeOrDbgPath = Locate(exeOrDbgKey, progress);

            return LocatePDB(exeOrDbgPath);
        }

        public static string LocatePDB(string exeOrDbgPath, string? searchPath = null, ILocatorProgress? progress = null)
        {
            if (!TryLocatePDB(exeOrDbgPath, out var result, searchPath, progress))
                throw new FileNotFoundException($"Failed to locate the PDB associated with file '{exeOrDbgPath}'");

            return result;
        }

        public static bool TryLocatePDB(string exeOrDbgPath, out string? result, string? searchPath = null, ILocatorProgress? progress = null)
        {
            result = TryLocatePDBAsync(exeOrDbgPath, searchPath, progress).ConfigureAwait(false).GetAwaiter().GetResult();

            return result != null;
        }

        public static async ValueTask<string?> TryLocatePDBAsync(
            string exeOrDbgPath,
            string? searchPath = null,
            ILocatorProgress? progress = null,
            CancellationToken cancellationToken = default)
        {
            var (artifacts, _) = await LocateInternalAsync(exeOrDbgPath, null, SearchFlags.PDB, searchPath, progress, cancellationToken).ConfigureAwait(false);

            if (artifacts != null)
                return artifacts.Value.PDBPath;

            return default;
        }

        #endregion
        #region String -> IFile (PDB)

        public static bool TryLocatePDB(IFile exeOrDbgFile, out string? result, string? searchPath = null, ILocatorProgress? progress = null)
        {
            result = TryLocatePDBAsync(exeOrDbgFile, searchPath, progress).ConfigureAwait(false).GetAwaiter().GetResult();

            return result != null;
        }

        public static async ValueTask<string?> TryLocatePDBAsync(
            IFile exeOrDbgFile,
            string? searchPath = null,
            ILocatorProgress? progress = null,
            CancellationToken cancellationToken = default)
        {
            if (exeOrDbgFile == null)
                throw new ArgumentNullException(nameof(exeOrDbgFile));

            var (artifacts, _) = await LocateInternalAsync(null, exeOrDbgFile, SearchFlags.PDB, searchPath, progress, cancellationToken).ConfigureAwait(false);

            if (artifacts != null)
                return artifacts.Value.PDBPath;

            return default;
        }

        #endregion
        #region String -> EmbeddedPortablePdb

        //Locate the embedded portable PDB associated with a given *.exe or *.dbg file. Realistically,
        //it should not be possible to have an Embedded Portable PDB inside a *.dbg file (since *.dbg files predate MPDB's)
        public static bool TryLocateEmbeddedPortablePDB(
            string exeOrDbgPath,
            out int? debugDirectoryIndex,
            string? searchPath = null,
            ILocatorProgress? progress = null,
            CancellationToken cancellationToken = default)
        {
            var (artifacts, _) = LocateInternalAsync(exeOrDbgPath, null, SearchFlags.MPDB, searchPath, progress, cancellationToken).ConfigureAwait(false).GetAwaiter().GetResult();

            if (artifacts != null)
            {
                debugDirectoryIndex = artifacts.Value.EmbeddedPortablePdbIndex;
                return debugDirectoryIndex != null;
            }

            debugDirectoryIndex = default;
            return false;
        }

        #endregion

        class LocatorContext
        {
            public ImageDebugDirectory[]? DebugTable;
            public SearchFlags Flags;
            public IFile File;
            public string? SearchPath;
            public string? PEFileExt;
            public bool Stripped;
            public bool NGEN;

            public uint PETimeDateStamp;
            public int PESizeOfImage;

            //If a file was ultimately located using a key, stores the key that was used in the locating
            public SymStoreKey? SymStoreKey;

            public string? DBGFilePath;
            public string? PDBFilePath;
            public int? MPDBIndex;
        }

        private static async ValueTask<(Artifacts? artifacts, SymStoreKey? symStoreKey)> LocateInternalAsync(
            string? exeOrDbgPath,
            IFile? file,
            SearchFlags flags,
            string? searchPath,
            ILocatorProgress progress,
            CancellationToken cancellationToken)
        {
            if (exeOrDbgPath == null && file == null)
                throw new ArgumentNullException(nameof(exeOrDbgPath));

            var ownsPEFile = true;

            if (file != null)
            {
                ownsPEFile = false;
                exeOrDbgPath = file.FileName;

                if (exeOrDbgPath == null)
                    throw new ArgumentException("The specifid file does not have a FileName");
            }

            var state = State.None;

            var ctx = new LocatorContext
            {
                Flags = flags,
                SearchPath = searchPath,
            };

            var run = true;

            try
            {
                while (run)
                {
                    switch (state)
                    {
                        case State.None:
                            if ((ctx.File = file!) != null || Detector.TryOpenFile(exeOrDbgPath, out ctx.File))
                            {
                                switch (ctx.File.Kind)
                                {
                                    case FileKind.PE:
                                        var peFile = (PEFile) ctx.File;
                                        ctx.DebugTable = peFile.DebugTable;
                                        state = State.ReadDebugTable;
                                        ctx.PEFileExt = Path.GetExtension(exeOrDbgPath).TrimStart('.');
                                        ctx.Stripped = (peFile.FileHeader.Characteristics & ImageFile.DebugStripped) != 0;
                                        ctx.PETimeDateStamp = peFile.FileHeader.TimeDateStamp;
                                        ctx.PESizeOfImage = peFile.OptionalHeader.SizeOfImage;

                                        if (exeOrDbgPath!.EndsWith(".ni.exe") || exeOrDbgPath.EndsWith(".ni.dll"))
                                        {
                                            //Possible NGEN file
                                            if (peFile.NgenHeader != null)
                                                ctx.NGEN = true;
                                        }

                                        break;

                                    case FileKind.DBG:
                                        ctx.DBGFilePath = exeOrDbgPath;
                                        ctx.DebugTable = ((DBGFile) ctx.File).DebugTable;
                                        state = State.ReadDebugTable;
                                        break;

                                    default:
                                        return default;
                                }
                            }
                            else
                                run = false;
                            break;

                        case State.ReadDebugTable:
                            if ((state = await ReadDebugTableAsync(ctx, progress, cancellationToken).ConfigureAwait(false)) == State.None)
                                run = false;
                            break;

                        case State.ReadMiscDebugTable: //An *.exe file pointed to a *.dbg file. Read the debug table of that *.dbg file
                            Debug.Assert(ctx.DBGFilePath != null);

                            if (Detector.TryOpenFile(ctx.DBGFilePath, out var dbgFile) && dbgFile is DBGFile d)
                            {
                                if (!ownsPEFile)
                                {
                                    //We're replacing the file with the DBGFile, so we _will_ need to dispose that when we're done
                                    ownsPEFile = false;
                                }
                                ctx.File = d;

                                ctx.Stripped = false;
                                ctx.DebugTable = d.DebugTable;

                                //Now read the debug table like normal
                                goto case State.ReadDebugTable;
                            }
                            else
                            {
                                run = false;
                                break;
                            }

                        case State.End:
                            run = false;
                            break;

                        default:
                            throw new NotImplementedException($"Don't know how to handle {nameof(State)} '{state}'");
                    }
                }

                //Success is dictated by whether we found anything
                if (ctx.DBGFilePath == null && ctx.PDBFilePath == null && ctx.MPDBIndex == null)
                    return default;

                ArtifactKind bestKind;

                if (ctx.PDBFilePath != null)
                    bestKind = ArtifactKind.PDB;
                else if (ctx.MPDBIndex != null)
                    bestKind = ArtifactKind.EmbeddedPortablePdb;
                else
                {
                    Debug.Assert(ctx.DBGFilePath != null);
                    bestKind = ArtifactKind.DBG;
                }

               return (new Artifacts(bestKind, ctx.DBGFilePath, ctx.PDBFilePath, ctx.MPDBIndex), ctx.SymStoreKey);
            }
            finally
            {
                if (ownsPEFile)
                    ctx.File?.Dispose();
            }
        }

        private static async ValueTask<State> ReadDebugTableAsync(LocatorContext ctx, ILocatorProgress progress, CancellationToken cancellationToken)
        {
            if (ctx.DebugTable == null)
                return default;

            for (int i = 0; i < ctx.DebugTable.Length; i++)
            {
                ImageDebugDirectory debugDir = ctx.DebugTable[i];

                switch (debugDir.Type)
                {
                    case ImageDebugType.Misc:
                        //It's possible for a *.dbg file to have a misc entry pointing back to the *.exe file. We only process misc entries when we're stripped,
                        //meaning we're processing the original PE file
                        if (ctx.Stripped && (ctx.Flags & SearchFlags.DBG) != 0 && debugDir.Data is ImageDebugMisc m)
                        {
                            var str = m.Data.ToString();

                            var symSrvIndex = SymStoreKey.FromMisc(str, ctx.PETimeDateStamp, ctx.PESizeOfImage);

                            var path = await LocateDBGFileAsync(ctx.File.FileName!, str, ctx.SearchPath, ctx.PEFileExt, symSrvIndex, progress, cancellationToken).ConfigureAwait(false);

                            //If we've already found a file in another record, don't blow it away because we didn't find one in this one
                            if (path.filePath != null)
                            {
                                ctx.DBGFilePath = path.filePath;
                                ctx.SymStoreKey = path.keyUsed;
                                return State.ReadMiscDebugTable;
                            }
                        }
                        break;

                    case ImageDebugType.CodeView:
                        if ((ctx.Flags & SearchFlags.PDB) != 0 && debugDir.Data is ICodeViewPDB c)
                        {
                            var pdbName = c.Path.ToString();

                            if (ctx.NGEN)
                            {
                                if (!pdbName.EndsWith(".ni.pdb"))
                                    continue;
                            }
                            else
                            {
                                if (pdbName.EndsWith(".ni.pdb"))
                                    continue;
                            }

                            /* Per LOCATOR::FLocatePdb, if the debug directory's minor version is PORTABLE_PDB_MINOR_VERSION (20557) then
                             * this indicates that the PDB may in fact be a portable PDB, which can be resolved on the symbol server by
                             * specifying an age of -1. LOCATOR::FLocatePdb tries for a normal PDB first, and if that fails tries for
                             * a portable one. Visual Studio does not use LOCATOR, and if it sees that PORTABLE_PDB_MINOR_VERSION is present,
                             * it may attempt to do a direct lookup with a negative age specified */
                            SymStoreKey symSrvIndex;
                            SymStoreKey? altSymSrvIndex = null;

                            if (c is RSDSI r)
                            {
                                var rsdsPath = r.Path.ToString();

                                symSrvIndex = SymStoreKey.FromRSDSI(rsdsPath, r.Guid, r.Age);

                                if (debugDir.IsPortablePDB)
                                    altSymSrvIndex = SymStoreKey.FromRSDSI(rsdsPath, r.Guid, -1);
                            }
                            else
                            {
                                //I would not expect anything using NB10 to be using portable PDBs
                                symSrvIndex = SymStoreKey.FromNB10((NB10I) c);
                            }

                            var path = await LocatePDBFileAsync(ctx.File.FileName!, pdbName, ctx.SearchPath, ctx.PEFileExt, symSrvIndex, altSymSrvIndex, progress, cancellationToken).ConfigureAwait(false);

                            //If we've already found a file in another record, don't blow it away because we didn't find one in this one
                            if (path.fileInPath != null)
                            {
                                ctx.PDBFilePath = path.fileInPath;
                                ctx.SymStoreKey = path.keyUsed;
                                return State.End;
                            }
                        }
                        break;

                    case ImageDebugType.EmbeddedPortablePdb:
                        if ((ctx.Flags & SearchFlags.MPDB) != 0 && debugDir.Data is EmbeddedPortablePdb)
                        {
                            ctx.MPDBIndex = i;
                            return State.End;
                        }
                        break;
                }
            }

            return default;
        }

        //This method is only called when we have an IMAGE_DEBUG_MISC entry, and a *.dbg file should not have an IMAGE_DEBUG_MISC pointing to another file
        private static async ValueTask<(string? filePath, SymStoreKey? keyUsed)> LocateDBGFileAsync(
            string parentFullName,
            string rawDbgName,
            string? searchPath,
            string? peFileExt,
            SymStoreKey symSrvIndex,
            ILocatorProgress progress,
            CancellationToken cancellationToken)
        {
            //First, check for a file with the PE file name + ".dbg" in the same directory as the parent *.exe file
            var peBaseName = Path.GetFileNameWithoutExtension(parentFullName);

            var parentDir = Path.GetDirectoryName(parentFullName);

            var syntheticDbgName = peBaseName + ".dbg";

            var candidateDbgPath = Path.Combine(parentDir, syntheticDbgName);

            if (File.Exists(candidateDbgPath))
                return (candidateDbgPath, null);

            //We don't try string specified in the IMAGE_DEBUG_MISC directly; unlike with PDBs, it's never the full path to the *.dbg file

            string nameToLocate;
            string? extToUse = null;

            if (!string.IsNullOrEmpty(rawDbgName))
            {
                //We have a misc path. Try it as is
                nameToLocate = rawDbgName;
            }
            else
            {
                //We don't have a misc path. Synthesize a name from the PE file name
                nameToLocate = syntheticDbgName;
                extToUse = peFileExt; //We allow searching under an extension directory in this case
            }

            /* Now try search paths. There are four paths we try:
             * - the user supplied search path
             * - %_NT_ALT_SYMBOL_PATH%
             * - %_NT_SYMBOL_PATH%
             * - %SystemRoot%
             *
             * A search path is a sequence of semicolon delimited values describing locations that we should search
             * for the file. %SystemRoot% will just be C:\Windows, but the other 3 could be complex expressions like
             *
             * D:\MySymbols;srv*c:\symbols*http://msdl.microsoft.com/download/symbols
             */

            var result = await LocateFileInPathAsync(nameToLocate, searchPath, extToUse, symSrvIndex, null, progress, cancellationToken).ConfigureAwait(false);

            if (result.fileInPath != null)
                return result;

            foreach (var environmentName in environmentNames)
            {
                var environmentPath = Environment.GetEnvironmentVariable(environmentName);

                result = await LocateFileInPathAsync(nameToLocate, environmentPath, extToUse, symSrvIndex, null, progress, cancellationToken).ConfigureAwait(false);

                if (result.fileInPath != null)
                    return result;
            }

            //File was not found in any search path
            return default;
        }

        /// <summary>
        /// Top-level method for locating the path to a PDB file
        /// </summary>
        /// <param name="parentFullName">The full path to the parent *.exe or *.dbg file that contained an IMAGE_DEBUG_TYPE_CODEVIEW record pointing to a PDB.</param>
        /// <param name="rawPdbName">The raw PDB name listed in the IMAGE_DEBUG_TYPE_CODEVIEW record. This may simply be a file name + extension or an absolute path to a file.</param>
        /// <param name="searchPath">A semicolon delimited list of search paths to search for the PDB in.</param>
        /// <param name="peFileExt">If the original file we were asked to locate was a PE file, the file extension of that file.</param>
        /// <param name="symSrvIndex">The index of the PDB file on the symbol server.</param>
        /// <param name="altSymSrvIndex">The index of an alternate PDB file to locate on the symbol server if <paramref name="symSrvIndex"/> can't be found</param>
        /// <param name="progress">A callback that can receive notice of progress while copying files between stores.</param>
        /// <param name="cancellationToken">A token that can be used to receive notice of cancellation.</param>
        /// <returns>The path to the PDB file on the search path, or <see langword="null"/> if a PDB was not found.</returns>
        private static async ValueTask<(string? fileInPath, SymStoreKey? keyUsed)> LocatePDBFileAsync(
            string parentFullName,
            string rawPdbName,
            string? searchPath,
            string? peFileExt,
            SymStoreKey symSrvIndex,
            SymStoreKey? altSymSrvIndex,
            ILocatorProgress progress,
            CancellationToken cancellationToken)
        {
            //First, check for a file with the base name + extension of the file listed in the IMAGE_DEBUG_TYPE_CODEVIEW record
            //in the same directory as the parent *.exe or *.dbg file
            var pdbNameWithoutDir = Path.GetFileName(rawPdbName);

            var parentDir = Path.GetDirectoryName(parentFullName)!;

            var candidatePdbPath = Path.Combine(parentDir, pdbNameWithoutDir);

            if (File.Exists(candidatePdbPath))
                return (candidatePdbPath, null);

            //Next, try the original path listed in the IMAGE_DEBUG_TYPE_CODEVIEW record. Sometimes PDBs embed an absolute path
            if (File.Exists(rawPdbName))
                return (rawPdbName, null);

            /* Now try search paths. There are four paths we try:
             * - the user supplied search path
             * - %_NT_ALT_SYMBOL_PATH%
             * - %_NT_SYMBOL_PATH%
             * - %SystemRoot%
             *
             * A search path is a sequence of semicolon delimited values describing locations that we should search
             * for the file. %SystemRoot% will just be C:\Windows, but the other 3 could be complex expressions like
             *
             * D:\MySymbols;srv*c:\symbols*http://msdl.microsoft.com/download/symbols
             */

            var result = await LocateFileInPathAsync(pdbNameWithoutDir, searchPath, peFileExt, symSrvIndex, altSymSrvIndex, progress, cancellationToken).ConfigureAwait(false);

            if (result.fileInPath != null)
                return result;

            foreach (var environmentName in environmentNames)
            {
                var environmentPath = Environment.GetEnvironmentVariable(environmentName);

                result = await LocateFileInPathAsync(pdbNameWithoutDir, environmentPath, peFileExt, symSrvIndex, altSymSrvIndex, progress, cancellationToken).ConfigureAwait(false);

                if (result.fileInPath != null)
                    return result;
            }

            //File was not found in any search path
            return default;
        }

        private static ValueTask<(string? fileInPath, SymStoreKey? keyUsed)> LocateFileInPathAsync(
            string nameAndExt,
            string? searchPath,
            string? peFileExt,
            SymStoreKey symSrvIndex,
            SymStoreKey? altSymSrvIndex,
            ILocatorProgress progress,
            CancellationToken cancellationToken)
        {
            if (searchPath == null)
                return default;

            var remainingSearchPath = searchPath.AsSpan();

            using var builder = new ValueStringBuilder();

            var run = true;

            while (run)
            {
                var index = remainingSearchPath.IndexOf(";".AsSpan(), StringComparison.OrdinalIgnoreCase);

                ReadOnlySpan<char> currentPath;

                if (index == -1)
                {
                    run = false;
                    currentPath = remainingSearchPath; //Use the rest of the path

                    remainingSearchPath = default;
                }
                else
                {
                    currentPath = remainingSearchPath.Slice(0, index);

                    //Update the current position for the next loop round
                    remainingSearchPath = remainingSearchPath.Slice(index + 1);
                }

                bool symSrv = false;
                bool cache = false;

                if (currentPath.StartsWith("srv*".AsSpan(), StringComparison.OrdinalIgnoreCase))
                {
                    symSrv = true;
                    currentPath = currentPath.Slice(4);
                }
                else if (currentPath.StartsWith("symsrv*".AsSpan(), StringComparison.OrdinalIgnoreCase))
                {
                    //Only symsrv.dll is supported
                    if (!currentPath.StartsWith("symsrv*symsrv.dll*".AsSpan(), StringComparison.OrdinalIgnoreCase))
                        return default;

                    symSrv = true;
                    currentPath = currentPath.Slice(18);
                }

                if (symSrv || cache)
                {
                    return SymStore.GetFileAsync(currentPath, symSrvIndex, altSymSrvIndex, progress, cancellationToken);
                }
                else
                {
                    builder.Clear();

                    builder.Append(currentPath);

                    if (!currentPath.EndsWith("\\".AsSpan()) && !currentPath.EndsWith("/".AsSpan()))
                        builder.Append("\\");

                    var prefix = builder.Length;

                    const string symbolsPrefix = "symbols\\";

                    builder.Append(symbolsPrefix);

                    if (peFileExt != null)
                    {
                        builder.Append(peFileExt);
                        builder.Append('\\');
                    }

                    builder.Append(nameAndExt);

                    var str = builder.ToString();

                    if (File.Exists(str))
                    {
                        return new ValueTask<(string?, SymStoreKey?)>((str, null));
                    }

                    //Try without symbols\ prefix
                    builder.Remove(prefix, symbolsPrefix.Length);

                    str = builder.ToString();

                    if (File.Exists(str))
                    {
                        return new ValueTask<(string?, SymStoreKey?)>((str, null));
                    }

                    if (peFileExt != null)
                    {
                        //If a PE File extension was specified, try without that now too
                        builder.Remove(prefix, peFileExt.Length + 1);
                        str = builder.ToString();

                        if (File.Exists(str))
                        {
                            return new ValueTask<(string?, SymStoreKey?)>((str, null));
                        }
                    }
                }
            }

            return default;
        }

        [Flags]
        enum SearchFlags
        {
            DBG = 1,
            PDB = 2,
            MPDB = 4,
            All = DBG | PDB | MPDB
        }

        public readonly struct Artifacts
        {
            public ArtifactKind BestKind { get; }

            public string? DBGPath { get; }

            public string? PDBPath { get; }

            /// <summary>
            /// Gets the index of the <see cref="ImageDebugDirectory"/> that contains <see cref="EmbeddedPortablePdb"/> data.<para/>
            /// If this value is <see langword="null"/>, embedded portable PDB metadata is not available.
            /// </summary>
            public int? EmbeddedPortablePdbIndex { get; } //Note that this can't be the EmbeddedPortablePdb itself, because if only a path to a PEFile was passed in, we'll have opened and then closed the file

            public Artifacts(ArtifactKind bestKind, string? dbgPath, string? pdbPath, int? embeddedPortablePdbIndex)
            {
                BestKind = bestKind;
                DBGPath = dbgPath;
                PDBPath = pdbPath;
                EmbeddedPortablePdbIndex = embeddedPortablePdbIndex;
            }
        }

        public enum ArtifactKind
        {
            DBG = 1,
            EmbeddedPortablePdb = 2,
            PDB = 3,
        }

        public enum State
        {
            //The initial state. Read headers to find out what type of file it is
            None,

            //Read the debug table of the input *.exe or *.dbg file
            ReadDebugTable,

            //Read the debug table of a *.dbg file pointed to by an *.exe file
            ReadMiscDebugTable,

            End
        }
    }
}
