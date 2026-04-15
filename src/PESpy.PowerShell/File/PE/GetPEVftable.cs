using System.Management.Automation;

namespace PESpy.PowerShell.PE
{
    [Cmdlet(VerbsCommon.Get, "PEVftable")]
    public class GetPEVftable : FileCmdlet<PEFile>
    {
        protected override void ProcessRecordEx()
        {
            var progress = new PowerShellLocatorProgress(this);

            var symbolAccessor = File.GetSymbolAccessor(progress: progress, cancellationToken: CancellationToken);

            var vftables = File.Vftables;

            if (vftables != null)
            {
                foreach (var vftable in vftables)
                    WriteObject(vftable);
            }
        }
    }
}
