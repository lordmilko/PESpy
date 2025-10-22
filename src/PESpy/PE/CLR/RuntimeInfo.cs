using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using PESpy.View;

namespace PESpy
{
    /// <summary>
    /// Represents the .NET Single File App <see cref="PESpy.Native.RuntimeInfo"/> structure pointed to by the "DotNetRuntimeInfo" export
    /// that describes the CLR, DAC and DBI versions that are associated with this executable.
    /// </summary>
    public class RuntimeInfo : IValue, IViewable
    {
        private const int SignatureOffset = 0;
        private const int VersionOffset = 20;
        private const int RuntimeModuleIndexOffset = 24;
        private const int DacModuleIndexOffset = 24 + ModuleIndex.StructSize;
        private const int DbiModuleIndexOffset = 24 + (2 * ModuleIndex.StructSize);
        private const int RuntimeVersionOffset = 24 + (3 * ModuleIndex.StructSize);

        public Utf8String Signature => chunk.PeekUtf8NullTerminatedString(SignatureOffset); //Should be DotNetRuntimeInfo\0

        public int Version => chunk.PeekInt32(VersionOffset); //2 bytes pf padding for alignment

        public ModuleIndex RuntimeModuleIndex => chunk.PeekUnmanaged<ModuleIndex>(RuntimeModuleIndexOffset);

        public ModuleIndex DacModuleIndex => chunk.PeekUnmanaged<ModuleIndex>(DacModuleIndexOffset);

        public ModuleIndex DbiModuleIndex => chunk.PeekUnmanaged<ModuleIndex>(DbiModuleIndexOffset);

        private Version? runtimeVersion;

        public Version? RuntimeVersion
        {
            get
            {
                if (runtimeVersion == null && Version >= 2)
                {
                    var start = RuntimeVersionOffset;

                    runtimeVersion = new Version(
                        chunk.PeekInt32(start),
                        chunk.PeekInt32(start + 4),
                        chunk.PeekInt32(start + 8),
                        chunk.PeekInt32(start + 12)
                    );
                }

                return runtimeVersion;
            }
        }

        public int Offset => chunk.AbsoluteOffset;

        internal const int FixedStructSize =
            20 + //Signature + padding
            sizeof(int) + //Version
            ModuleIndex.StructSize + //RuntimeModuleIndex
            ModuleIndex.StructSize + //DacModuleIndex
            ModuleIndex.StructSize; //DbiModuleIndex

        internal int StructSize =>
            Version < 2
            ? FixedStructSize
            : FixedStructSize + (4 * sizeof(int));

        private readonly MemoryChunk chunk;

        internal RuntimeInfo(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.RuntimeInfo, this, ViewKind.RuntimeInfo, StructSize);

        int IViewable.NumChildren() => Version >= 2 ? 7 : 6;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteUtf8NullTerminatedField(nameof(Signature), SignatureOffset, Signature);
                    break;

                case 1:
                    structWriter.WriteByteBlob(VersionOffset - sizeof(ushort), sizeof(ushort));
                    break;

                case 2:
                    structWriter.WriteField(nameof(Version), VersionOffset, Version);
                    break;

                case 3:
                    structWriter.WriteStructField(nameof(RuntimeModuleIndex), RuntimeModuleIndexOffset, RuntimeModuleIndex);
                    break;

                case 4:
                    structWriter.WriteStructField(nameof(DacModuleIndex), DacModuleIndexOffset, DacModuleIndex);
                    break;

                case 5:
                    structWriter.WriteStructField(nameof(DbiModuleIndex), DbiModuleIndexOffset, DbiModuleIndex);
                    break;

                case 6:
                    if (Version >= 2)
                        structWriter.WriteField(nameof(RuntimeVersion), RuntimeVersionOffset, new int[] { RuntimeVersion!.Major, RuntimeVersion.Minor, RuntimeVersion.Build, RuntimeVersion.Revision });
                    else
                        throw new IndexOutOfRangeException();

                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        [DebuggerDisplay("Size = {Size}, TimeStamp = {TimeStamp}, ImageSize = {ImageSize}")]
        public unsafe struct ModuleIndex : IViewable
        {
            private const int SizeOffset = 0;
            private const int TimeStampOffset = 1;
            private const int ImageSizeOffset = 5;
            private const int ExtraOffset = 9;

            public byte Size;
            public Timestamp TimeStamp;
            public int ImageSize;
            public fixed byte Extra[15];

            internal const int StructSize =
                sizeof(byte) + //Size
                sizeof(uint) + //TimeStamp
                sizeof(int) + //ImageSize
                15; //Module index is 24 bytes. Remaining bytes are currently unused

            void IViewable.WriteGlobals(ViewWriter writer)
            {
                //No globals
            }

            IView? IViewable.WriteStruct(ViewWriter writer) =>
                writer.NewUnmanagedStruct(Strings.ModuleIndex, this, ViewKind.ModuleIndex, StructSize);

            int IViewable.NumChildren() => 4;

            void IViewable.WriteChild(int index, ref StructWriter structWriter)
            {
                switch (index)
                {
                    case 0:
                        structWriter.WriteField(nameof(Size), SizeOffset, Size);
                        break;

                    case 1:
                        structWriter.WriteField(nameof(TimeStamp), TimeStampOffset, TimeStamp);
                        break;

                    case 2:
                        structWriter.WriteField(nameof(ImageSize), ImageSizeOffset, ImageSize);
                        break;

                    case 3:
                        fixed (byte* e = Extra)
                            structWriter.WriteField(nameof(Extra), ExtraOffset, new NativeSpan<byte>(e, 15));
                        break;

                    default:
                        throw new IndexOutOfRangeException();
                }
            }
        }
    }
}
