using PESpy.Native;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy
{
    /// <summary>
    /// Represents the <see cref="IMAGE_RESOURCE_DIR_STRING_U"/> structure that describes the name of an <see cref="IMAGE_RESOURCE_DIRECTORY_ENTRY"/>.
    /// </summary>
    public class ImageResourceDirStringU : IValue, IViewable //This is a class so that it can be null without needing to use Nullable<T>
    {
        public short Length { get; init; }

        public string NameString { get; init; }

        public RawOffset Offset { get; }

        internal ImageResourceDirStringU(ref FileReader reader)
        {
            Offset = (RawOffset) reader.Position;

            Length = reader.ReadInt16();

            NameString = reader.ReadUnicodeString(Length);
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct(nameof(IMAGE_RESOURCE_DIR_STRING_U), this, ViewKind.ImageResourceDirStringU);

            s.WriteField(nameof(Length), Length);
            s.WriteUTF16Field(nameof(NameString), NameString, Length);
        }

        public override string ToString()
        {
            return NameString;
        }
    }
}
