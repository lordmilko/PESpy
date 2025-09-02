namespace PESpy.PDB
{
    //Name made up
    public readonly struct PDBSourceFile
    {
        public AnsiString Name { get; }

        //Either a RawString<FixedAnsiString> or SrcFormat containing the contents of the file.
        //Note that not all SrcFormat files actually contain the full source, they may just
        //contain summary information
        public IValue Value { get; }

        internal PDBSourceFile(AnsiString name, IValue value)
        {
            Name = name;
            Value = value;
        }

        public override string ToString()
        {
            return Name.ToString();
        }
    }
}
