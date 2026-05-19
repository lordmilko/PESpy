namespace PESpy.View
{
    internal class DataFileAccessor : FileAccessor
    {
        public DataFileAccessor(DBGFile dbgFile) : base(dbgFile, GetBitness(dbgFile.DebugHeader.Machine))
        {
            FileViewKind = ViewKind.DBGFile;
        }

        public DataFileAccessor(OMFFile omfFile) : base(omfFile, bitness: 16)
        {
            FileViewKind = ViewKind.OMFFile;
        }

        public DataFileAccessor(OMFDBGFile omfDbgFile) : base(omfDbgFile, bitness: 16)
        {
            FileViewKind = ViewKind.OMFDBGFile;
        }

        public DataFileAccessor(OMFLIBFile omfLibFile) : base(omfLibFile, bitness: 16)
        {
            FileViewKind = ViewKind.OMFLIBFile;
        }

        //All of the virtual methods on FileAccessor pertain to RVAs and virtual addresses,
        //which aren't applicable in OBJ Files. So the fact we also have code doesn't matter
        public DataFileAccessor(OBJFile objFile) : base(objFile, GetBitness(objFile))
        {
            FileViewKind = ViewKind.OBJFile;
        }

        public DataFileAccessor(PDB1File pdbFile) : base(pdbFile, bitness: 16)
        {
            FileViewKind = ViewKind.PDBFile;
        }

        public DataFileAccessor(SYMFile symFile) : base(symFile, bitness: 16)
        {
            FileViewKind = ViewKind.SYMFile;
        }

        private static int GetBitness(OBJFile objFile)
        {
            var anonObjectHeader = objFile.AnonObjectHeader;

            //The machine on the FileHeader will be 0xc13; the real machine is listed on the ANON_OBJECT_HEADER
            if (anonObjectHeader != null)
                return GetBitness(anonObjectHeader.Machine);

            return GetBitness(objFile.FileHeader.Machine);
        }
    }
}
