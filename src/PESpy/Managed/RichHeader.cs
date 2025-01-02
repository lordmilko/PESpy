using System;
using System.Runtime.InteropServices;
using PESpy.Native;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy
{
    /// <summary>
    /// Represents the undocumented Rich Header structure. This type does not have a well-known native struct declaration.
    /// </summary>
    public class RichHeader : IValue, IViewable //May be null, so can't be a struct
    {
        internal static RichHeader? New(RawOffset ntHeaderOffset, IFileReader reader)
        {
            //https://www.virusbulletin.com/virusbulletin/2020/01/vb2019-paper-rich-headers-leveraging-mysterious-artifact-pe-format/

            /* The rich header, if it exists, lives between the DOS and NT header, and ends in the unencrypted word "Rich"
             * followed by a XOR key that can be used to decrypt the previous bytes of the header, 4 bytes at a time.
             * The beginning of the header is demarcated by the word "DanS" (which must be decrypted with the XOR key) */

            var start = (RawOffset) reader.Position;

            var toRead = (int) (ntHeaderOffset - start);
            var bytes = reader.ReadBytes(toRead);

            if (!TryFindRich(bytes, out var richPosition))
                return null;

            if (!TryFindDanS(bytes, richPosition, out var dansPosition))
                return null;

            var richHeaderSize = richPosition - dansPosition;

            return new RichHeader(bytes, start, dansPosition, richHeaderSize);
        }

        private static bool TryFindRich(byte[] bytes, out int richPosition)
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

        private static bool TryFindDanS(byte[] bytes, int richPosition, out int dansPosition)
        {
            //Skip over "Rich"
            var xorKeyStart = richPosition + 4;

            var xorKey = new byte[4];
            Array.Copy(bytes, xorKeyStart, xorKey, 0, 4);

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

        public RawOffset Offset { get; }

        private RichHeader(byte[] bytes, RawOffset start, int bufferPos, int length)
        {
            //"start" stores the start offset of the bytes after the DOS Stub, and bufferPos initially stores the address of
            //the start of the RichHeader section within that buffer
            Offset = start + bufferPos;

            //At the start of the rich header is 12 padding bytes, all 0

            DanS = BitConverter.ToInt32(bytes, bufferPos);
            bufferPos += 4;

            Padding1 = BitConverter.ToInt32(bytes, bufferPos);
            bufferPos += 4;

            Padding2 = BitConverter.ToInt32(bytes, bufferPos);
            bufferPos += 4;

            Padding3 = BitConverter.ToInt32(bytes, bufferPos);
            bufferPos += 4;

            //DanS + 3x padding bytes
            const int prolog = 16;

            var size = Marshal.SizeOf<PRODITEM>();
            var numRecords = (length - prolog) / size;

            var items = new ProdItem[numRecords];

            for (var i = 0; i < numRecords; i++)
            {
                items[i] = new ProdItem(start, bufferPos, bytes);

                bufferPos += size;
            }

            Items = items;

            Rich = BitConverter.ToInt32(bytes, bufferPos);
            bufferPos += 4;

            XorKey = BitConverter.ToInt32(bytes, bufferPos);
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct("Rich Header", this, ViewKind.RichHeader);

            s.WriteField(nameof(DanS), DanS);
            s.WriteField(nameof(Padding1), Padding1);
            s.WriteField(nameof(Padding2), Padding2);
            s.WriteField(nameof(Padding3), Padding3);
            s.WriteInline(Items);
            s.WriteField(nameof(Rich), Rich);
            s.WriteField(nameof(XorKey), XorKey);
        }
    }
}
