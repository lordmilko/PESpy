using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("PESpy.Benchmarks")]
[assembly: InternalsVisibleTo("PESpy.PowerShell")]
[assembly: InternalsVisibleTo("PESpy.Tests")]
[assembly: InternalsVisibleTo("PESpyUI")]
[assembly: InternalsVisibleTo("ReAnalyze")]
[assembly: InternalsVisibleTo("ReFlow")]
[assembly: InternalsVisibleTo("ReDbg")]
[assembly: InternalsVisibleTo("ReDbg.Engine")]
[assembly: InternalsVisibleTo("SymHelp")]

//init only properties require this type be defined, which is not present in .NET Standard / .NET Framework
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit
    {
    }
}
