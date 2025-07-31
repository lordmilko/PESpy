using System.Diagnostics;
using ClrDebug.DIA;

#nullable disable

namespace PESpy
{
    public static partial class Demangler
    {
        public class TagTypeNode : TypeNode
        {
            public QualifiedNameNode QualifiedName { get; internal set; }

            public TagKind Tag { get; internal set; }

            public TagTypeNode() : base(NodeKind.TagType)
            {
            }

            public override void OutputPre(ref Utf8StringBuilder builder, UNDNAME flags)
            {
                Debug.Assert(Tag != TagKind.Enum, "I think enum plays by different rules when it comes to no ECSU and/or name only. Check UnDecorateSymbolName");
                if ((flags & UNDNAME.UNDNAME_NO_ECSU) == 0 && (flags & UNDNAME.UNDNAME_NAME_ONLY) == 0)
                {
                    var value = Tag switch
                    {
                        TagKind.Class => "class",
                        TagKind.Struct => "struct",
                        TagKind.Union => "union",
                        TagKind.Enum => "enum"
                    };

                    builder.Append(value);
                    builder.Append(" ");
                }

                QualifiedName.Output(ref builder, flags);

                var skipFirstSpaceBefore = false;
                OutputQualifiers(ref builder, flags, Qualifiers, true, false, ref skipFirstSpaceBefore);
            }

            public override void OutputPost(ref Utf8StringBuilder builder, UNDNAME flags)
            {
                //Empty
            }

            public override void Reset()
            {
                base.Reset();

                QualifiedName = default;
                Tag = default;
            }
        }
    }
}
