using System.Collections.Generic;
using PESpy.View;

namespace PESpy.Tests
{
    internal class FindKindViewWriter : ViewWriter
    {
        public ViewKind Kind;

        public List<object> Matches { get; } = new List<object>();

        public static unsafe FindKindViewWriter New(PEFile peFile, ViewKind kind)
        {
            return new FindKindViewWriter(peFile, kind);
        }

        private unsafe FindKindViewWriter(PEFile peFile, ViewKind kind) : base(peFile)
        {
            Kind = kind;
        }

        protected internal override IView NewStruct<T>(in T value, ViewKind kind, int structSize)
        {
            if (kind == Kind)
                Matches.Add(value);

            return base.NewStruct(value, kind, structSize);
        }

        protected internal override IView NewValue<T>(long offset, in T value, int size, ViewKind kind, bool fromRegion)
        {
            if (kind == Kind)
                Matches.Add(value);

            return null;
        }
    }
}
