namespace PESpy.PDB
{
    public class HcNi : HashClass<NI>
    {
        public static readonly HcNi Instance = new();

        public override uint GetHashableValue(NI key)
        {
            //The value is simply converted to HASH (a short hash)
            return (ushort) (uint) key;
        }
    }
}
