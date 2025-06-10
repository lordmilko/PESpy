using System;
using System.IO;
using ClrDebug.PDB;
using PInvoke;

namespace PESpy.Tests
{
    /// <summary>
    /// Provides facilities for creating and interacting with PDBs
    /// via mspdbcore.dll which is shipped with Visual Studio.
    /// </summary>
    internal class VsPDB : IDisposable
    {
        private static MsPdbCore pdbCore;

        static unsafe VsPDB()
        {
            if (IntPtr.Size != 8)
                throw new InvalidOperationException("mspdbcore.dll requires 64-bit Test Host");

            var vsRoot = "C:\\Program Files\\Microsoft Visual Studio\\2022\\Enterprise\\Common7\\IDE";

            if (!Directory.Exists(vsRoot))
                throw new InvalidOperationException("Visual Studio 2022 is not installed");

            var path = IntPtr.Size == 4
                ? "C:\\Program Files\\Microsoft Visual Studio\\2022\\Enterprise\\VC\\Tools\\MSVC\\14.42.34433\\bin\\Hostx86\\x86\\mspdbcore.dll"
                : "C:\\Program Files\\Microsoft Visual Studio\\2022\\Enterprise\\VC\\Tools\\MSVC\\14.42.34433\\bin\\Hostx64\\x64\\mspdbcore.dll";

            var hModule = Kernel32.LoadLibraryExW(path, LOAD_LIBRARY_FLAGS.LOAD_WITH_ALTERED_SEARCH_PATH);

            pdbCore = new MsPdbCore(hModule);
        }

        public static VsPDB CreateMSF(string fileName)
        {
            var msf = pdbCore.MSFOpenW(fileName, true);

            return new VsPDB(fileName, msf);
        }

        public static VsPDB CreatePDB(string fileName)
        {
            var pdb = pdbCore.PDBOpen2W(fileName, PdbOpenMode.pdbWrite);

            return new VsPDB(fileName, pdb);
        }

        public string FileName { get; }

        public MSF MSF { get; private set; }

        public PDB1 PDB { get; private set; }

        private VsPDB(string fileName, MSF msf)
        {
            FileName = fileName;
            MSF = msf;
        }

        private VsPDB(string fileName, PDB1 pdb)
        {
            FileName = fileName;
            PDB = pdb;
        }

        public void Dispose()
        {
            PDB?.Dispose();
            PDB = null;

            MSF?.Dispose();
            MSF = null;
        }
    }
}
