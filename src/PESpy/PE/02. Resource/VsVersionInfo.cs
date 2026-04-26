using System;
using System.Diagnostics;
using PESpy.Native;
using PESpy.View;

namespace PESpy
{
    /// <summary>
    /// Represents the <see cref="VS_VERSIONINFO"/> structure.
    /// </summary>
    public partial class VsVersionInfo : IValue, IViewable //This is a class so that it can be null without needing to use Nullable<T>
    {
        private const int LengthOffset = 0;
        private const int ValueLengthOffset = 2;
        private const int TypeOffset = 4;
        internal const int KeyOffset = 6;
        private const int Padding1Offset = 38;

        public short Length => chunk.PeekInt16(LengthOffset);

        public short ValueLength => chunk.PeekInt16(ValueLengthOffset);

        public short Type => chunk.PeekInt16(TypeOffset);

        public FixedUtf16String Key => chunk.PeekUtf16FixedLength(KeyOffset, 15); //VS_VERSION_INFO + \0 (16 in total)

        //Due to the fact we've read 3 shorts and then 16 bits, we should always align here
        public short Padding1 => chunk.PeekInt16(Padding1Offset); //6 + (16 * 2)

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
                                /* The spec for version info is https://learn.microsoft.com/en-us/windows/win32/menurc/versioninfo-resource
                                 *
                                 * It doesn't say it, but it's possible to have "custom" sections, e.g. Adobe files can have a "LanguageInfo" section
                                 * Unlike StringFileInfo and VarFileInfo, with these entries it seems like we skip right to the underlying data. So there's
                                 * no StringFileInfo, we just go straight to the StringTable. Or at least, that's how it was for Adobe
                                 */

                                var type = chunk.PeekUInt16(read + 4);

                                if (type == 1)
                                {
                                    //It's text
                                    var item = new StringTable(chunk.Slice(read));
                                    read += item.Length;
                                    results.Add(item);
                                }
                                else
                                {
                                    //It's variable. But is it VarFileInfo or Var? What do we do?
                                    throw new NotImplementedException();
                                }
                            }

                            //Attempting to align can push us over the length; that's OK
                            read = (read + 3) & ~3;
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
            32 +            //Key
            sizeof(short);  //Padding1

        private readonly MemoryChunk chunk;

        internal VsVersionInfo(in MemoryChunk chunk)
        {
            this.chunk = chunk;

#if STRESS_TEST
            _ = Value;
            _ = Children;
#endif
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.VsVersionInfo, Length);

        int IViewable.NumChildren() => throw StructWriter.GetEagerLoadOnlyException();

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            if (index != -1)
                throw StructWriter.GetEagerLoadOnlyException();

            using var s = structWriter.CreateEagerWriter();

            s.WriteField("wLength", Length);
            s.WriteField("wValueLength", ValueLength);
            s.WriteField("wType", Type);
            s.WriteUtf16FixedLengthField("szKey", Key, 16);
            s.WriteField(nameof(Padding1), Padding1);

            var value = Value;

            if (value != null)
                s.WriteInline(value);

            var children = Children;

            if (children != null)
            {
                for (var i = 0; i < children.Length; i++)
                {
                    var child = children[i];
                    s.WriteInline((IViewableValue) child);

                    if (i < children.Length - 1)
                        s.Align(4);
                }
            }

            s.VerifyLength(Length);

            structWriter.EagerFields = s.ToArray();
        }
    }
}
