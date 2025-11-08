namespace PESpy
{
    static class Hasher
    {
        //V1
        public static unsafe uint lhashPbCb(byte* data, int length, uint modulo)
        {
            var numLeadingWords = length / 4;
            uint hash = 0;

            //The native version uses duff's device, which can't be done in C# because it requires being able to fall
            //through the cases in a switch statement
            for (var i = 0; i < numLeadingWords; i++)
            {
                hash ^= *(uint*) data;
                data += sizeof(int);
            }

            //If the length is 4, bits 1 and 2 aren't set. If it's 5, bit 1 is set, 6 bit 2 is set, 7 bits 1 and 2 are set

            if ((length & 2) != 0)
            {
                hash ^= *(ushort*) data;
                data += sizeof(ushort);
            }

            if ((length & 1) != 0)
            {
                hash ^= *data;
            }

            hash |= 0x20202020;
            hash ^= (hash >> 11);

            hash ^= (hash >> 16);

            return hash % modulo;
        }
    }
}
