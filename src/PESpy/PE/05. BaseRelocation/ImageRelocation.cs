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
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string DebuggerDisplay => $"VirtualAddress = {VirtualAddress}, SymbolTableIndex = {SymbolTableIndex}, Type = {(ImageRelI386) Type} (I386) / {(ImageRelAmd64) Type} (Amd64)";

#if PEFAST
        public int VirtualAddress => chunk.PeekInt32(0);

        public int RelocCount => VirtualAddress;

        public int SymbolTableIndex => chunk.PeekInt32(4);

        public short Type => chunk.PeekInt16(8);
#else
        public int VirtualAddress { get; init; }

        public int RelocCount => VirtualAddress;

        public int SymbolTableIndex { get; }

        public short Type { get; }
#endif

        internal const int StructSize =
            sizeof(int) + //VirtualAddress
            sizeof(int) + //SymbolTableIndex
            sizeof(short); //Type

#if PEFAST
        public int Offset => chunk.AbsoluteOffset;

        private readonly MemoryChunk chunk;

        internal ImageRelocation(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

#else
        public int Offset { get; }

        internal ImageRelocation(IFileReader reader)
        {
            Offset = (int) reader.Position;

            VirtualAddress = reader.ReadInt32();
            SymbolTableIndex = reader.ReadInt32();
            Type = reader.ReadInt16();
        }
#endif

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.IMAGE_RELOCATION, this, ViewKind.ImageRelocation, StructSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(VirtualAddress), VirtualAddress);
            s.WriteField(nameof(SymbolTableIndex), SymbolTableIndex);

            var machine = ((IMachineWriter) viewWriter).Machine;

            //Unknown:
            //ImageRelAm
            //ImageRelBased
            //ImageRelMips

            switch (machine)
            {
                case IMAGE_FILE_MACHINE.UNKNOWN:
                    goto default;

                case IMAGE_FILE_MACHINE.I386:
                    s.WriteField(nameof(Type), (ImageRelI386) Type, sizeof(short));
                    break;

                case IMAGE_FILE_MACHINE.R3000:
                case IMAGE_FILE_MACHINE.R4000:
                case IMAGE_FILE_MACHINE.R10000:
                case IMAGE_FILE_MACHINE.WCEMIPSV2:
                    goto default;

                case IMAGE_FILE_MACHINE.ALPHA:
                    s.WriteField(nameof(Type), (ImageRelAlpha) Type, sizeof(short));
                    break;

                case IMAGE_FILE_MACHINE.SH3:
                    s.WriteField(nameof(Type), (ImageRelSh3) Type, sizeof(short));
                    break;

                case IMAGE_FILE_MACHINE.SH3DSP:
                case IMAGE_FILE_MACHINE.SH3E:
                case IMAGE_FILE_MACHINE.SH4:
                case IMAGE_FILE_MACHINE.SH5:
                case IMAGE_FILE_MACHINE.ARM:
                    s.WriteField(nameof(Type), (ImageRelArm) Type, sizeof(short));
                    break;

                case IMAGE_FILE_MACHINE.THUMB:
                case IMAGE_FILE_MACHINE.ARMNT:
                case IMAGE_FILE_MACHINE.AM33:
                case IMAGE_FILE_MACHINE.POWERPC:
                case IMAGE_FILE_MACHINE.POWERPCFP:
                case IMAGE_FILE_MACHINE.IA64:
                    s.WriteField(nameof(Type), (ImageRelIa64) Type, sizeof(short));
                    break;

                case IMAGE_FILE_MACHINE.MIPS16:
                case IMAGE_FILE_MACHINE.ALPHA64:
                case IMAGE_FILE_MACHINE.MIPSFPU:
                case IMAGE_FILE_MACHINE.MIPSFPU16:
                case IMAGE_FILE_MACHINE.TRICORE:
                    goto default;

                case IMAGE_FILE_MACHINE.CEF:
                    s.WriteField(nameof(Type), (ImageRelCef) Type, sizeof(short));
                    break;

                case IMAGE_FILE_MACHINE.EBC:
                    s.WriteField(nameof(Type), (ImageRelEbc) Type, sizeof(short));
                    break;

                case IMAGE_FILE_MACHINE.AMD64:
                    s.WriteField(nameof(Type), (ImageRelAmd64) Type, sizeof(short));
                    break;

                case IMAGE_FILE_MACHINE.M32R:
                    s.WriteField(nameof(Type), (ImageRelM32R) Type, sizeof(short));
                    break;

                case IMAGE_FILE_MACHINE.ARM64:
                    s.WriteField(nameof(Type), (ImageRelArm64) Type, sizeof(short));
                    break;

                case IMAGE_FILE_MACHINE.CEE:
                    s.WriteField(nameof(Type), (ImageRelCee) Type, sizeof(short));
                    break;

                default:
                    s.WriteField(nameof(Type), Type);
                    break;
            }

            return s.ToArray();
        }
    }
}
