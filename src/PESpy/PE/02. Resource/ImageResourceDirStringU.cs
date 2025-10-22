using System;
using PESpy.Native;
using PESpy.View;

namespace PESpy
{
    /// <summary>
    /// Represents the <see cref="IMAGE_RESOURCE_DIR_STRING_U"/> structure that describes the name of an <see cref="IMAGE_RESOURCE_DIRECTORY_ENTRY"/>.
    /// </summary>
    public class ImageResourceDirStringU : IValue, IViewable //This is a class so that it can be null without needing to use Nullable<T>
    {
        private const int LengthOffset = 0;
        private const int NameStringOffset = 2;

        public short Length => chunk.PeekInt16(LengthOffset);

        public FixedUtf16String NameString => chunk.PeekUtf16FixedLength(NameStringOffset, Length);

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

        int IViewable.NumChildren() => 2;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(Length), LengthOffset, Length);
                    break;

                case 1:
                    structWriter.WriteUtf16FixedLengthField(nameof(NameString), NameStringOffset, NameString);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }

        public override string ToString()
        {
            return NameString.ToString();
        }
    }
}
