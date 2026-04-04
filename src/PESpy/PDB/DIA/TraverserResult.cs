namespace PESpy.PDB
{
    internal struct TraverserResult
    {
        public OffSegSym offSegSym;
        public bool hasName;
        public ushort imod;

        public override string ToString()
        {
            return offSegSym.ToString();
        }
    }
}
