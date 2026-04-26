using System;
using System.Diagnostics;
using ClrDebug;
using PESpy.View;
using static ClrDebug.IMAGE_FILE_MACHINE;

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
        private string DebuggerDisplay => $"VirtualAddress = {VirtualAddress}, SymbolTableIndex = {SymbolTableIndex}, Type = {(IMAGE_REL_I386) Type} (I386) / {(IMAGE_REL_AMD64) Type} (Amd64)";

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
            writer.NewStruct(this, ViewKind.ImageRelocation, StructSize);

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
                        case IMAGE_FILE_MACHINE_UNKNOWN:
                            goto default;

                        case IMAGE_FILE_MACHINE_I386:
                            structWriter.WriteField(nameof(Type), TypeOffset, (IMAGE_REL_I386) Type, sizeof(short));
                            break;

                        case IMAGE_FILE_MACHINE_R3000:
                        case IMAGE_FILE_MACHINE_R4000:
                        case IMAGE_FILE_MACHINE_R10000:
                        case IMAGE_FILE_MACHINE_WCEMIPSV2:
                            goto default;

                        case IMAGE_FILE_MACHINE_ALPHA:
                            structWriter.WriteField(nameof(Type), TypeOffset, (IMAGE_REL_ALPHA) Type, sizeof(short));
                            break;

                        case IMAGE_FILE_MACHINE_SH3:
                            structWriter.WriteField(nameof(Type), TypeOffset, (IMAGE_REL_SH3) Type, sizeof(short));
                            break;

                        case IMAGE_FILE_MACHINE_SH3DSP:
                        case IMAGE_FILE_MACHINE_SH3E:
                        case IMAGE_FILE_MACHINE_SH4:
                        case IMAGE_FILE_MACHINE_SH5:
                        case IMAGE_FILE_MACHINE_ARM:
                            structWriter.WriteField(nameof(Type), TypeOffset, (IMAGE_REL_ARM) Type, sizeof(short));
                            break;

                        case IMAGE_FILE_MACHINE_THUMB:
                        case IMAGE_FILE_MACHINE_ARMNT:
                        case IMAGE_FILE_MACHINE_AM33:
                        case IMAGE_FILE_MACHINE_POWERPC:
                        case IMAGE_FILE_MACHINE_POWERPCFP:
                        case IMAGE_FILE_MACHINE_IA64:
                            structWriter.WriteField(nameof(Type), TypeOffset, (IMAGE_REL_IA64) Type, sizeof(short));
                            break;

                        case IMAGE_FILE_MACHINE_MIPS16:
                        case IMAGE_FILE_MACHINE_ALPHA64:
                        case IMAGE_FILE_MACHINE_MIPSFPU:
                        case IMAGE_FILE_MACHINE_MIPSFPU16:
                        case IMAGE_FILE_MACHINE_TRICORE:
                            goto default;

                        case IMAGE_FILE_MACHINE_CEF:
                            structWriter.WriteField(nameof(Type), TypeOffset, (IMAGE_REL_CEF) Type, sizeof(short));
                            break;

                        case IMAGE_FILE_MACHINE_EBC:
                            structWriter.WriteField(nameof(Type), TypeOffset, (IMAGE_REL_EBC) Type, sizeof(short));
                            break;

                        case IMAGE_FILE_MACHINE_AMD64:
                            structWriter.WriteField(nameof(Type), TypeOffset, (IMAGE_REL_AMD64) Type, sizeof(short));
                            break;

                        case IMAGE_FILE_MACHINE_M32R:
                            structWriter.WriteField(nameof(Type), TypeOffset, (IMAGE_REL_M32R) Type, sizeof(short));
                            break;

                        case IMAGE_FILE_MACHINE_ARM64:
                            structWriter.WriteField(nameof(Type), TypeOffset, (IMAGE_REL_ARM64) Type, sizeof(short));
                            break;

                        case IMAGE_FILE_MACHINE_CEE:
                            structWriter.WriteField(nameof(Type), TypeOffset, (IMAGE_REL_CEE) Type, sizeof(short));
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
