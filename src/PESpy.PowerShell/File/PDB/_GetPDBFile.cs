using System.Management.Automation;

namespace PESpy.PowerShell.PDB
{
    [Cmdlet(VerbsCommon.Get, "PDBFile")]
    public class GetPDBFile : FileOverviewCmdlet<PDBFile>
    {
        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, ParameterSetName = ParameterSet.FromPath)]
        [Alias("PSPath")] //ValueFromPipelineByPropertyName applies to this, and FileInfo objects have a PSPath
        public new string Path
        {
            get => base.Path;
            set => base.Path = value;
        }

        protected override object CreateOverview(ISymbolAccessor symbolAccessor) =>
            new PDBFileOverview(File, symbolAccessor);
    }
}
