using System.Diagnostics;
using System.Linq;

namespace PESpy
{
    [DebuggerDisplay("{DebuggerDisplay(),nq}")]
    public struct INTERPRETER_OPT_FLAGS2
    {
        private string DebuggerDisplay()
        {
            var items = new[]
            {
                (nameof(HasNewCorrDesc), HasNewCorrDesc),
                (nameof(ClientCorrCheck), ClientCorrCheck),
                (nameof(ServerCorrCheck), ServerCorrCheck),
                (nameof(HasNotify), HasNotify),
                (nameof(HasNotify2), HasNotify2),
                (nameof(HasComplexReturn), HasComplexReturn)
            };

            var str = string.Join(" | ", items.Where(v => v.Item2).Select(v => v.Item1));

            if (str.Length == 0)
                return "<None>";

            return str;
        }

        public bool HasNewCorrDesc => (data & 0x01) != 0;
        public bool ClientCorrCheck => (data & 0x02) != 0;
        public bool ServerCorrCheck => (data & 0x04) != 0;
        public bool HasNotify => (data & 0x08) != 0;
        public bool HasNotify2 => (data & 0x10) != 0;
        public bool HasComplexReturn => (data & 0x20) != 0;
        public byte Unused => (byte) ((data & 0xC0) >> 6);

        private byte data;

        public static implicit operator INTERPRETER_OPT_FLAGS2(byte value) => new INTERPRETER_OPT_FLAGS2 { data = value };
    }
}
