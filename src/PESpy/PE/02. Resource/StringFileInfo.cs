using System.Collections.Generic;
using System.Diagnostics;
using PESpy.View;

namespace PESpy
{
    public partial class VsVersionInfo
    {
        public class StringFileInfo : IValue, IViewable //This is a class so that it can be null without needing to use Nullable<T>
        {
            /// <summary>
            /// The length, in bytes, of the entire StringFileInfo block, including all structures indicated by the Children member.
            /// </summary>
            public short Length => chunk.PeekInt16(0);

            /// <summary>
            /// This member is always equal to zero.
            /// </summary>
            public short ValueLength => chunk.PeekInt16(2);

            /// <summary>
            /// The type of data in the version resource. This member is 1 if the version resource contains text data and 0
            /// if the version resource contains binary data.
            /// </summary>
            public short Type => chunk.PeekInt16(4);

            /// <summary>
            /// The Unicode string L"StringFileInfo".
            /// </summary>
            public FixedUtf16String Key => chunk.PeekUtf16FixedLength(6, 14);

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

            public int Offset => chunk.AbsoluteOffset;

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
                writer.NewStruct(Strings.StringFileInfo, this, ViewKind.StringFileInfo, Length);

            IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
            {
                using var s = viewWriter.CreateStruct(parent);

                s.WriteField("wLength", Length);
                s.WriteField("wValueLength", ValueLength);
                s.WriteField("wType", Type);
                s.WriteUTF16Field("szKey", Key, 15);

                if (Children != null)
                {
                    for (var i = 0; i < Children.Length; i++)
                    {
                        var item = Children[i];
                        s.WriteInline(item);

                        if (i < Children.Length - 1)
                            s.Align(4);
                    }
                }

                s.VerifyLength(Length);

                return s.ToArray();
            }
        }
    }
}
