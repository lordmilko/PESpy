using System.Management.Automation;

namespace PESpy.PowerShell.PE
{
    [Cmdlet(VerbsCommon.Get, "PEFile")]
    [OutputType(typeof(PEFile), typeof(PEFileOverview))]
    public class GetPEFile : FileOverviewCmdlet<PEFile>
    {
        protected override object CreateOverview(ISymbolAccessor symbolAccessor) =>
            new PEFileOverview(File, symbolAccessor);
    }
}
