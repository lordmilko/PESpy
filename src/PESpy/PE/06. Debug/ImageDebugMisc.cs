using System;
using System.Diagnostics;
using PESpy.Native;
using PESpy.View;

namespace PESpy
{
    /// <summary>
    /// Represents the <see cref="IMAGE_DEBUG_MISC"/> structure.
    /// </summary>
    public class ImageDebugMisc : IValue, IViewable //It will always be boxed
    {
        private const int DataTypeOffset = 0;
        private const int LengthOffset = 4;
        private const int UnicodeOffset = 8;
        private const int ReservedOffset = 9;
        private const int DataOffset = 12;

        public IMAGE_DEBUG_MISC_TYPE DataType => (IMAGE_DEBUG_MISC_TYPE) chunk.PeekUInt32(DataTypeOffset);

        public int Length => chunk.PeekInt32(LengthOffset);

        public bool Unicode => chunk.PeekByte(UnicodeOffset) != 0;

        public NativeSpan<byte> Reserved => chunk.PeekNativeSpan<byte>(ReservedOffset, 3);

        public NullTerminatedString Data => chunk.PeekNullTerminatedString(DataOffset, Unicode ? StringKind.UTF16 : StringKind.ANSI);

        public long Offset => chunk.AbsoluteOffset;

        internal const int FixedStructSize =
            sizeof(int) +  //DataType
            sizeof(int) +  //Length
            sizeof(byte) + //Unicode
            3;             //Reserved

        internal int StructSize =>
            FixedStructSize +
            (Unicode
                ? (Data.Length + 1) * 2
                : (Data.Length + 1));

        private readonly MemoryChunk chunk;

        internal ImageDebugMisc(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.ImageDebugMisc, StructSize);

        int IViewable.NumChildren() => 5;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(DataType), DataTypeOffset, DataType, sizeof(int));
                    break;

                case 1:
                    structWriter.WriteField(nameof(Length), LengthOffset, Length);
                    break;

                case 2:
                    structWriter.WriteField(nameof(Unicode), UnicodeOffset, (byte) (Unicode ? 1 : 0));
                    break;

                case 3:
                    structWriter.WriteField(nameof(Reserved), ReservedOffset, Reserved);
                    break;

                case 4:
                    structWriter.WriteNullTerminatedField(nameof(Data), DataOffset, Data);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }

        public override string ToString()
        {
            return Data.ToString();
        }
    }
}
