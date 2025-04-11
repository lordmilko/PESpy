using System;

namespace PESpy
{
    public class RuntimeInfo : IValue
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

            //It's not really null padded, but there's a trailing \0
            Signature = reader.ReadNullPaddedUTF8(18);

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

        //This type is made up
        public struct ModuleIndex
        {
            //https://github.com/dotnet/runtime/blob/511d26611c051c56e546404ea616c220cc78817c/eng/native/genmoduleindex.cmd#L4

            public byte Size { get; }

            public uint TimeStamp { get; }

            public int ImageSize { get; }

            public byte[] Extra { get; }

            internal ModuleIndex(IFileReader reader)
            {
                Size = reader.ReadByte();
                TimeStamp = reader.ReadUInt32();
                ImageSize = reader.ReadInt32();

                //The module index is 24 bytes. Read the remaining bytes (currently unused)
                Extra = reader.ReadBytes(15);
            }
        }
    }
}
