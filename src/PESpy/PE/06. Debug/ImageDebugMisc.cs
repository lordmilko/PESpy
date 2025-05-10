using System;
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
    public class ImageDebugMisc : IValue, IViewable //It will always be boxed
    {
#if PEFAST
        public ImageDebugMiscType DataType => (ImageDebugMiscType) chunk.PeekUInt32(0);
#else
        public ImageDebugMiscType DataType { get; }
#endif

#if PEFAST
        public int Length => chunk.PeekInt32(4);
#else
        public int Length { get; }
#endif

#if PEFAST
        public bool Unicode => chunk.PeekByte(8) != 0;
#else
        public bool Unicode { get; }
#endif

#if PEFAST
        public NativeSpan<byte> Reserved => chunk.PeekNativeSpan<byte>(9, 3);
#else
        public byte[] Reserved { get; }
#endif

        public string Data { get; }

#if PEFAST
        public RawOffset Offset => chunk.AbsoluteOffset;
#else
        public RawOffset Offset { get; }
#endif

        internal const int FixedStructSize =
            sizeof(int) +  //DataType
            sizeof(int) +  //Length
            sizeof(byte) + //Unicode
            3;             //Reserved

#if PEFAST
        private readonly MemoryChunk chunk;

        internal ImageDebugMisc(in MemoryChunk chunk)
        {
            this.chunk = chunk;

            if (Unicode)
                Data = chunk.PeekUtf16NullTerminatedString(FixedStructSize).ToString();
            else
                Data = chunk.PeekAnsiNullTerminatedString(FixedStructSize).ToString();
        }
#else
        internal ImageDebugMisc(IFileReader reader)
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
#endif

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

        public override string ToString()
        {
            return Data;
        }
    }
}
