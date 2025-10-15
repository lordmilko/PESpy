using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy
{
    public static partial class Bundle
    {
        //header_fixed_t

        /// <summary>
        /// Represents the header_fixed_t structure that is the first component
        /// in the .NET Core Bundle Manifest headers.<para/>
        /// https://github.com/dotnet/runtime/blob/8d3c4013b7e7c6d603b08a1fbf563d51e2c2e00c/src/native/corehost/bundle/header.h#L27
        /// </summary>
        public readonly struct HeaderFixed : IValue, IViewable
        {
            private const int MajorVersionOffset = 0;
            private const int MinorVersionOffset = 4;
            private const int NumEmbeddedFilesOffset = 8;

            public int MajorVersion => chunk.PeekInt32(MajorVersionOffset);

            public int MinorVersion => chunk.PeekInt32(MinorVersionOffset);

            public int NumEmbeddedFiles => chunk.PeekInt32(NumEmbeddedFilesOffset);

            public int Offset => chunk.AbsoluteOffset;

            internal const int StructSize =
                sizeof(int) + //MajorVersion
                sizeof(int) + //MinorVersion
                sizeof(int);  //NumEmbeddedFiles

            private readonly MemoryChunk chunk;

            internal HeaderFixed(in MemoryChunk chunk)
            {
                this.chunk = chunk;
            }

            void IViewable.WriteGlobals(ViewWriter writer)
            {
                //No globals
            }

            IView? IViewable.WriteStruct(ViewWriter writer) =>
                writer.NewStruct(Strings.header_fixed_t, this, ViewKind.BundleHeaderFixed, StructSize);

        int IViewable.NumChildren => 3;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField("major_version", MajorVersionOffset, MajorVersion);
                    break;

                case 1:
                    structWriter.WriteField("minor_version", MinorVersionOffset, MinorVersion);
                    break;

                case 2:
                    structWriter.WriteField("num_embedded_files", NumEmbeddedFilesOffset, NumEmbeddedFiles);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
        }
    }
}
