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
            public int MajorVersion => chunk.PeekInt32(0);

            public int MinorVersion => chunk.PeekInt32(4);

            public int NumEmbeddedFiles => chunk.PeekInt32(8);

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

            IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
            {
                using var s = viewWriter.CreateStruct(parent);

                s.WriteField("major_version", MajorVersion);
                s.WriteField("minor_version", MinorVersion);
                s.WriteField("num_embedded_files", NumEmbeddedFiles);

                Debug.Assert(parent.Size == s.Size, "Size was not correct");
                return s.ToArray();
            }
        }
    }
}
