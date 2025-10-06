using System.Diagnostics;
using System.Text;

namespace PESpy.PDB
{
    //Type is made up
    [DebuggerDisplay("{DebuggerDisplay(),nq}")]
    public readonly struct ThunkEntry
    {
        private string DebuggerDisplay()
        {
            var builder = new StringBuilder();

            builder.Append(Thunk);

            builder.Append(" -> ");

            builder.Append(Target.SymType);

            if (Target.Displacement != 0)
                builder.Append("+0x").AppendFormat("{0:X}", Target.Displacement);

            return builder.ToString();
        }

        public SymType Thunk { get; }

        public (SymType SymType, int Displacement) Target { get; }

        internal ThunkEntry(
            SymType thunk,
            (SymType SymType, int Displacement) target)
        {
            Thunk = thunk;
            Target = target;
        }
    }
}
