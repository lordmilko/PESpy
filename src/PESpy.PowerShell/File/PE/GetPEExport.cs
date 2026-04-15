using System.Management.Automation;

namespace PESpy.PowerShell.PE
{
    [Cmdlet(VerbsCommon.Get, "PEExport")]
    public class GetPEExport : FileCmdlet<PEFile>
    {
        protected override void ProcessRecordEx()
        {
            var exports = File.ExportTable?.Exports;

            if (exports != null)
            {
                foreach (var export in exports)
                    WriteObject(export);
            }
        }
    }
}
