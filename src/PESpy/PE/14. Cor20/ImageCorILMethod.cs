using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ClrDebug;
using PESpy.View;

namespace PESpy
{
    /// <summary>
    /// Represents the <see cref="IMAGE_COR_ILMETHOD"/> union which encapsulates the <see cref="IMAGE_COR_ILMETHOD_TINY"/> and <see cref="IMAGE_COR_ILMETHOD_FAT"/> structures.
    /// </summary>
    [DebuggerDisplay("{Flags}")]
    public readonly struct ImageCorILMethod : IValue, IViewable
    {
        //Tiny + Fat
        public CorILMethodFlags Flags { get; }
        public int CodeSize { get; }

        //Fat

        public byte Size { get; }

        public short MaxStack { get; }

#if PEFAST
        public NativeSpan<byte> ILBytes
        {
            get
            {
                var kind = (CorILMethodFlags) ((ushort) Flags & Extensions.CorILMethod_FormatMask);

                switch (kind)
                {
                    case CorILMethodFlags.TinyFormat:
                    case CorILMethodFlags.TinyFormat1:
                        return chunk.PeekNativeSpan<byte>(1, CodeSize);

                    case CorILMethodFlags.FatFormat:
                        return chunk.PeekNativeSpan<byte>(12, CodeSize);

                    default:
                        return default;
                }
            }
        }
#else
        public byte[]? ILBytes { get; }
#endif

        public mdSignature LocalVarSigTok { get; }

#if PEFAST
        public int Offset => chunk.AbsoluteOffset;
#else
        public int Offset { get; }
#endif

        public ImageCorILMethodSectEH[] EHSections { get; }

#if PEFAST
        private readonly MemoryChunk chunk;

        //It's a bit of a complicated structure due to the fact we're trying to represent a unioned type, so we eagerly read everything

        internal ImageCorILMethod(in MemoryChunk chunk, out bool isValid)
        {
            this.chunk = chunk;

            //Is it an IMAGE_COR_ILMETHOD_FAT or an IMAGE_COR_ILMETHOD_TINY?
            //Read the kind part of IMAGE_COR_ILMETHOD_FAT.FlagsAndSize or IMAGE_COR_ILMETHOD_TINY.Flags_CodeSize
            var byte1 = chunk.PeekByte(0);

            var kind = (CorILMethodFlags) (byte1 & Extensions.CorILMethod_FormatMask);

            //In tiny format, 2 will always be set (TinyFormat (2)), and if 4 is set that means its odd (TinyFormat1 (6))
            switch (kind)
            {
                case CorILMethodFlags.TinyFormat:
                case CorILMethodFlags.TinyFormat1:
                    Flags = (CorILMethodFlags) byte1;
                    CodeSize = (byte) (byte1 >> (Extensions.CorILMethod_FormatShift - 1));
                    Size = 1;
                    MaxStack = 8;

                    LocalVarSigTok = default;
                    isValid = true;

                    //Note that you can potentially have a byte like 0x1e which would indicate that there are MoreSects and InitLocals, however
                    //in the case of TinyFormat, these bits should be ignored (this is also how dnlib handles things)
                    EHSections = Array.Empty<ImageCorILMethodSectEH>();

                    break;

                case CorILMethodFlags.FatFormat:
                    var byte2 = chunk.PeekByte(1);
                    Flags = (CorILMethodFlags) (((byte2 & 0x0F) << 8) | (byte1)); //Flags: 12 bits
                    Size = (byte) (byte2 >> 4);
                    MaxStack = chunk.PeekInt16(2);
                    CodeSize = chunk.PeekInt32(4);
                    LocalVarSigTok = chunk.PeekUInt32(8);
                    isValid = true;

                    if ((Flags & CorILMethodFlags.MoreSects) != 0)
                    {
                        //Skip over the IL bytes, and then align to a 32-bit boundary
                        var read = (12 + CodeSize + 3) & ~3;

                        EHSections = ReadExtraSections(chunk.Slice(read));
                    }
                    else
                        EHSections = Array.Empty<ImageCorILMethodSectEH>();

                    break;

                default:
                    //You can have PInvokes that say they have RVAs but these don't point to valid data
                    Flags = default;
                    CodeSize = default;
                    Size = default;
                    MaxStack = default;
                    LocalVarSigTok = default;
                    isValid = false;
                    EHSections = Array.Empty<ImageCorILMethodSectEH>();
                    break;
            }
        }
#else
        internal ImageCorILMethod(IFileReader reader, out bool isValid)
        {
            Offset = (int) reader.Position;

            //Is it an IMAGE_COR_ILMETHOD_FAT or an IMAGE_COR_ILMETHOD_TINY?
            //Read the kind part of IMAGE_COR_ILMETHOD_FAT.FlagsAndSize or IMAGE_COR_ILMETHOD_TINY.Flags_CodeSize
            var byte1 = reader.ReadByte();

            var kind = (CorILMethodFlags) (byte1 & Extensions.CorILMethod_FormatMask);

            EHSections = Array.Empty<ImageCorILMethodSectEH>();

            //In tiny format, 2 will always be set (TinyFormat), and if 4 is set that means its odd (TinyFormat1)
            switch (kind)
            {
                case CorILMethodFlags.TinyFormat:
                case CorILMethodFlags.TinyFormat1:
                    Flags = (CorILMethodFlags) byte1;
                    CodeSize = (byte) (byte1 >> (Extensions.CorILMethod_FormatShift - 1));
                    Size = 1;
                    MaxStack = 8;
                    ILBytes = reader.ReadBytes(CodeSize);

                    LocalVarSigTok = default;
                    isValid = true;

                    //Note that you can potentially have a byte like 0x1e which would indicate that there are MoreSects and InitLocals, however
                    //in the case of TinyFormat, these bits should be ignored (this is also how dnlib handles things)

                    break;

                case CorILMethodFlags.FatFormat:
                    var byte2 = reader.ReadByte();
                    Flags = (CorILMethodFlags) (((byte2 & 0x0F) << 8) | (byte1)); //Flags: 12 bits
                    Size = (byte) (byte2 >> 4);
                    MaxStack = reader.ReadInt16();
                    CodeSize = reader.ReadInt32();
                    LocalVarSigTok = reader.ReadInt32();
                    ILBytes = reader.ReadBytes(CodeSize);
                    isValid = true;

                    if ((Flags & CorILMethodFlags.MoreSects) != 0)
                    {
                        var alignedPosition = (reader.Position + 3) & ~3;

                        while (reader.Position < alignedPosition)
                            reader.ReadByte();

                        EHSections = ReadExtraSections(reader);
                    }

                    break;

                default:
                    //You can have PInvokes that say they have RVAs but these don't point to valid data
                    Flags = default;
                    CodeSize = default;
                    Size = default;
                    MaxStack = default;
                    ILBytes = default;
                    LocalVarSigTok = default;
                    isValid = false;
                    break;
            }
        }
#endif

#if PEFAST
        private static ImageCorILMethodSectEH[] ReadExtraSections(in MemoryChunk chunk)
        {
            using var sections = new PooledList<ImageCorILMethodSectEH>();

            var sectFlags = (CorILMethodSect) chunk.PeekByte(0);

            var kind = sectFlags & CorILMethodSect.KindMask;

            switch (kind)
            {
                case CorILMethodSect.EHTable:
                    sections.Add(new ImageCorILMethodSectEH(kind, chunk));
                    break;

                case CorILMethodSect.OptILTable:
                case CorILMethodSect.Reserved:
                    break;

                default:
                    throw new NotImplementedException($"Don't know how to handle {nameof(CorILMethodSect)} '{kind}'");
            }

            if ((sectFlags & CorILMethodSect.MoreSects) != 0)
                throw new NotImplementedException("Don't know how to handle having more sections. Do we need to align first? And then jump back to the start (after initializing our list)?");

            return sections.ToArray();
        }
#else
        private static ImageCorILMethodSectEH[] ReadExtraSections(IFileReader reader)
        {
            using var sections = new PooledList<ImageCorILMethodSectEH>();

            var sectFlags = (CorILMethodSect) reader.ReadByte();

            var kind = sectFlags & CorILMethodSect.KindMask;

            switch (kind)
            {
                case CorILMethodSect.EHTable:
                    sections.Add(new ImageCorILMethodSectEH(kind, reader));
                    break;

                case CorILMethodSect.OptILTable:
                case CorILMethodSect.Reserved:
                    break;

                default:
                    throw new NotImplementedException($"Don't know how to handle {nameof(CorILMethodSect)} '{kind}'");
            }

            if ((sectFlags & CorILMethodSect.MoreSects) != 0)
                throw new NotImplementedException("Don't know how to handle having more sections. Do we need to align first? And then jump back to the start (after initializing our list)?");

            return sections.ToArray();
        }
#endif

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer)
        {
            var kind = (CorILMethodFlags) ((int) Flags & Extensions.CorILMethod_FormatMask);

            switch (kind)
            {
                case CorILMethodFlags.TinyFormat:
                case CorILMethodFlags.TinyFormat1:
                    return writer.NewStruct(Strings.IMAGE_COR_ILMETHOD_TINY, this, ViewKind.ImageCorILMethodTiny, sizeof(byte) + ILBytes.Length);

                case CorILMethodFlags.FatFormat:
                    return writer.NewStruct(Strings.IMAGE_COR_ILMETHOD_FAT, this, ViewKind.ImageCorILMethodFat, GetFatStructSize());

                default:
                    return null;
            }
        }

