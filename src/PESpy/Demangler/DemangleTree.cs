using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace PESpy
{
    public static partial class Demangler
    {
        public ref struct DemangleTree
        {
            public readonly SymbolNode Root;
            private Span<FixedUtf8String> pointers;
            private DemanglerNodeArena? arena;

            internal DemangleTree(SymbolNode root, Span<FixedUtf8String> pointers, DemanglerNodeArena? arena)
            {
                Root = root;
                this.pointers = pointers;
                this.arena = arena;
            }

            public unsafe void Dispose()
            {
                var p = pointers;

                for (var i = 0; i < p.Length; i++)
                    Marshal.FreeHGlobal((IntPtr) p[i].Value);

                var a = arena;

                if (a != null)
                {
                    a.Reset();
                    Interlocked.CompareExchange(ref TextWindow.cachedArena, a, null);
                }
            }

            public override string ToString() => Root?.ToString() ?? "<empty>";
        }
    }
}
