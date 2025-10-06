using System;

namespace PESpy.PDB
{
    public class HcSzo : HashClass<int>
    {
        //The way that this hasher is meant to work is that the Buffer is registered as a context,
        //and then this gets passed to SZO.Equals which looks up the offset of the string in the buffer
        //to see if the input SZO matches

        public static readonly HcSzo Instance = new();

        public override bool Equals(int d1, int d2)
        {
            throw new NotImplementedException();
        }

        public override uint GetHashableValue(int key)
        {
            throw new NotImplementedException();
        }
    }
}
