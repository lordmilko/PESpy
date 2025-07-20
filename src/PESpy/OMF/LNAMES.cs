using System.Collections.Generic;

namespace PESpy.OMF
{
    public readonly unsafe struct LNAMES
    {
        private readonly byte* value;

        public OMFRecordType RecordType => *(OMFRecordType*) value;

        public ushort RecordLength => *(ushort*) (value + 1);

        //Content
        public FixedAnsiString[] Names
        {
            get
            {
                var length = RecordLength;
                var end = value + length - 1;

                var ptr = value + 3;

                var results = new List<FixedAnsiString>();

                while (ptr < end)
                {
                    var strLen = *ptr;
                    var str = new FixedAnsiString(ptr + 1, strLen);

                    results.Add(str);

                    ptr += strLen + 1;
                }

                return results.ToArray();
            }
        }

        public byte Checksum => *(value + RecordLength + 2);

        public LNAMES(byte* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return RecordType.ToString();
        }
    }
}
