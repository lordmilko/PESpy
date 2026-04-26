using System.Diagnostics;
using PESpy.View;

namespace PESpy
{
    public partial class VsVersionInfo
    {
        public readonly struct Var : IViewableValue
        {
        private const int LengthOffset = 0;
        private const int ValueLengthOffset = 2;
        private const int TypeOffset = 4;

            public short Length => chunk.PeekInt16(LengthOffset);

            public short ValueLength => chunk.PeekInt16(ValueLengthOffset);

            public short Type => chunk.PeekInt16(TypeOffset);

            public Utf16String Key => chunk.PeekUtf16NullTerminatedString(FixedStructSize);

            public short Padding
            {
                get
                {
                    //Whether we need to align or not will depend on whether Key has an odd number of characters or not.
                    //If it's odd, including the \0 it's even, but we read 3 shorts so we're down a word
                    var currentLength = FixedStructSize + ((Key.Length + 1) * 2);

                    var alignedLength = (currentLength + 3) & ~3;

                    if (alignedLength == 0)
                        return 0;

                    return chunk.PeekInt16(currentLength);
                }
            }

            public NativeSpan<int> Value
            {
                get
                {
                    var offset = (FixedStructSize + ((Key.Length + 1) * 2) + 3) & ~3;

                    var numItems = ValueLength / 4;

                    return chunk.PeekNativeSpan<int>(offset, numItems);
                }
            }

            public int Offset => chunk.AbsoluteOffset;

            internal const int FixedStructSize =
                sizeof(short) + //Length
                sizeof(short) + //ValueLength
                sizeof(short);  //Type

            private readonly MemoryChunk chunk;

            internal Var(in MemoryChunk chunk)
            {
                this.chunk = chunk;
            }

            void IViewable.WriteGlobals(ViewWriter writer)
            {
                //No globals
            }

            IView? IViewable.WriteStruct(ViewWriter writer) =>
                writer.NewStruct(this, ViewKind.VarFileInfo_Var, Length);

            int IViewable.NumChildren() => throw StructWriter.GetEagerLoadOnlyException();

            void IViewable.WriteChild(int index, ref StructWriter structWriter)
            {
                if (index != -1)
                    throw StructWriter.GetEagerLoadOnlyException();

                using var s = structWriter.CreateEagerWriter();

                s.WriteField("wLength", Length);
                s.WriteField("wValueLength", ValueLength);
                s.WriteField("wType", Type);
                s.WriteUtf16NullTerminatedField("szKey", Key);

                if (s.NeedAlignment(4, out var required))
                {
                    Debug.Assert(required == 2);
                    s.WriteField(nameof(Padding), Padding);
                }

                s.WriteField(nameof(Value), Value);

                s.VerifyLength(Length);

                structWriter.EagerFields = s.ToArray();
            }
        }
    }
}
