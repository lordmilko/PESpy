using System.Diagnostics;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

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
#if PEFAST
            public short Length => chunk.PeekInt16(0);
#else
            public short Length { get; init; }
#endif

            /// <summary>
            /// The size, in words, of the Value member.
            /// </summary>
#if PEFAST
            public short ValueLength => chunk.PeekInt16(2);
#else
            public short ValueLength { get; init; }
#endif

            /// <summary>
            /// The type of data in the version resource. This member is 1 if the version resource contains text data and 0 if the version resource contains binary data.
            /// </summary>
#if PEFAST
            public short Type => chunk.PeekInt16(4);
#else
            public short Type { get; init; }
#endif

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
#if PEFAST
            public Utf16String Key => chunk.PeekUtf16NullTerminatedString(FixedStructSize);
#else
            public string Key { get; init; }
#endif

            /// <summary>
            /// As many zero words as necessary to align the Value member on a 32-bit boundary.
            /// </summary>
#if PEFAST
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
#else
            public short Padding { get; init; }
#endif

            /// <summary>
            /// A zero-terminated string. See the szKey member description for more information.
            /// </summary>
#if PEFAST
            public Utf16String Value => chunk.PeekUtf16NullTerminatedString((FixedStructSize + ((Key.Length + 1) * 2) + 3) & ~3);
#else
            public string Value { get; init; }
#endif

            internal const int FixedStructSize =
            sizeof(short) + //Length
            sizeof(short) + //ValueLength
            sizeof(short);  //Type

#if PEFAST
            public RawOffset Offset => chunk.AbsoluteOffset;
#else
            public RawOffset Offset { get; }
#endif

#if PEFAST
            private readonly MemoryChunk chunk;

            internal String(in MemoryChunk chunk)
            {
                this.chunk = chunk;
            }
#else
            internal String(IFileReader reader)
            {
                Offset = (RawOffset) reader.Position;

                Length = reader.ReadInt16();

                Debug.Assert(Length != 0);
                var end = Offset + Length;

                ValueLength = reader.ReadInt16();
                Type = reader.ReadInt16();
                Key = reader.ReadUTF16NullTerminatedString();

                Padding = Align32(reader, out var didAlign, end);

                if (ValueLength > 0)
                {
                    Value = reader.ReadUTF16NullTerminatedString();

                    //You can have a null terminator in the middle of the value. So we'll say that if there's still at least 4 bytes remaining,
                    //there's more to the string remaining
                    while (reader.Position < end - 4)
                        Value += reader.ReadUTF16NullTerminatedString();
                }
                else
                    Value = default;

                if (reader.Position < end)
                    Align32(reader, out var didAlign2, end);

                Debug.Assert(reader.Position == end);
            }
#endif

            void IViewable.WriteGlobals(ViewWriter writer)
            {
                //No globals
            }

            IView? IViewable.WriteStruct(ViewWriter writer) =>
                writer.NewStruct(nameof(String), this, ViewKind.StringTable_String, Length);

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

                s.VerifyLength(Length);

                return s.ToArray();
            }
        }
    }
}
