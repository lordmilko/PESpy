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

        public byte[]? ILBytes { get; }

        public mdSignature LocalVarSigTok { get; }

        public int Offset { get; }

        public ImageCorILMethodSectEH[] EHSections { get; }

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

                    if (Flags.HasFlag(CorILMethodFlags.MoreSects))
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

        private static ImageCorILMethodSectEH[] ReadExtraSections(IFileReader reader)
        {
            var sections = new List<ImageCorILMethodSectEH>();

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

            if (sectFlags.HasFlag(CorILMethodSect.MoreSects))
                throw new NotImplementedException("Don't know how to handle having more sections. Do we need to align first? And then jump back to the start (after initializing our list)?");

            return sections.ToArray();
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            var kind = (CorILMethodFlags) ((int) Flags & Extensions.CorILMethod_FormatMask);

            switch (kind)
            {
                case CorILMethodFlags.TinyFormat:
                case CorILMethodFlags.TinyFormat1:
                {
                    using var s = writer.CreateStruct(nameof(IMAGE_COR_ILMETHOD_TINY), this, ViewKind.ImageCorILMethodTiny);

                    var value = (byte) (((byte) Flags & Extensions.CorILMethod_FormatMask) | (CodeSize << (Extensions.CorILMethod_FormatShift - 1)));

                    s.WriteField("Flags_CodeSize", value);
                    s.WriteField("ILBytes", ILBytes); //Not sure what the best way to write this is; it's not really a "field"
                    break;
                }

                case CorILMethodFlags.FatFormat:
                {
                    using var s = writer.CreateStruct(nameof(IMAGE_COR_ILMETHOD_FAT), this, ViewKind.ImageCorILMethodFat);

                    using (var b = s.WriteBitFields<uint>())
                    {
                        b.WriteField(nameof(Flags), Flags, 12);
                        b.WriteField(nameof(Size), Size, 4);
                        b.WriteField(nameof(MaxStack), MaxStack, 16);
                    }

                    s.WriteField(nameof(CodeSize), CodeSize);
                    s.WriteField(nameof(LocalVarSigTok), LocalVarSigTok);
                    s.WriteField("ILBytes", ILBytes); //Not sure what the best way to write this is; it's not really a "field"

                    throw new NotImplementedException("Need to align and then write the sections");
                    break;
                }
            }
        }
    }
}
