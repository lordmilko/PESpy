using ClrDebug.DIA;

#nullable disable

namespace PESpy
{
    public static partial class Demangler
    {
        public class RttiBaseClassDescriptorNode : IdentifierNode
        {
            public ulong NVOffset { get; internal set; }

            public long VBPtrOffset { get; internal set; }

            public ulong VBTableOffset { get; internal set; }

            public ulong Flags { get; internal set; }

            public RttiBaseClassDescriptorNode() : base(NodeKind.RttiBaseClassDescriptor)
            {
            }

            public override void Output(ref Utf8StringBuilder builder, UNDNAME flags)
            {
                builder.Append("`RTTI Base Class Descriptor at (");
                builder.Append(NVOffset);
                builder.Append(",");
                builder.Append(VBPtrOffset);
                builder.Append(",");
                builder.Append(VBTableOffset);
                builder.Append(",");
                builder.Append(Flags);
                builder.Append(")'"); ;
            }

            public override void Reset()
            {
                base.Reset();

                NVOffset = default;
                VBPtrOffset = default;
                VBTableOffset = default;
                Flags = default;
            }
        }
    }
}
