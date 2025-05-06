using System;
using System.Diagnostics;
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
        public int Version => chunk.PeekInt32(18);
#else
        public int Version { get; }
#endif

#if PEFAST
        public ModuleIndex RuntimeModuleIndex => new ModuleIndex(chunk.Slice(22));
#else
        public ModuleIndex RuntimeModuleIndex { get; }
#endif

#if PEFAST
        public ModuleIndex DacModuleIndex => new ModuleIndex(chunk.Slice(22 + ModuleIndex.StructSize));
#else
        public ModuleIndex DacModuleIndex { get; }
#endif

#if PEFAST
        public ModuleIndex DbiModuleIndex => new ModuleIndex(chunk.Slice(22 + (2 * ModuleIndex.StructSize)));
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
            s.WriteStructField(nameof(RuntimeModuleIndex), RuntimeModuleIndex);
            s.WriteStructField(nameof(DacModuleIndex), DacModuleIndex);
            s.WriteStructField(nameof(DbiModuleIndex), DbiModuleIndex);

            if (Version >= 2)
            {
                s.WriteField(nameof(RuntimeVersion), new int[] { RuntimeVersion!.Major, RuntimeVersion.Minor, RuntimeVersion.Build, RuntimeVersion.Revision });
            }
        }

        //This type is made up
        [DebuggerDisplay("Size = {Size}, TimeStamp = {TimeStamp}, ImageSize = {ImageSize}")]
        public struct ModuleIndex : IValue, IViewable
        {
            //https://github.com/dotnet/runtime/blob/511d26611c051c56e546404ea616c220cc78817c/eng/native/genmoduleindex.cmd#L4

#if PEFAST
            public byte Size => chunk.PeekByte(0);
#else
            public byte Size { get; }
#endif

#if PEFAST
            public uint TimeStamp => chunk.PeekUInt32(1);
#else
            public uint TimeStamp { get; }
#endif

#if PEFAST
            public int ImageSize => chunk.PeekInt32(5);
#else
            public int ImageSize { get; }
#endif

#if PEFAST
            public Span<byte> Extra => chunk.PeekSpan<byte>(9, 15);
#else
            public byte[] Extra { get; }
#endif

#if PEFAST
            public int Offset => chunk.AbsoluteOffset;
#else
            public int Offset { get; }
#endif

            internal const int StructSize =
                sizeof(byte) + //Size
                sizeof(uint) + //TimeStamp
                sizeof(int) + //ImageSize
                15; //Module index is 24 bytes. Remaining bytes are currently unused

#if PEFAST
            private readonly MemoryChunk chunk;

            internal ModuleIndex(in MemoryChunk chunk)
            {
                this.chunk = chunk;
            }
#else
            internal ModuleIndex(IFileReader reader)
            {
                Offset = (int) reader.Position;

                Size = reader.ReadByte();
                TimeStamp = reader.ReadUInt32();
                ImageSize = reader.ReadInt32();

                //The module index is 24 bytes. Read the remaining bytes (currently unused)
                Extra = reader.ReadBytes(15);
            }
#endif

            void IViewable.WriteView(ViewWriter writer)
            {
                using var s = writer.CreateStruct("Module Index", this, ViewKind.ModuleIndex);

                s.WriteField(nameof(Size), Size);
                s.WriteField(nameof(TimeStamp), TimeStamp);
                s.WriteField(nameof(ImageSize), ImageSize);
                s.WriteField(nameof(Extra), Extra);
            }
        }
    }
}
