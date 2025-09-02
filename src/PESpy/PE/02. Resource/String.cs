using System.Diagnostics;
using PESpy.View;

namespace PESpy
{
    public partial class VsVersionInfo
    {
        /// <summary>
        /// Represents the organization of data in a file-version resource. It contains a string that describes a specific aspect of a file, for example, a file's version, its copyright notices, or its trademarks.
        /// </summary>
        [DebuggerDisplay("{Key,nq} = {Value}")]
        public readonly struct String : IValue, IViewable
        {
            /// <summary>
            /// The length, in bytes, of this String structure.
            /// </summary>
            public short Length => chunk.PeekInt16(0);

            /// <summary>
            /// The size, in words, of the Value member.
            /// </summary>
            public short ValueLength => chunk.PeekInt16(2);

            /// <summary>
            /// The type of data in the version resource. This member is 1 if the version resource contains text data and 0 if the version resource contains binary data.
            /// </summary>
            public short Type => chunk.PeekInt16(4);

            /// <summary>
            /// An arbitrary Unicode string. The szKey member can be one or more of the following values. These values are guidelines only.
            /// - Comments
            /// - CompanyName
            /// - FileDescription
            /// - FileVersion
            /// - InternalName
            /// - LegalCopyright
            /// - LegalTrademarks
            /// - OriginalFilename
            /// - PrivateBuild
            /// - ProductName
            /// - ProductVersion
            /// - SpecialBuild
            /// </summary>
            public Utf16String Key => chunk.PeekUtf16NullTerminatedString(FixedStructSize);

            /// <summary>
            /// As many zero words as necessary to align the Value member on a 32-bit boundary.
            /// </summary>
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

            //todo: but you can have null terminators inside the value? so should we make it a fixed length string instead?

            /// <summary>
            /// A zero-terminated string. See the szKey member description for more information.
            /// </summary>
            public Utf16String Value => chunk.PeekUtf16NullTerminatedString((FixedStructSize + ((Key.Length + 1) * 2) + 3) & ~3);

            internal const int FixedStructSize =
            sizeof(short) + //Length
            sizeof(short) + //ValueLength
            sizeof(short);  //Type

            public int Offset => chunk.AbsoluteOffset;

            private readonly MemoryChunk chunk;

            internal String(in MemoryChunk chunk)
            {
                this.chunk = chunk;
            }

            void IViewable.WriteGlobals(ViewWriter writer)
            {
                //No globals
            }

            IView? IViewable.WriteStruct(ViewWriter writer) =>
                writer.NewStruct(Strings.String, this, ViewKind.StringTable_String, Length);

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

                s.WriteUTF16NullTerminatedField(nameof(Value), Value);

                if (s.Size < Length)
                    s.Align(4);

                s.VerifyLength(Length);

                return s.ToArray();
            }
        }
    }
}
