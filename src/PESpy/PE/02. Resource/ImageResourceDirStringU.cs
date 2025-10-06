using System.Diagnostics;
using PESpy.Native;
using PESpy.View;

namespace PESpy
{
    /// <summary>
    /// Represents the <see cref="IMAGE_RESOURCE_DIR_STRING_U"/> structure that describes the name of an <see cref="IMAGE_RESOURCE_DIRECTORY_ENTRY"/>.
    /// </summary>
    public class ImageResourceDirStringU : IValue, IViewable //This is a class so that it can be null without needing to use Nullable<T>
    {
        public short Length => chunk.PeekInt16(0);

        public FixedUtf16String NameString => chunk.PeekUtf16FixedLength(2, Length);

        public int Offset => chunk.AbsoluteOffset;

        internal int StructSize =>
            sizeof(short) + //Length
            (Length * 2); //NameString

        private readonly MemoryChunk chunk;

        internal ImageResourceDirStringU(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.IMAGE_RESOURCE_DIR_STRING_U, this, ViewKind.ImageResourceDirStringU, StructSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(Length), Length);
            s.WriteUTF16Field(nameof(NameString), NameString, Length);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }

        public override string ToString()
        {
            return NameString.ToString();
        }
    }
}
