using System.Collections.Generic;
using PESpy.View;

namespace PESpy.Tests
{
    internal class PEFindKindViewWriter : PEViewWriter
    {
        public ViewKind Kind;

        public List<object> Matches { get; } = new List<object>();

        public static unsafe PEFindKindViewWriter New(PEFile peFile, ViewKind kind)
        {
            return new PEFindKindViewWriter(peFile, kind);
        }

        private unsafe PEFindKindViewWriter(PEFile peFile, ViewKind kind) : base(peFile)
        {
            Kind = kind;
        }

        protected internal override IView NewStruct<T>(in T value, ViewKind kind, int structSize)
        {
            if (kind == Kind)
                Matches.Add(value);

            return base.NewStruct(value, kind, structSize);
        }

        protected internal override IView NewValue<T>(int offset, in T value, int size, ViewKind kind, bool fromRegion)
        {
            if (kind == Kind)
                Matches.Add(value);

            return null;
        }
    }
}
