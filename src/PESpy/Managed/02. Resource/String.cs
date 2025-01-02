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
            public short Length { get; init; }

            /// <summary>
            /// The size, in words, of the Value member.
            /// </summary>
            public short ValueLength { get; init; }

            /// <summary>
            /// The type of data in the version resource. This member is 1 if the version resource contains text data and 0 if the version resource contains binary data.
            /// </summary>
            public short Type { get; init; }

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
            public string Key { get; init; }

            /// <summary>
            /// As many zero words as necessary to align the Value member on a 32-bit boundary.
            /// </summary>
            public short Padding { get; init; }

            /// <summary>
            /// A zero-terminated string. See the szKey member description for more information.
            /// </summary>
            public string Value { get; init; }

            public RawOffset Offset { get; }

            internal String(IFileReader reader)
            {
                Offset = (RawOffset) reader.Position;

                Length = reader.ReadInt16();

                Debug.Assert(Length != 0);
                var end = Offset + Length;

                ValueLength = reader.ReadInt16();
                Type = reader.ReadInt16();
                Key = reader.ReadUTF16NullTerminatedString();

                Padding = Align32(reader, out var didAlign);

                Value = reader.ReadUTF16NullTerminatedString();

                Debug.Assert(reader.Position == end);
            }

            void IViewable.WriteView(ViewWriter writer)
            {
                using var s = writer.CreateStruct(nameof(String), this, ViewKind.StringTable_String);

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
            }
        }
    }
}
