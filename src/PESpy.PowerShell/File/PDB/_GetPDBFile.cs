using System.Management.Automation;

namespace PESpy.PowerShell.PDB
{
    [Cmdlet(VerbsCommon.Get, "PDBFile")]
    public class GetPDBFile : FileOverviewCmdlet<PDBFile>
    {
        protected override object CreateOverview(ISymbolAccessor symbolAccessor) =>
            new PDBFileOverview(File, symbolAccessor);
    }
}
