using System.Diagnostics;
using ClrDebug;

namespace PESpy.Mstat
{
    [DebuggerDisplay("{DebuggerDisplay(),nq}")]
    public struct MstatFrozenObjectRow
    {
        private string DebuggerDisplay()
        {
            using var builder = new ValueStringBuilder();

            builder.Append(ToString());
            builder.Append(" (");
            builder.AppendSize(Size);
            builder.Append(')');

            return builder.ToString();
        }

        public int Size { get; }

        public FixedUtf8String Name { get; }

        public object InstanceType => GetType(_instanceTypeToken);

        public object OwningType => GetType(_owningTypeToken);

        internal int NextFrozenObject;

        private readonly MstatHeap _mstatHeap;
        private readonly mdToken _instanceTypeToken;
        private readonly mdToken _owningTypeToken;

        internal MstatFrozenObjectRow(in MstatInfo.FrozenObject frozenObject, MstatHeap mstatHeap)
        {
            _mstatHeap = mstatHeap;

            Name = frozenObject.MangledName;
            Size = frozenObject.Size;
            _mstatHeap = mstatHeap;
            _instanceTypeToken = frozenObject.InstanceTypeToken;
            _owningTypeToken = frozenObject.OwningTypeToken;
        }

        private object GetType(mdToken token)
        {
            if (token.IsNil)
                return null;

            return token.Type switch
            {
                CorTokenType.mdtTypeRef => _mstatHeap.TypeDefTable[token.Rid - 1],
                CorTokenType.mdtTypeSpec => _mstatHeap.TypeSpecTable[token.Rid - 1],
            };
        }

        public override string ToString()
        {
            return InstanceType.ToString();
        }
    }
}
