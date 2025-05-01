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
        public string Signature { get; }

        public int Version { get; }

        public ModuleIndex RuntimeModuleIndex { get; }

        public ModuleIndex DacModuleIndex { get; }

        public ModuleIndex DbiModuleIndex { get; }

        public Version? RuntimeVersion { get; }

        public int Offset { get; }

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
                s.WriteField(nameof(RuntimeVersion), new int[] { RuntimeVersion.Major, RuntimeVersion.Minor, RuntimeVersion.Build, RuntimeVersion.Revision });
            }
        }

        //This type is made up
        [DebuggerDisplay("Size = {Size}, TimeStamp = {TimeStamp}, ImageSize = {ImageSize}")]
        public struct ModuleIndex : IValue, IViewable
        {
            //https://github.com/dotnet/runtime/blob/511d26611c051c56e546404ea616c220cc78817c/eng/native/genmoduleindex.cmd#L4

            public int Offset { get; }

            public byte Size { get; }

            public uint TimeStamp { get; }

            public int ImageSize { get; }

            public byte[] Extra { get; }

            internal ModuleIndex(IFileReader reader)
            {
                Offset = (int) reader.Position;

                Size = reader.ReadByte();
                TimeStamp = reader.ReadUInt32();
                ImageSize = reader.ReadInt32();

                //The module index is 24 bytes. Read the remaining bytes (currently unused)
                Extra = reader.ReadBytes(15);
            }

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
