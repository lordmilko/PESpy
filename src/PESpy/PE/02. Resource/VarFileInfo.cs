using System.Diagnostics;
using PESpy.View;

namespace PESpy
{
    public partial class VsVersionInfo
    {
        public struct VarFileInfo : IValue, IViewable //This is a class so that it can be null without needing to use Nullable<T>
        {
            public short Length => chunk.PeekInt16(0);

            public short ValueLength => chunk.PeekInt16(2);

            public short Type => chunk.PeekInt16(4);

            public Utf16String Key => chunk.PeekUtf16NullTerminatedString(FixedStructSize);

            public short Padding
            {
                get
                {
                    var currentLength = FixedStructSize + ((Key.Length + 1) * 2);

                    var alignedLength = (currentLength + 3) & ~3;

                    if (alignedLength == 0)
                        return 0;

                    return chunk.PeekInt16(currentLength);
                }
            }

            private Var[]? children;

            public Var[]? Children
            {
                get
                {
                    if (children == null)
                    {
                        var read = FixedStructSize + ((Key.Length + 1) * 2);

                        var alignedRead = (read + 3) & ~3;

                        var length = Length;

                        if (alignedRead < length)
                        {
                            using var results = new PooledList<Var>();

                            do
                            {
                                var item = new Var(chunk.Slice(alignedRead));
                                results.Add(item);
                                Debug.Assert(item.Length != 0);

                                //On the basis that each String must be 32-bit aligned, I'm going to assume that each Var must be 32-bit aligned too
                                alignedRead += (item.Length + 3) & ~3;
                            } while (alignedRead < length);

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
                sizeof(short);  //Type

            private readonly MemoryChunk chunk;

            internal VarFileInfo(in MemoryChunk chunk)
            {
                this.chunk = chunk;
                children = default;
            }

            void IViewable.WriteGlobals(ViewWriter writer)
            {
                //No globals
            }

            IView? IViewable.WriteStruct(ViewWriter writer) =>
                writer.NewStruct(Strings.VarFileInfo, this, ViewKind.VarFileInfo, Length);

            IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
            {
                using var s = viewWriter.CreateStruct(parent);

                s.WriteField("wLength", Length);
                s.WriteField("wValueLength", ValueLength);
                s.WriteField("wType", Type);
                s.WriteUTF16NullTerminatedField("szKey", Key);

                if (s.NeedAlignment(4, out var required))
                {
                    Debug.Assert(required == 2);
                    s.WriteField(nameof(Padding), Padding);
                }

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

                return s.ToArray();
            }
        }
    }
}
