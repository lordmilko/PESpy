namespace PESpy.PDB
{
    internal struct OffSegSym
    {
        public ISECT seg;
        public int off;
        public SymType symType;

        public override string ToString()
        {
            return symType.ToString();
        }
    }
}
