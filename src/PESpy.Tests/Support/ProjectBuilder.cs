using System;
using System.Diagnostics;
using System.IO;

namespace PESpy.Tests
{
    enum TestProcessKind
    {
        SingleFile,
        NativeAOT
    }

    internal class ProjectBuilder
    {
        public static string GetOrCreateSingleFile()
        {
            var dir = GetSourceDir("SingleFileTest");
            var exe = Path.Combine(dir, "bin\\Release\\net9.0\\win-x64\\publish\\SingleFileTest.exe");

            if (File.Exists(exe))
                return exe;

            //Note: the reason single file apps are so big are because singlefilehost.exe
            //has 6mb of crap
            Process.Start("dotnet", $"new console -f net9.0 -o \"{dir}\"").WaitForExit();

            Process.Start("dotnet", $"publish \"{dir}\" /p:PublishSingleFile=true /p:PublishTrimmed=true").WaitForExit();

            if (!File.Exists(exe))
                throw new NotImplementedException();

            return exe;
        }

        public static string GetOrCreateNativeAOT()
        {
            var dir = GetSourceDir("NativeAOTTest");
            var exe = Path.Combine(dir, "bin\\Release\\net9.0\\win-x64\\native\\NativeAOTTest.exe");

            if (File.Exists(exe))
                return exe;

            Process.Start("dotnet", $"new console --aot -f net9.0 -o \"{dir}\"").WaitForExit();

            var sourceFile = Path.Combine(dir, "Program.cs");

            File.WriteAllText(sourceFile, @"
using System.Diagnostics;

BindLifetimeToParentProcess();

while (true)
    Thread.Sleep(1);

static void BindLifetimeToParentProcess()
{
    var str = Environment.GetEnvironmentVariable(""PESPY_TEST_PARENT_PID"");

    if (!string.IsNullOrEmpty(str) && int.TryParse(str, out var val))
    {
        var parent = Process.GetProcessById(val);

        parent.EnableRaisingEvents = true;
        parent.Exited += (s, o) => Process.GetCurrentProcess().Kill();
    }
}
");

            Process.Start("dotnet", $"publish \"{dir}\"").WaitForExit();

            if (!File.Exists(exe))
                throw new NotImplementedException();

            return exe;
        }

        private static string GetSourceDir(string name)
        {
            var dir = Path.GetDirectoryName(typeof(ProjectBuilder).Assembly.Location);

            return Path.Combine(dir, name);
        }
    }
}
