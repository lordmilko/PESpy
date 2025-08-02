using System;
using PESpy.Native;
using PESpy.View;

namespace PESpy
{
    /// <summary>
    /// Represents the <see cref="IMAGE_DEBUG_MISC"/> structure.
    /// </summary>
    public class ImageDebugMisc : IValue, IViewable //It will always be boxed
    {
        public ImageDebugMiscType DataType => (ImageDebugMiscType) chunk.PeekUInt32(0);

        public int Length => chunk.PeekInt32(4);

        public bool Unicode => chunk.PeekByte(8) != 0;

        public NativeSpan<byte> Reserved => chunk.PeekNativeSpan<byte>(9, 3);

        public NullTerminatedString Data => chunk.PeekNullTerminatedString(12, Unicode ? StringKind.UTF16 : StringKind.ANSI);

        public int Offset => chunk.AbsoluteOffset;

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
            writer.NewStruct(Strings.IMAGE_DEBUG_MISC, this, ViewKind.ImageDebugMisc, StructSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(DataType), DataType, sizeof(int));
            s.WriteField(nameof(Length), Length);
            s.WriteField(nameof(Unicode), (byte) (Unicode ? 1 : 0));
            s.WriteField(nameof(Reserved), Reserved);
            s.WriteNullTerminatedField(nameof(Data), Data);

            return s.ToArray();
        }

        public override string ToString()
        {
            return Data.ToString();
        }
    }
}
