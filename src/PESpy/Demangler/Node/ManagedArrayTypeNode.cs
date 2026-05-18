using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ClrDebug.DIA;
using static PESpy.Demangler;

namespace PESpy
{
    internal class ManagedArrayTypeNode : TypeNode
    {
        public PointerAffinity Affinity { get; internal set; }

        public TypeNode ElementType { get; internal set; }

        public int Rank { get; internal set; }

        public ManagedArrayTypeNode() : base(NodeKind.ManagedArray)
        {
        }

        public override void OutputPre(ref Utf8StringBuilder builder, UNDNAME flags)
        {
            builder.Append("cli::array<");
            ElementType.OutputPre(ref builder, flags);

            Debug.Assert(Rank == 1); //If the rank is greater than 1, we may need to include some extra commas

            //Not sure whether we're supposed to apply any qualifiers
        }

        public override void OutputPost(ref Utf8StringBuilder builder, UNDNAME flags)
        {
            builder.Append(" >");
        }

        public override void Reset()
        {
            base.Reset();

            Affinity = default;
            ElementType = default;
            Rank = default;
        }
    }
}
