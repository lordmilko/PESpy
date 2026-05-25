using System.Management.Automation;

namespace PESpy.PowerShell.OBJ
{
    [Cmdlet(VerbsCommon.Get, "OBJFile")]
    [OutputType(typeof(OBJFile), typeof(OBJFileOverview))]
    public class GetOBJFile : FileOverviewCmdlet<OBJFile>
    {
        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, ParameterSetName = ParameterSet.FromPath)]
        [Alias("PSPath")] //ValueFromPipelineByPropertyName applies to this, and FileInfo objects have a PSPath
        public new string Path
        {
            get => base.Path;
            set => base.Path = value;
        }

        protected override object CreateOverview(ISymbolAccessor symbolAccessor) =>
            new OBJFileOverview(File, symbolAccessor);
    }
}
