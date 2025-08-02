using System;
using System.Collections.Generic;

namespace PESpy.View
{
    public partial class ViewWriter
    {
        internal ref struct StructBitFieldWriter
        {
            private FixedUtf8String structName;
            private ViewKind kind;
            private List<IView> parentFields;
            private List<IView> fields;
            private int offset;
            private int bitsUsed;
            private int maxSize; //In bytes
            private ViewWriter viewWriter;

            internal StructBitFieldWriter(FixedUtf8String name, int offset, ViewKind kind, List<IView> parentFields, int bytes, ViewWriter viewWriter)
            {
                structName = name;
                this.offset = offset;
                this.kind = kind;
                this.parentFields = parentFields;
                this.viewWriter = viewWriter;
                this.fields = viewWriter.RentList();
                bitsUsed = 0;
                maxSize = bytes;
            }

            public void WriteField(string name, short value, int bits) =>
                WriteFieldInternal(name, value, bits);

            public void WriteField<T>(string name, T value, int bits) where T : Enum =>
                WriteFieldInternal(name, value, bits);

            private void WriteFieldInternal<T>(string name, T value, int bits)
            {
                var element = new BitFieldView<T>(offset, name, value, bits, maxSize);

                bitsUsed += bits;

                fields.Add(element);
            }

            public void Dispose()
            {
                if (bitsUsed != maxSize * 8)
                    throw new NotImplementedException();

                var structView = new StructView<object>(offset, structName, default, fields.ToArray(), maxSize, kind, viewWriter);

                parentFields.Add(structView);
                viewWriter.ReturnList(fields);
            }
        }
    }
}
