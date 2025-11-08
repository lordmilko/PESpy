namespace PESpy
{
    static class HasherV2
    {
        public static unsafe uint lhashPbCb(byte* data, int length, uint modulo)
        {
            uint hash = 0xb170a1bf;

            // Hash 4 characters/one ULONG at a time. 
            while (length >= 4)
            {
                length -= 4;
                hash += *(uint*) data;
                hash += (hash << 10);
                hash ^= (hash >> 6);
                data += 4;
            }

            // Hash the rest 1 by 1. 
            while (length > 0)
            {
                length -= 1;
                hash += *data;
                hash += (hash << 10);
                hash ^= (hash >> 6);
                data += 1;
            }

            return HashULONG(hash) % modulo;
        }

        private static uint HashULONG(uint u)
        {
            // From Numerical Recipes in C, second edition, pg 284. 
            return u * 1664525 + 1013904223;
        }
    }
}
