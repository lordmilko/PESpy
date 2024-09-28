using System;
using System.Collections.Generic;

namespace PESpy.View
{
    public partial class ViewWriter
    {
        internal ref struct BitFieldWriter
        {
            private List<IView> fields;
            private int offset;
            private int bitsUsed;
            private int maxSize;

            public BitFieldWriter(int offset, List<IView> fields, int bytes)
            {
                this.offset = offset;
                this.fields = fields;
                bitsUsed = 0;
                maxSize = bytes * 8;
            }

            public void WriteField(string name, byte value, int bits) =>
                WriteFieldInternal(name, value, bits);

            public void WriteField(string name, short value, int bits) =>
                WriteFieldInternal(name, value, bits);

            public void WriteField(string name, int value, int bits) =>
                WriteFieldInternal(name, value, bits);

            public void WriteField(string name, bool value, int bits)
            {
                if (bits != 1) //Don't know if it could be possible to have a bool that occupies 2 bytes, so make the caller think about what the size of the field is instead of just assuming all bools are 1 byte
                    throw new InvalidOperationException($"Writing a bool that is supposed to occupy {bits} bits is not implemented");

                WriteFieldInternal(name, (byte) (value ? 1 : 0), bits);
            }

            public void WriteField<T>(string name, T value, int bits) where T : Enum =>
                WriteFieldInternal(name, value, bits);

            private void WriteFieldInternal<T>(string name, T value, int bits)
            {
                var element = new BitFieldView<T>(offset, name, value, bits);

                bitsUsed += bits;

                fields.Add(element);
            }

            public void Dispose()
            {
                if (bitsUsed != maxSize)
                    throw new InvalidOperationException($"Expected exactly {maxSize} bits to be written, however {bitsUsed} bits were written instead.");
            }
        }
    }
}
