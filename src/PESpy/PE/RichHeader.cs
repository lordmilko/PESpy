using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using PESpy.Native;
using PESpy.View;

namespace PESpy
{
    /// <summary>
    /// Represents the undocumented Rich Header structure. This type does not have a well-known native struct declaration.
    /// </summary>
    public class RichHeader : IValue, IViewable //May be null, so can't be a struct
    {
        private const int DanSOffset = 0;
        private const int Padding1Offset = 4;
        private const int Padding2Offset = 8;
        private const int Padding3Offset = 12;
        private int RichOffset => 16 + (Items.Length * PRODITEM.StructSize);
        private int XorKeyOffset => 20 + (Items.Length * PRODITEM.StructSize);

        internal static unsafe RichHeader? New(int ntHeaderOffset, HeaderMemoryBlock headerBlock)
        {
            //https://www.virusbulletin.com/virusbulletin/2020/01/vb2019-paper-rich-headers-leveraging-mysterious-artifact-pe-format/

            /* The rich header, if it exists, lives between the DOS and NT header, and ends in the unencrypted word "Rich"
             * followed by a XOR key that can be used to decrypt the previous bytes of the header, 4 bytes at a time.
             * The beginning of the header is demarcated by the word "DanS" (which must be decrypted with the XOR key) */

            var start = ImageDosHeader.StructSize;

            var toRead = (int) (ntHeaderOffset - start);

            var remoteBytes = new MemoryChunk(headerBlock, start).PeekSpan<byte>(0, toRead);

            Span<byte> span;
            IntPtr alloc = default;

            //We need to mutate the bytes as we decode them, so we need to take a copy.
            //We would normally expect the Rich Header to lie within the header region
            //which should be 0x1000 bytes, but a bogus NT Header Offset could cause issues,
            //so allocate a buffer on the heap in the event there's too much data
            if (toRead < 0x1000)
            {
                var bytes = stackalloc byte[toRead];
                span = new Span<byte>(bytes, toRead);
                remoteBytes.CopyTo(span);
            }
            else
            {
                alloc = Marshal.AllocHGlobal(toRead);
                span = new Span<byte>((void*) alloc, toRead);
                remoteBytes.CopyTo(span);
            }

            try
            {
                if (!TryFindRich(span, out var richPosition))
                    return null;

                if (!TryFindDanS(span, richPosition, out var dansPosition))
                    return null;

                var richHeaderSize = richPosition - dansPosition;

                return new RichHeader(span, start, dansPosition, richHeaderSize);
            }
            finally
            {
                if (alloc != default)
                    Marshal.FreeHGlobal(alloc);
            }

        }

        private static bool TryFindRich(Span<byte> bytes, out int richPosition)
        {
            for (var i = 0; i < bytes.Length - 4; i++)
            {
                //"Rich"
                if (bytes[i] == 0x52 && bytes[i + 1] == 0x69 && bytes[i + 2] == 0x63 && bytes[i + 3] == 0x68)
                {
                    richPosition = i;
                    return true;
                }
            }

            richPosition = default;
            return false;
        }

        private static unsafe bool TryFindDanS(Span<byte> bytes, int richPosition, out int dansPosition)
        {
            //Skip over "Rich"
            var xorKeyStart = richPosition + 4;

            var xorKey = stackalloc byte[4];
            bytes.Slice(xorKeyStart, 4).CopyTo(new Span<byte>(xorKey, 4));

            var position = richPosition;

            while (position > 0)
            {
                position -= 4;

                for (var i = 0; i < 4; i++)
                    bytes[position + i] = (byte)(bytes[position + i] ^ xorKey[i]);

                //"DanS"
                if (bytes[position] == 0x44 && bytes[position + 1] == 0x61 && bytes[position + 2] == 0x6e && bytes[position + 3] == 0x53)
                {
                    dansPosition = position;
                    return true;
                }
            }

            dansPosition = default;
            return false;
        }

        public int DanS { get; }

        public int Padding1 { get; }

        public int Padding2 { get; }

        public int Padding3 { get; }

        public ProdItem[] Items { get; }

        public int Rich { get; }

        public int XorKey { get; }

        public int Offset { get; }

        internal const int FixedStructSize =
            sizeof(int) + //DanS
            sizeof(int) + //Padding1
            sizeof(int) + //Padding2
            sizeof(int) + //Padding3
            sizeof(int) + //Rich
            sizeof(int); //XorKey

        private RichHeader(Span<byte> bytes, int start, int bufferPos, int length)
        {
            //"start" stores the start offset of the bytes after the DOS Stub, and bufferPos initially stores the address of
            //the start of the RichHeader section within that buffer
            Offset = start + bufferPos;

            //At the start of the rich header is 12 padding bytes, all 0

            DanS = MemoryMarshal.Read<int>(bytes.Slice(bufferPos));
            bufferPos += 4;

            Padding1 = MemoryMarshal.Read<int>(bytes.Slice(bufferPos));
            bufferPos += 4;

            Padding2 = MemoryMarshal.Read<int>(bytes.Slice(bufferPos));
            bufferPos += 4;

            Padding3 = MemoryMarshal.Read<int>(bytes.Slice(bufferPos));
            bufferPos += 4;

            //DanS + 3x padding bytes
            const int prolog = 16;

            var numRecords = (length - prolog) / PRODITEM.StructSize;

            var items = new ProdItem[numRecords];

            for (var i = 0; i < numRecords; i++)
            {
                items[i] = new ProdItem(start, bufferPos, bytes);

                bufferPos += PRODITEM.StructSize;
            }

            Items = items;

            Rich = MemoryMarshal.Read<int>(bytes.Slice(bufferPos));
            bufferPos += 4;

            XorKey = MemoryMarshal.Read<int>(bytes.Slice(bufferPos));
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.RichHeader, this, ViewKind.RichHeader, FixedStructSize + (Items.Length * ProdItem.StructSize));

        int IViewable.NumChildren() => 6 + Items.Length;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(DanS), DanSOffset, DanS);
                    break;

                case 1:
                    structWriter.WriteField(nameof(Padding1), Padding1Offset, Padding1);
                    break;

                case 2:
                    structWriter.WriteField(nameof(Padding2), Padding2Offset, Padding2);
                    break;

                case 3:
                    structWriter.WriteField(nameof(Padding3), Padding3Offset, Padding3);
                    break;

                default:
                    var i = index - 4;

                    if (i < Items.Length)
                    {
                        structWriter.WriteInline(Items[i]);
                    }
                    else
                    {
                        i -= Items.Length;

                        switch (i)
                        {
                            case 0:
                                structWriter.WriteField(nameof(Rich), RichOffset, Rich);
                                break;

                            case 1:
                                structWriter.WriteField(nameof(XorKey), XorKeyOffset, XorKey);
                                break;

                            default:
                                throw new IndexOutOfRangeException();
                        }
                    }
                    break;
            }
        }
    }
}
