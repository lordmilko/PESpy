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
#if PEFAST
        public Utf8String Signature => chunk.PeekUtf8NullTerminatedString(0); //Should be DotNetRuntimeInfo\0
#else
        public string Signature { get; }
#endif

#if PEFAST
        public int Version => chunk.PeekInt32(20); //2 bytes pf padding for alignment
#else
        public int Version { get; }
#endif

#if PEFAST
        public ModuleIndex RuntimeModuleIndex => chunk.PeekUnmanaged<ModuleIndex>(24);
#else
        public ModuleIndex RuntimeModuleIndex { get; }
#endif

#if PEFAST
        public ModuleIndex DacModuleIndex => chunk.PeekUnmanaged<ModuleIndex>(24 + ModuleIndex.StructSize);
#else
        public ModuleIndex DacModuleIndex { get; }
#endif

#if PEFAST
        public ModuleIndex DbiModuleIndex => chunk.PeekUnmanaged<ModuleIndex>(24 + (2 * ModuleIndex.StructSize));
#else
        public ModuleIndex DbiModuleIndex { get; }
#endif

#if PEFAST
        private Version? runtimeVersion;

        public Version? RuntimeVersion
        {
            get
            {
                if (runtimeVersion == null && Version >= 2)
                {
                    var start = 24 + (3 * ModuleIndex.StructSize);

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
#else
        public Version? RuntimeVersion { get; }
#endif

#if PEFAST
        public int Offset => chunk.AbsoluteOffset;
#else
        public int Offset { get; }
#endif

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

#if PEFAST
        private readonly MemoryChunk chunk;

        internal RuntimeInfo(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }
#else
        internal RuntimeInfo(IFileReader reader)
        {
            Offset = (int) reader.Position;

            Signature = reader.ReadUTF8NullTerminatedString();

            if (Signature != "DotNetRuntimeInfo")
            {
                throw new NotImplementedException("Don't know how to handle having an invalid signature");
            }

            //Signature is 18 bytes, so need to read 2 more for alignment
            var padding = reader.ReadInt16();

            Version = reader.ReadInt32();

            RuntimeModuleIndex = new ModuleIndex(reader);
            DacModuleIndex = new ModuleIndex(reader);
            DbiModuleIndex = new ModuleIndex(reader);

            if (Version >= 2)
            {
                RuntimeVersion = new Version(
                    reader.ReadInt32(),
                    reader.ReadInt32(),
                    reader.ReadInt32(),
                    reader.ReadInt32()
                );
            }
            else
            {
                RuntimeVersion = null;
            }
        }
#endif

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.RuntimeInfo, this, ViewKind.RuntimeInfo, StructSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteUTF8NullTerminatedField(nameof(Signature), Signature);
            s.Align(4);

            s.WriteField(nameof(Version), Version);
            s.WriteStructField(nameof(RuntimeModuleIndex), RuntimeModuleIndex);
            s.WriteStructField(nameof(DacModuleIndex), DacModuleIndex);
            s.WriteStructField(nameof(DbiModuleIndex), DbiModuleIndex);

            if (Version >= 2)
            {
                s.WriteField(nameof(RuntimeVersion), new int[] { RuntimeVersion!.Major, RuntimeVersion.Minor, RuntimeVersion.Build, RuntimeVersion.Revision });
            }

            return s.ToArray();
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        [DebuggerDisplay("Size = {Size}, TimeStamp = {TimeStamp}, ImageSize = {ImageSize}")]
        public unsafe struct ModuleIndex : IViewable
        {
            public byte Size;
            public uint TimeStamp;
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

            IView[] IViewable.GetChildren(IView parent, ViewWriter writer)
            {
                using var s = writer.CreateStruct(parent);

                fixed (byte* e = Extra)
                {
                    s.WriteField(nameof(Size), Size);
                    s.WriteField(nameof(TimeStamp), TimeStamp);
                    s.WriteField(nameof(ImageSize), ImageSize);
                    s.WriteField(nameof(Extra), new NativeSpan<byte>(e, 15));

                    return s.ToArray();
                }
            }
        }
    }
}
