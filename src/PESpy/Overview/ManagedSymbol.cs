using ClrDebug;
using PESpy.Ecma335;

namespace PESpy
{
    public static partial class FileOverview
    {
        public class ManagedSymbol
        {
            public mdToken Token { get; }

            //The file may be closed immediately after getting the overview, so we can't hold onto the MethodDefRow
            public string? Name { get; }

            internal ManagedSymbol(mdToken token, string? name)
            {
                Token = token;
                Name = name;
            }

            public override string ToString()
            {
                if (Name != null)
                    return $"{Name} ({Token})";

                return $"<Invalid> ({Token})";
            }
        }
    }
}
