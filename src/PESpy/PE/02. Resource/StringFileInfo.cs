using System.Diagnostics;
using PESpy.View;

namespace PESpy
{
    public partial class VsVersionInfo
    {
        public class StringFileInfo : IViewableValue //This is a class so that it can be null without needing to use Nullable<T>
        {
        private const int LengthOffset = 0;
        private const int ValueLengthOffset = 2;
        private const int TypeOffset = 4;
        private const int KeyOffset = 6;

            /// <summary>
            /// The length, in bytes, of the entire StringFileInfo block, including all structures indicated by the Children member.
            /// </summary>
            public short Length => chunk.PeekInt16(LengthOffset);

            /// <summary>
            /// This member is always equal to zero.
            /// </summary>
            public short ValueLength => chunk.PeekInt16(ValueLengthOffset);

            /// <summary>
            /// The type of data in the version resource. This member is 1 if the version resource contains text data and 0
            /// if the version resource contains binary data.
            /// </summary>
            public short Type => chunk.PeekInt16(TypeOffset);

            /// <summary>
            /// The Unicode string L"StringFileInfo".
            /// </summary>
            public FixedUtf16String Key => chunk.PeekUtf16FixedLength(KeyOffset, 14);

            //Will never need to align, as Key is 30 bytes, so we're now on byte 36

            /// <summary>
            /// As many zero words as necessary to align the Children member on a 32-bit boundary.
            /// </summary>
            public short Padding => 0; //There is never any padding, due to the length of the key

            private StringTable[]? children;

            public StringTable[]? Children
            {
                get
                {
                    if (children == null)
                    {
                        var length = Length;

                        var read = FixedStructSize; //Includes the key already

                        if (read < length)
                        {
                            using var results = new PooledList<StringTable>();

                            do
                            {
                                var item = new StringTable(chunk.Slice(read));
                                Debug.Assert(item.Length != 0);
                                results.Add(item);
                                read += (item.Length + 3) & ~3; //Not sure if we have to align, so just do it anyway
                            } while (read < length);

                            children = results.ToArray();
                        }
                    }

                    return children;
                }
            }

            public long Offset => chunk.AbsoluteOffset;

            internal const int FixedStructSize =
                sizeof(short) + //Length
                sizeof(short) + //ValueLength
                sizeof(short) + //Type
                30;             //Key

            private readonly MemoryChunk chunk;

            internal StringFileInfo(in MemoryChunk chunk)
            {
                this.chunk = chunk;

#if STRESS_TEST
                _ = Children;
#endif
            }

            void IViewable.WriteGlobals(ViewWriter writer)
            {
                //No globals
            }

            IView? IViewable.WriteStruct(ViewWriter writer) =>
                writer.NewStruct(this, ViewKind.StringFileInfo, Length);

            int IViewable.NumChildren() => throw StructWriter.GetEagerLoadOnlyException();

            void IViewable.WriteChild(int index, ref StructWriter structWriter)
            {
                if (index != -1)
                    throw StructWriter.GetEagerLoadOnlyException();

                using var s = structWriter.CreateEagerWriter();

                s.WriteField("wLength", Length);
                s.WriteField("wValueLength", ValueLength);
                s.WriteField("wType", Type);
                s.WriteUtf16FixedLengthField("szKey", Key, 15);

                if (Children != null)
                {
                    for (var i = 0; i < Children.Length; i++)
                    {
                        var item = Children[i];
                        s.WriteInline(item);

                        if (i < Children.Length - 1)
                            s.Align(4);
                    }

                    if (s.Size < Length)
                        s.AlignMax(4, Length);
                }

                s.VerifyLength(Length);

                structWriter.EagerFields = s.ToArray();
            }
        }
    }
}
