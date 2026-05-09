using System.Diagnostics;
using System.Linq;

namespace PESpy
{
    [DebuggerDisplay("{DebuggerDisplay(),nq}")]
    public struct INTERPRETER_OPT_FLAGS
    {
        private string DebuggerDisplay()
        {
            var items = new[]
            {
                (nameof(ServerMustSize), ServerMustSize),
                (nameof(ClientMustSize), ClientMustSize),
                (nameof(HasReturn), HasReturn),
                (nameof(HasPipes), HasPipes),
                (nameof(HasAsyncUuid), HasAsyncUuid),
                (nameof(HasExtensions), HasExtensions),
                (nameof(HasAsyncHandle), HasAsyncHandle)
            };

            var str = string.Join(" | ", items.Where(v => v.Item2).Select(v => v.Item1));

            if (str.Length == 0)
                return "<None>";

            return str;
        }

        public bool ServerMustSize => (data & 0x01) != 0;
        public bool ClientMustSize => (data & 0x02) != 0;
        public bool HasReturn => (data & 0x04) != 0;
        public bool HasPipes => (data & 0x08) != 0;
        public bool Unused => (data & 0x10) != 0;
        public bool HasAsyncUuid => (data & 0x20) != 0;
        public bool HasExtensions => (data & 0x40) != 0;
        public bool HasAsyncHandle => (data & 0x80) != 0;

        private byte data;

        public static implicit operator INTERPRETER_OPT_FLAGS(byte value) => new INTERPRETER_OPT_FLAGS { data = value };
    }
}
