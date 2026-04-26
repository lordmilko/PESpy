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
        public readonly struct String : IViewableValue
        {
        private const int LengthOffset = 0;
        private const int ValueLengthOffset = 2;
        private const int TypeOffset = 4;

            /// <summary>
            /// The length, in bytes, of this String structure.
            /// </summary>
            public short Length => chunk.PeekInt16(LengthOffset);

            /// <summary>
            /// The size, in words, of the Value member.
            /// </summary>
            public short ValueLength => chunk.PeekInt16(ValueLengthOffset);

            /// <summary>
            /// The type of data in the version resource. This member is 1 if the version resource contains text data and 0 if the version resource contains binary data.
            /// </summary>
            public short Type => chunk.PeekInt16(TypeOffset);

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

            /// <summary>
            /// A zero-terminated string. See the szKey member description for more information.
            /// </summary>
            public Utf16String Value
            {
                get
                {
                    /* The Value can be a bit of a mess
                     * - If the ValueLength is 0, there isn't a Value
                     * - When there is a value, its actual length can be less than the length listed! Which means you
                     *   should perhaps be looking for a null terminator instead of looking at the ValueLength
                     * - Except I apparently saw embedded null terminators once! So what am I supposed to do!
                     * - And then to make matters worse, you can have a bogus value where there's a null terminated
                     *   string that extends past the end of the struct
                     *
                     * Well, in the case where the ValueLength lied to us, the Length did not, so I _was_ supposed
                     * to just read up to the null terminator
                     */
                    if (ValueLength == 0)
                        return default;

                    return chunk.PeekUtf16NullTerminatedString(ValueStart);
                }
            }

            //Sometimes the ValueLength does not account for all of the remaining bytes in the String. I've seen cases, for instance, where the rest of the bytes
            //were padded with X's
            public FixedUtf16String Extra
            {
                get
                {
                    if (ValueLength == 0)
                        return default;

                    //ValueLength is meaningless; Value.Length might be more
                    var start = ValueStart + ((Value.Length + 1) * 2);

                    var remaining = Length - start;

                    if (remaining > 0)
                    {
                        var extra = chunk.PeekUtf16FixedLength(start, (remaining / 2) - 1);

                        return extra;
                    }

                    return default;
                }
            }

            private int ValueStart => (FixedStructSize + ((Key.Length + 1) * 2) + 3) & ~3;

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
                writer.NewStruct(this, ViewKind.StringTable_String, Length);

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

                if (ValueLength > 0)
                {
                    if (ValueStart + (ValueLength * sizeof(short)) > Length)
                    {
                        //The value is bogus, and with the null terminator extends past the end of the string
                        var realValueLength = Length - ValueStart; //Length in bytes
                        s.WriteByteBlob(realValueLength);
                    }
                    else
                        s.WriteUtf16NullTerminatedField(nameof(Value), Value);

                    var extra = Extra;

                    if (extra.Length > 0)
                    {
                        s.WriteInlineUtf16NullTerminated(extra, (extra.Length + 1) * 2);
                    }
                }

                if (s.Size < Length)
                    s.AlignMax(4, Length);

                s.VerifyLength(Length);

                structWriter.EagerFields = s.ToArray();
            }
        }
    }
}
