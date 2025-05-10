using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("PESpy.Tests")]
[assembly: InternalsVisibleTo("App")]

//init only properties require this type be defined, which is not present in .NET Standard / .NET Framework
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit
    {
    }
}
