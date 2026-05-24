using System.Management.Automation;

namespace PESpy.PowerShell.PE
{
    [Cmdlet(VerbsCommon.Get, "PEFile")]
    [OutputType(typeof(PEFile), typeof(PEFileOverview))]
    public class GetPEFile : FileOverviewCmdlet<PEFile>
    {
        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, ParameterSetName = ParameterSet.FromPath)]
        [Alias("PSPath")] //ValueFromPipelineByPropertyName applies to this, and FileInfo objects have a PSPath
        public new string Path
        {
            get => base.Path;
            set => base.Path = value;
        }

        protected override object CreateOverview(ISymbolAccessor symbolAccessor) =>
            new PEFileOverview(File, symbolAccessor);
    }
}
