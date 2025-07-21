using ClrDebug.DIA;

#nullable disable

namespace PESpy
{
    public static partial class Demangler
    {
        public class ThunkSignatureNode : FunctionSignatureNode
        {
            private ThunkThisAdjustor thisAdjustor;

            public ref ThunkThisAdjustor ThisAdjustor => ref thisAdjustor;

            public ThunkSignatureNode() : base(NodeKind.ThunkSignature)
            {
            }

            public override void OutputPre(ref Utf8StringBuilder builder, UNDNAME flags)
            {
                builder.Append("[thunk]:");

                base.OutputPre(ref builder, flags);
            }

            public override void OutputPost(ref Utf8StringBuilder builder, UNDNAME flags)
            {
                var functionClass = FunctionClass;

                if ((functionClass & FunctionClass.Adjustor) != 0)
                {
                    builder.Append("`adjustor{");
                    builder.Append(ThisAdjustor.StaticOffset);
                    builder.Append("}' ");
                }
                else if ((functionClass & FunctionClass.VirtualThisAdjust) != 0)
                {
                    if ((functionClass & FunctionClass.VirtualThisAdjustEx) != 0)
                    {
                        builder.Append("`vtordispex{");
                        builder.Append(ThisAdjustor.VBPtrOffset);
                        builder.Append(", ");
                        builder.Append(ThisAdjustor.VBOffsetOffset);
                        builder.Append(", ");
                        builder.Append(ThisAdjustor.VtordispOffset);
                        builder.Append(", ");
                        builder.Append(ThisAdjustor.StaticOffset);
                        builder.Append("}' "); ;
                    }
                    else
                    {
                        builder.Append("`vtordisp{");
                        builder.Append(ThisAdjustor.VtordispOffset);
                        builder.Append(", ");
                        builder.Append(ThisAdjustor.StaticOffset);
                        builder.Append("}' "); ;
                    }
                }

                base.OutputPost(ref builder, flags);
            }

            public override void Reset()
            {
                base.Reset();

                thisAdjustor = default;
            }
        }

        public struct ThunkThisAdjustor
        {
            public long StaticOffset;
            public long VBPtrOffset;
            public long VBOffsetOffset;
            public long VtordispOffset;
        }
    }
}
