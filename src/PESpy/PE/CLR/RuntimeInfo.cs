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
                    var start = 22 + (3 * ModuleIndex.StructSize);

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

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct(nameof(RuntimeInfo), this, ViewKind.RuntimeInfo);

            s.WriteUTF8NullTerminatedField(nameof(Signature), Signature);
            s.Align(4);

            s.WriteField(nameof(Version), Version);
            s.WriteUnmanagedField(nameof(RuntimeModuleIndex), RuntimeModuleIndex);
            s.WriteUnmanagedField(nameof(DacModuleIndex), DacModuleIndex);
            s.WriteUnmanagedField(nameof(DbiModuleIndex), DbiModuleIndex);

            if (Version >= 2)
            {
                s.WriteField(nameof(RuntimeVersion), new int[] { RuntimeVersion!.Major, RuntimeVersion.Minor, RuntimeVersion.Build, RuntimeVersion.Revision });
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        [DebuggerDisplay("Size = {Size}, TimeStamp = {TimeStamp}, ImageSize = {ImageSize}")]
        public unsafe struct ModuleIndex
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
        }
    }
}
