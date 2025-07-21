using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using PESpy.Native;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy
{
    /// <summary>
    /// Represents the <see cref="VS_VERSIONINFO"/> structure.
    /// </summary>
    public partial class VsVersionInfo : IValue, IViewable //This is a class so that it can be null without needing to use Nullable<T>
    {
#if PEFAST
        public short Length => chunk.PeekInt16(0);

        public short ValueLength => chunk.PeekInt16(2);

        public short Type => chunk.PeekInt16(4);

        public FixedUtf16String Key => chunk.PeekUtf16FixedLength(6, 15); //VS_VERSION_INFO + \0 (16 in total)

        //Due to the fact we've read 3 shorts and then 16 bits, we should always align here
        public short Padding1 => chunk.PeekInt16(38); //6 + (16 * 2)

        private VsFixedFileInfo? value;

        public VsFixedFileInfo? Value
        {
            get
            {
                if (value == null && ValueLength > 0)
                    value = new VsFixedFileInfo(chunk.Slice(FixedStructSize));

                return value;
            }
        }

#if !PEFAST
        public short Padding2 { get; init; }
#endif
        private IValue[]? children;


        public IValue[]? Children
        {
            get
            {
                if (children == null)
                {
                    //In the event we read VS_FIXEDFILEINFO, it has an even number of shorts, so we should never need to align here
                    var read = FixedStructSize + ValueLength;

                    var length = Length;

                    if (read < length)
                    {
                        using var results = new PooledList<IValue>();

                        do
                        {
                            //We now have a sequence of StringFileInfo and/or VarFileInfo items. The header format of these types
                            //is identical, they just have different keys

                            var szKey = chunk.PeekUtf16NullTerminatedString(read + 6); //Skip over the InfoLength, InfoValueLength and Type

                            if (szKey == "StringFileInfo")
                            {
                                var item = new StringFileInfo(chunk.Slice(read));
                                read += item.Length;
                                results.Add(item);
                            }
                            else if (szKey == "VarFileInfo")
                            {
                                var item = new VarFileInfo(chunk.Slice(read));
                                read += item.Length;
                                results.Add(item);
                            }
                            else
                            {
                                Debug.Assert(false);
                            }
                        } while (read < length);                        

                        children = results.ToArray();
                    }
                }

                return children;
            }
        }

        public int Offset => chunk.AbsoluteOffset;
#else
        public short Length { get; init; }

        public short ValueLength { get; init; }

        public short Type { get; init; }

        public string Key { get; init; }

        public short Padding1 { get; init; }

        public VsFixedFileInfo Value { get; init; }

        public short Padding2 { get; init; }

        public IValue[] Children;

        public RawOffset Offset { get; }
#endif

        internal const int FixedStructSize =
            sizeof(short) + //Length
            sizeof(short) + //ValueLength
            sizeof(short) + //Type
            32 +            //Key
            sizeof(short);  //Padding1

#if PEFAST
        private readonly MemoryChunk chunk;

        internal VsVersionInfo(in MemoryChunk chunk)
        {
            this.chunk = chunk;

#if STRESS_TEST
            _ = Value;
            _ = Children;
#endif
        }
#else
        internal VsVersionInfo(IFileReader reader)
        {
            Offset = (RawOffset) reader.Position;

            Length = reader.ReadInt16();

            Debug.Assert(Length != 0);
            var end = Offset + Length;

            reader.FillBuffer(Length - 2);
            
            ValueLength = reader.ReadInt16();

            Type = reader.ReadInt16();
            Key = reader.ReadUnicodeString(16); //VS_VERSION_INFO + \0

            //Due to the fact we've read 3 shorts and then 16 bits, we should always align here
            Padding1 = Align32(reader, out var didAlign, end);
            Debug.Assert(didAlign);

            if (ValueLength > 0)
            {
                Debug.Assert(ValueLength == VsFixedFileInfo.StructSize);
                Value = new VsFixedFileInfo(reader);
            }

            if (reader.Position < end)
            {
                //In the event we read VS_FIXEDFILEINFO, it has an even number of shorts, so we should never need to align here
                Padding2 = Align32(reader, out didAlign, end);
                Debug.Assert(!didAlign);

                //Read StringFileInfo/VarFileInfo structures. The start of these structures are identical

                using var children = new PooledList<IValue>();

                while (reader.Position < end)
                {
                    var offset = (int) reader.Position;

                    var infoLength = reader.ReadInt16();
                    var infoValueLength = reader.ReadInt16();
                    var type = reader.ReadInt16();
                    var szKey = reader.ReadUTF16NullTerminatedString();

                    switch (szKey)
                    {
                        case "StringFileInfo":
                            children.Add(new StringFileInfo(offset, infoLength, infoValueLength, type, szKey, reader));
                            break;

                        case "VarFileInfo":
                            children.Add(new VarFileInfo(offset, infoLength, infoValueLength, type, szKey, reader));
                            break;

                        default:
                            throw new NotImplementedException(); //todo: throw in debug only
                    }

                    if (reader.Position < end)
                    {
                        //Not sure if we have to align again here, but to be safe I think the answer is yes
                        Align32(reader, out didAlign, end);
                    }
                }

                Debug.Assert(reader.Position == end);

                Children = children.ToArray();
            }
        }
#endif

#if !PEFAST
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static short Align32(IFileReader reader, out bool didAlign, int end)
        {
            var alignedPosition = (reader.Position + 3) & ~3;

            if (alignedPosition == reader.Position)
            {
                didAlign = false;
                return 0;
            }

            //You can have dodgy end values that are not 32-bit aligned. If aligning will push us past our limit,
            //just clamp to the limit
            if (alignedPosition > end)
            {
                var diff = end - reader.Position;

                Debug.Assert(diff == 1);
                didAlign = true;
                return reader.ReadByte();
            }
            else
            {
                var diff = alignedPosition - reader.Position;

                //If the previous alignment attempt aligned 1 byte because the end was not aligned, we're now
                //going to be unaligned here too

                if (diff == 1)
                {
                    didAlign = true;
                    return reader.ReadByte();
                }
                else
                {
                    Debug.Assert(diff == 2);
                    didAlign = true;
                    return reader.ReadInt16();
                }
            }
        }
#endif

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(nameof(VS_VERSIONINFO), this, ViewKind.VsVersionInfo, Length);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField("wLength", Length);
            s.WriteField("wValueLength", ValueLength);
            s.WriteField("wType", Type);
            s.WriteUTF16Field("szKey", Key, 16);
            s.WriteField(nameof(Padding1), Padding1);

            var value = Value;

            if (value != null)
                s.WriteInline(value);

#if !PEFAST
            if (s.NeedAlignment(4, out var required))
            {
                Debug.Assert(required == 2);
                s.WriteField(nameof(Padding2), Padding2);
            }
#endif
            var children = Children;

            if (children != null)
            {
                for (var i = 0; i < children.Length; i++)
                {
                    var child = children[i];
                    s.WriteInline((IViewable) child);

                    if (i < children.Length - 1)
                        s.Align(4);
                }
            }

            s.VerifyLength(Length);

            return s.ToArray();
        }
    }
}
