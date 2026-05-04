using System;
using System.Diagnostics;
using ClrDebug;
using PESpy.Ecma335;
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

        public mdSignature LocalVarSigTok { get; }

        public StandAloneSigRow? Sig => LocalVarSigTok.Rid == 0 ? null : chunk.PEFile().EcmaMetadata.CompressedModelHeap.StandAloneSigTable.FromToken(LocalVarSigTok);

        public ImageCorILMethodSectEH[] EHSections { get; }

        public long Offset => chunk.AbsoluteOffset;

        internal const int TinyStructSize =
            sizeof(byte);

        internal const int FatStructSize =
            sizeof(int) + //Flags / Size / MaxStack
                sizeof(int) + //CodeSize
                sizeof(int); //LocalVarSigTok

        private readonly MemoryChunk chunk;

        //It's a bit of a complicated structure due to the fact we're trying to represent a unioned type, so we eagerly read everything

        //Should only be used by ViewProvider for methods we already know are valid
        internal ImageCorILMethod(in MemoryChunk chunk) : this(chunk, out _)
        {
        }

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

        private static ImageCorILMethodSectEH[] ReadExtraSections(in MemoryChunk chunk)
        {
            using var sections = new PooledList<ImageCorILMethodSectEH>();

            var sectFlags = (CorILMethodSect) chunk.PeekByte(0);

            var kind = sectFlags & CorILMethodSect.KindMask;

            switch (kind)
            {
                case CorILMethodSect.EHTable:
                    sections.Add(new ImageCorILMethodSectEH(sectFlags, chunk));
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

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //We want to be able to tag IL as code, but that causes an issue because if we have EH Sections, we'll no longer see those bytes
            //as being part of the ImageCorILMethod. So instead we need to write the IL and EH Sections as globals. This arguably matches the
            //native definitions anyway, which don't say that they "contain" the EH Sections or any of the IL
            var kind = (CorILMethodFlags) ((int) Flags & Extensions.CorILMethod_FormatMask);

            switch (kind)
            {
                case CorILMethodFlags.TinyFormat:
                case CorILMethodFlags.TinyFormat1:
                    //Just write the IL
                    writer.WriteIL(Offset + TinyStructSize, ILBytes);
                    break;

                case CorILMethodFlags.FatFormat:
                    writer.WriteIL(Offset + FatStructSize, ILBytes);

                    if (EHSections.Length > 0)
                    {
                        var padding = ((ILBytes.Length + 3) & ~3) - ILBytes.Length;

                        if (padding > 0)
                            writer.WritePadding(Offset + FatStructSize + ILBytes.Length, chunk.PeekNativeSpan<byte>(FatStructSize + ILBytes.Length, padding));

                        writer.WriteGlobal(EHSections);
                    }
                    break;

                default:
                    throw new NotImplementedException($"Don't know how to handle {nameof(CorILMethodSect)} '{kind}'");
            }
        }

        IView? IViewable.WriteStruct(ViewWriter writer)
        {
            var kind = (CorILMethodFlags) ((int) Flags & Extensions.CorILMethod_FormatMask);

            switch (kind)
            {
                case CorILMethodFlags.TinyFormat:
                case CorILMethodFlags.TinyFormat1:
                    return writer.NewStruct(this, ViewKind.ImageCorILMethodTiny, TinyStructSize);

                case CorILMethodFlags.FatFormat:

                    return writer.NewStruct(this, ViewKind.ImageCorILMethodFat, FatStructSize);

                default:
                    return null;
            }
        }

        int IViewable.NumChildren() => throw StructWriter.GetEagerLoadOnlyException();

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            if (index != -1)
                throw StructWriter.GetEagerLoadOnlyException();

            var kind = (CorILMethodFlags) ((int) Flags & Extensions.CorILMethod_FormatMask);

            using var s = structWriter.CreateEagerWriter();

            switch (kind)
            {
                case CorILMethodFlags.TinyFormat:
                case CorILMethodFlags.TinyFormat1:
                    {
                        //The bit shifts make it very confusing, but per ECMA 335 II.25.4.2 the format is as follows
                        using (var b = s.WriteBitFields<byte>(2))
                        {
                            b.WriteField("Flags", Flags, 2);
                            b.WriteField("CodeSize", CodeSize, 6);
                        }
                        break;
                    }

                case CorILMethodFlags.FatFormat:
                    {
                        using (var b = s.WriteBitFields<uint>(3))
                        {
                            b.WriteField(nameof(Flags), Flags, 12);
                            b.WriteField(nameof(Size), Size, 4);
                            b.WriteField(nameof(MaxStack), MaxStack, 16);
                        }

                        s.WriteField(nameof(CodeSize), CodeSize);
                        s.WriteField(nameof(LocalVarSigTok), LocalVarSigTok);

                        break;
                    }
            }

            structWriter.EagerFields = s.ToArray();
        }
    }
}