        private int GetFatStructSize()
        {
            var size =
                sizeof(int) + //Flags / Size / MaxStack
                sizeof(int) + //CodeSize
                sizeof(int) + //LocalVarSigTok
                ILBytes.Length; //ILBytes

            if (EHSections.Length > 0)
            {
                size = (size + 3) & ~3; //32-bit align

                var ehSections = EHSections;

                for (var i = 0; i < ehSections.Length; i++)
                    throw new NotImplementedException(); //todo: will the section's header's datasize tell us?
            }

            throw new NotImplementedException();
        }

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            var kind = (CorILMethodFlags) ((int) Flags & Extensions.CorILMethod_FormatMask);

            using var s = viewWriter.CreateStruct(parent);

            switch (kind)
            {
                case CorILMethodFlags.TinyFormat:
                case CorILMethodFlags.TinyFormat1:
                {
                    //The bit shifts make it very confusing, but per ECMA 335 II.25.4.2 the format is as follows
                    using (var b = s.WriteBitFields<byte>())
                    {
                        b.WriteField("Flags", Flags, 2);
                        b.WriteField("CodeSize", CodeSize, 6);
                    }

                    s.WriteField("ILBytes", ILBytes); //Not sure what the best way to write this is; it's not really a "field"
                    break;
                }

                case CorILMethodFlags.FatFormat:
                {
                    using (var b = s.WriteBitFields<uint>())
                    {
                        b.WriteField(nameof(Flags), Flags, 12);
                        b.WriteField(nameof(Size), Size, 4);
                        b.WriteField(nameof(MaxStack), MaxStack, 16);
                    }

                    s.WriteField(nameof(CodeSize), CodeSize);
                    s.WriteField(nameof(LocalVarSigTok), LocalVarSigTok);
                    s.WriteField("ILBytes", ILBytes); //Not sure what the best way to write this is; it's not really a "field"

                    if (EHSections.Length > 0)
                    {
                        s.Align(4);

                        s.WriteInline(EHSections);
                    }

                    break;
                }
            }

            return s.ToArray();
        }
    }
}
