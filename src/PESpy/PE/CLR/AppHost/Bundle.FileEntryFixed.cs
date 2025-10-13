using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy
{
    public static partial class Bundle
    {
        //file_entry_fixed_t
        public readonly struct FileEntryFixed : IValue, IViewable
        {
            public long Offset => chunk.PeekInt64(0);

            public long Size => chunk.PeekInt64(8);

            public long CompressedSize => hasCompressedSize ? chunk.PeekInt64(16) : 0;

            public file_type_t Type => (file_type_t) chunk.PeekByte(hasCompressedSize ? 24 : 16);

            int IValue.Offset => chunk.AbsoluteOffset;

            internal const int FixedStructSize =
                sizeof(long) + //Offset
                sizeof(long) + //Size
                //CompressedSize is optional
                sizeof(byte);  //Type

            public int StructSize => FixedStructSize + (hasCompressedSize ? 8 : 0);

            private readonly MemoryChunk chunk;
            private readonly bool hasCompressedSize;

            internal FileEntryFixed(in MemoryChunk chunk, bool hasCompressedSize)
            {
                this.chunk = chunk;
                this.hasCompressedSize = hasCompressedSize;
            }

            void IViewable.WriteGlobals(ViewWriter writer)
            {
                //No globals
            }

            IView? IViewable.WriteStruct(ViewWriter writer) =>
                writer.NewStruct(Strings.file_entry_fixed_t, this, ViewKind.BundleFileEntryFixed, StructSize);

            IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
            {
                using var s = viewWriter.CreateStruct(parent);

                s.WriteField("offset", Offset);
                s.WriteField("size", Size);

                if (hasCompressedSize)
                    s.WriteField("compressedSize", CompressedSize);

                s.WriteField("type", Type, sizeof(byte));

                Debug.Assert(parent.Size == s.Size, "Size was not correct");
                return s.ToArray();
            }
        }
    }    
}
