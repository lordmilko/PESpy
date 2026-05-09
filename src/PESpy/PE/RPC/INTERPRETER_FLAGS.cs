using System.Diagnostics;
using System.Linq;

namespace PESpy
{
    [DebuggerDisplay("{DebuggerDisplay(),nq}")]
    public struct INTERPRETER_FLAGS
    {
        private string DebuggerDisplay()
        {
            var items = new[]
            {
                (nameof(FullPtrUsed), FullPtrUsed),
                (nameof(RpcSsAllocUsed), RpcSsAllocUsed),
                (nameof(ObjectProc), ObjectProc),
                (nameof(HasRpcFlags), HasRpcFlags),
                (nameof(IgnoreObjectException), IgnoreObjectException),
                (nameof(HasCommOrFault), HasCommOrFault),
                (nameof(UseNewInitRoutines), UseNewInitRoutines)
            };

            var str = string.Join(" | ", items.Where(v => v.Item2).Select(v => v.Item1));

            if (str.Length == 0)
                return "<None>";

            return str;
        }

        public bool FullPtrUsed => (data & 0x01) != 0;
        public bool RpcSsAllocUsed => (data & 0x02) != 0;
        public bool ObjectProc => (data & 0x04) != 0;
        public bool HasRpcFlags => (data & 0x08) != 0;
        public bool IgnoreObjectException => (data & 0x10) != 0;
        public bool HasCommOrFault => (data & 0x20) != 0;
        public bool UseNewInitRoutines => (data & 0x40) != 0;
        public bool Unused => (data & 0x80) != 0;

        private byte data;

        public static implicit operator INTERPRETER_FLAGS(byte value) => new INTERPRETER_FLAGS { data = value };
    }
}
