using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PESpy.Tests
{
    class MSVC
    {
        private string testDir;
        private string cppFile;
        private string objFile;
        private string libFile;
        private string exeFile;
        private string pdbFile;

        private string msvcPath;

        private bool ltcg;

        public MSVC(string testName, string text, bool ltcg)
        {
            //var rootDir = Path.Combine(Path.GetTempPath(), "PESpyTest");
            var rootDir = "C:\\";
            this.ltcg = ltcg;

            if (!Directory.Exists(rootDir))
            {
                Directory.CreateDirectory(rootDir);
            }

            testDir = Path.Combine(rootDir, testName);

            if (!Directory.Exists(testDir))
            {
                Directory.CreateDirectory(testDir);
            }

            cppFile = Path.Combine(testDir, $"{testName}.cpp");
            objFile = Path.Combine(testDir, $"{testName}.obj");
            libFile = Path.Combine(testDir, $"{testName}.lib");
            exeFile = Path.Combine(testDir, $"{testName}.exe");
            pdbFile = Path.Combine(testDir, $"{testName}.pdb");

            if (!File.Exists(cppFile))
            {
                File.WriteAllText(cppFile, text);
            }

            //Ideally, we should not hard code the path to MSVC but instead do the same thing that the .NET ILCompiler does: it calls findvcvarsall.bat
            //with the arg x86 or x64 and then invokes vcvarsall.bat and searches for the directory containing link.exe

            msvcPath = "C:\\Program Files (x86)\\Microsoft Visual Studio\\2019\\Enterprise\\VC\\Tools\\MSVC\\14.29.30133\\bin\\HostX86\\x86";
        }

        public string Compile() //When we compile with /GL, we get new-style OBJ file. Otherwise, we get old style
        {
            if (File.Exists(objFile))
                return objFile;

            var args = new List<string>
            {
                "/c", //Compile only
                "/Zi", //Emit PDB
                "/nologo",
                //"/W3", //Warning level 3
                //"/WX-", //No warnings as errors
                //"/diagnostics:column",
                "/sdl-", //Disable Security Development Lifecycle checks
                "/O2", //Optimizations Level 2
                "/Oy-", //Disable frame pointer omission
                "/D WIN32 /D NDEBUG /D _CONSOLE /D _UNICODE /D UNICODE", //Macro defines
                //"/Gm-", //Disable minimal rebuild
                "/EHsc", //Assumes extern "C" functions never throw exceptions
                "/MD", //MD link with MSVCRT.LIB
                "/GS-", //Disable security checks
                "/Gy-", //Disable separate functions for linker
                "/fp:precise", //Floating point model
                //"/permissive-", //Disable being extra permissive with non-conformant code
                "/Zc:wchar_t /Zc:forScope /Zc:inline", //Language conformance
                $"/Fo\"{objFile}\"", //Object File Name. When it ends in two slashes (escaped to 4 here) it will automatically generate the file name
                //"/Fd\"Z:\\test\\Release\\vc142.pdb\"", //PDB File Name. This provides input information
                //"/W3", //Warning level for external headers
                "/Gd", //cdecl calling convention
                "/TP", //Compile all files as .cpp
                "/analyze-", //Disable native analysis
                "/FC", //Use full path names in diagnostics
                //"/errorReport:queue",
                $"\"{cppFile}\""
            };

            if (ltcg)
                args.Add("/GL");

            var psi = new ProcessStartInfo(Path.Combine(msvcPath, "CL.exe"));
            psi.Arguments = string.Join(" ", args);
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;

            var process = Process.Start(psi);
            process.WaitForExit();

            var stdout = process.StandardOutput.ReadToEnd();
            var stderr = process.StandardError.ReadToEnd();

            if (!File.Exists(objFile))
                Assert.Fail(stderr);

            return objFile;
        }

        public void Link()
        {
            var args = new List<string>
            {
                "/ERRORREPORT:QUEUE",
                $"/OUT:\"{exeFile}\"",
                "/INCREMENTAL:NO",
                "/NOLOGO",
                //"kernel32.lib user32.lib gdi32.lib winspool.lib comdlg32.lib advapi32.lib shell32.lib ole32.lib oleaut32.lib uuid.lib odbc32.lib odbccp32.lib",
                "/NODEFAULTLIB",
                //"/MANIFEST",
                //"/MANIFESTUAC:\"level='asInvoker' uiAccess='false'\"",
                //"/manifest:embed",
                "/DEBUG",
                $"/PDB:\"{pdbFile}\"",
                "/SUBSYSTEM:CONSOLE",
                "/OPT:REF", //Eliminate unreferenced COMDATs
                "/OPT:ICF", //Fold identical COMDATs
                "/TLBID:1", //Type Library ID
                "/ENTRY:\"main\"",
                "/DYNAMICBASE",
                "/NXCOMPAT", //Indicates that an executable is compatible with the Windows Data Execution Prevention feature
                $"/IMPLIB:\"{libFile}\"",
                "/MACHINE:X86",
                "/SAFESEH",
                objFile,
            };

            //If you don't specify /LTCG you get a warning "MSIL .netmodule or module compiled with /GL found; restarting link with /LTCG; add /LTCG to the link command line to improve linker performance"
            if (ltcg)
                args.Add("/LTCG");

            var psi = new ProcessStartInfo(Path.Combine(msvcPath, "link.exe"));
            psi.Arguments = string.Join(" ", args);
            psi.RedirectStandardOutput = true;

            var process = Process.Start(psi);
            process.WaitForExit();

            var str = process.StandardOutput.ReadToEnd();
        }
    }
}
