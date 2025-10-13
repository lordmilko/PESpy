using System.Diagnostics;
using PESpy.View;

namespace PESpy
{
    public static partial class Bundle
    {
        public readonly struct Location : IValue, IViewable
        {
            public long Offset => chunk.PeekInt64(0);
            public long Size => chunk.PeekInt64(8);

            int IValue.Offset => chunk.AbsoluteOffset;

            internal const int StructSize =
                sizeof(long) + //Offset
                sizeof(long);  //Size

            private readonly MemoryChunk chunk;

            internal Location(in MemoryChunk chunk)
            {
                this.chunk = chunk;
            }

            void IViewable.WriteGlobals(ViewWriter writer)
            {
                //No globals
            }

            IView? IViewable.WriteStruct(ViewWriter writer) =>
                writer.NewStruct(Strings.location_t, this, ViewKind.BundleLocation, StructSize);

            IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
            {
                using var s = viewWriter.CreateStruct(parent);

                s.WriteField("offset", Offset);
                s.WriteField("size", Size);

                Debug.Assert(parent.Size == s.Size, "Size was not correct");
                return s.ToArray();
            }
        }
    }
}
