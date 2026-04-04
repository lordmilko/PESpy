using ClrDebug.PDB;

namespace PESpy
{
    static class Hasher
    {
        //Old. Name is made up
        public static unsafe ushort oldHashPbCb(TYPTYPE* typType)
        {
            /* In NT 4, PDB1 symbols are rehashed because the algorithm has changed. We can see the old
             * algorithm inside VC++ 152's C1XX3216.EXE in the functiona t address 0x17888
             * For the most part it's the same as the new style algorithm (using duff's device), with
             * the following key differences
             * 1. If the length is odd, it's made even
             * 2. Duff's device does add instead of xor
             * 3. There's no need to change everything to lowercase, since symbols don't contain strings
             */

            var length = typType->len + 2; //Include sizeof(len) in the length
            var data = (byte*) typType;

            uint hash = 0;

            if ((length & 2) != 0)
            {
                hash = *(ushort*) data;
                data += 2;
            }

            var numLeadingWords = length / 4;

            //The native version uses duff's device, which can't be done in C# because it requires being able to fall
            //through the cases in a switch statement
            for (var i = 0; i < numLeadingWords; i++)
            {
                hash += *(uint*) data;
                data += sizeof(int);
            }

            hash ^= (hash >> 11);
            hash ^= (hash >> 16);

            return (ushort) (hash % PDB.MsfStream.TPI.cchnV7);
        }

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
