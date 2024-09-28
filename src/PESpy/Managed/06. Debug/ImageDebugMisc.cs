using PESpy.Native;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy
{
    /// <summary>
    /// Represents the <see cref="IMAGE_DEBUG_MISC"/> structure.
    /// </summary>
    public class ImageDebugMisc : IValue, IViewable
    {
        public ImageDebugMiscType DataType { get; }

        public int Length { get; }

        public bool Unicode { get; }

        public byte[] Reserved { get; }

        public string Data { get; }

        public RawOffset Offset { get; }

        internal const int FixedStructSize =
            sizeof(int) +  //DataType
            sizeof(int) +  //Length
            sizeof(byte) + //Unicode
            3;             //Reserved

        public ImageDebugMisc(ref FileReader reader)
        {
            Offset = (RawOffset) reader.Position;

            reader.FillBuffer(FixedStructSize);

            DataType = (ImageDebugMiscType) reader.ReadInt32();
            Length = reader.ReadInt32();
            Unicode = reader.ReadByte() != 0;
            Reserved = reader.ReadArray<byte>(3);

            if (Unicode)
                Data = reader.ReadUTF16NullTerminatedString();
            else
                Data = reader.ReadAnsiNullTerminatedString();
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct(nameof(IMAGE_DEBUG_MISC), this, ViewKind.ImageDebugMisc);

            s.WriteField(nameof(DataType), DataType, sizeof(int));
            s.WriteField(nameof(Length), Length);
            s.WriteField(nameof(Unicode), (byte) (Unicode ? 1 : 0));
            s.WriteField(nameof(Reserved), Reserved);

            if (Unicode)
                s.WriteUTF16NullTerminatedField(nameof(Data), Data);
            else
                s.WriteAnsiNullTerminatedField(nameof(Data), Data);
        }
    }
}
