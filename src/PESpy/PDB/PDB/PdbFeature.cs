using ClrDebug.PDB;

namespace PESpy.PDB
{
    //Name is made up
    public enum PdbFeature : uint
    {
        impvVC110 = PDBIMPV.PDBImpvVC110,
        impvVC140 = PDBIMPV.PDBImpvVC140,

        featNoTypeMerge = 0x4D544F4E,    // "NOTM"
        featMinimalDbgInfo = 0x494E494D,    // "MINI". Indicates that the file was compiled with /DEBUG:FASTLINK
    }
}
