using System;
using System.Diagnostics;
using ClrDebug;
using PESpy.View;

namespace PESpy
{
    /// <summary>
    /// Represents the <see cref="IMAGE_RELOCATION"/> structure.
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public readonly struct ImageRelocation : IValue, IViewable
    {
        private const int VirtualAddressOffset = 0;
        private const int SymbolTableIndexOffset = 4;
        private const int TypeOffset = 8;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string DebuggerDisplay => $"VirtualAddress = {VirtualAddress}, SymbolTableIndex = {SymbolTableIndex}, Type = {(ImageRelI386) Type} (I386) / {(ImageRelAmd64) Type} (Amd64)";

        public int VirtualAddress => chunk.PeekInt32(VirtualAddressOffset);

        public int RelocCount => VirtualAddress;

        public int SymbolTableIndex => chunk.PeekInt32(SymbolTableIndexOffset);

        public short Type => chunk.PeekInt16(TypeOffset);


        internal const int StructSize =
            sizeof(int) + //VirtualAddress
            sizeof(int) + //SymbolTableIndex
            sizeof(short); //Type

        public int Offset => chunk.AbsoluteOffset;

        private readonly MemoryChunk chunk;

        internal ImageRelocation(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.IMAGE_RELOCATION, this, ViewKind.ImageRelocation, StructSize);

        int IViewable.NumChildren() => 3;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(VirtualAddress), VirtualAddressOffset, VirtualAddress);
                    break;

                case 1:
                    structWriter.WriteField(nameof(SymbolTableIndex), SymbolTableIndexOffset, SymbolTableIndex);
                    break;

                case 2:
                    var machine = structWriter.GetMachine(chunk);

                    //Unknown:
                    //ImageRelAm
                    //ImageRelBased
                    //ImageRelMips

                    switch (machine)
                    {
                        case IMAGE_FILE_MACHINE.UNKNOWN:
                            goto default;

                        case IMAGE_FILE_MACHINE.I386:
                            structWriter.WriteField(nameof(Type), TypeOffset, (ImageRelI386) Type, sizeof(short));
                            break;

                        case IMAGE_FILE_MACHINE.R3000:
                        case IMAGE_FILE_MACHINE.R4000:
                        case IMAGE_FILE_MACHINE.R10000:
                        case IMAGE_FILE_MACHINE.WCEMIPSV2:
                            goto default;

                        case IMAGE_FILE_MACHINE.ALPHA:
                            structWriter.WriteField(nameof(Type), TypeOffset, (ImageRelAlpha) Type, sizeof(short));
                            break;

                        case IMAGE_FILE_MACHINE.SH3:
                            structWriter.WriteField(nameof(Type), TypeOffset, (ImageRelSh3) Type, sizeof(short));
                            break;

                        case IMAGE_FILE_MACHINE.SH3DSP:
                        case IMAGE_FILE_MACHINE.SH3E:
                        case IMAGE_FILE_MACHINE.SH4:
                        case IMAGE_FILE_MACHINE.SH5:
                        case IMAGE_FILE_MACHINE.ARM:
                            structWriter.WriteField(nameof(Type), TypeOffset, (ImageRelArm) Type, sizeof(short));
                            break;

                        case IMAGE_FILE_MACHINE.THUMB:
                        case IMAGE_FILE_MACHINE.ARMNT:
                        case IMAGE_FILE_MACHINE.AM33:
                        case IMAGE_FILE_MACHINE.POWERPC:
                        case IMAGE_FILE_MACHINE.POWERPCFP:
                        case IMAGE_FILE_MACHINE.IA64:
                            structWriter.WriteField(nameof(Type), TypeOffset, (ImageRelIa64) Type, sizeof(short));
                            break;

                        case IMAGE_FILE_MACHINE.MIPS16:
                        case IMAGE_FILE_MACHINE.ALPHA64:
                        case IMAGE_FILE_MACHINE.MIPSFPU:
                        case IMAGE_FILE_MACHINE.MIPSFPU16:
                        case IMAGE_FILE_MACHINE.TRICORE:
                            goto default;

                        case IMAGE_FILE_MACHINE.CEF:
                            structWriter.WriteField(nameof(Type), TypeOffset, (ImageRelCef) Type, sizeof(short));
                            break;

                        case IMAGE_FILE_MACHINE.EBC:
                            structWriter.WriteField(nameof(Type), TypeOffset, (ImageRelEbc) Type, sizeof(short));
                            break;

                        case IMAGE_FILE_MACHINE.AMD64:
                            structWriter.WriteField(nameof(Type), TypeOffset, (ImageRelAmd64) Type, sizeof(short));
                            break;

                        case IMAGE_FILE_MACHINE.M32R:
                            structWriter.WriteField(nameof(Type), TypeOffset, (ImageRelM32R) Type, sizeof(short));
                            break;

                        case IMAGE_FILE_MACHINE.ARM64:
                            structWriter.WriteField(nameof(Type), TypeOffset, (ImageRelArm64) Type, sizeof(short));
                            break;

                        case IMAGE_FILE_MACHINE.CEE:
                            structWriter.WriteField(nameof(Type), TypeOffset, (ImageRelCee) Type, sizeof(short));
                            break;

                        default:
                            structWriter.WriteField(nameof(Type), TypeOffset, Type);
                            break;
                    }
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
