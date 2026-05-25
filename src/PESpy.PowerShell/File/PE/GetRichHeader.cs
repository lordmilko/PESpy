using System.Management.Automation;

namespace PESpy.PowerShell.PE
{
    [Cmdlet(VerbsCommon.Get, "RichHeader")]
    public class GetRichHeader : FileCmdlet<PEFile>
    {
        protected override void ProcessRecordEx()
        {
            var richHeader = File.RichHeader;

            if (richHeader != null)
            {
                foreach (var item in richHeader.Items)
                    WriteObject(item);
            }
        }
    }
}
