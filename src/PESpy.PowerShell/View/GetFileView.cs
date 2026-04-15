using System;
using System.Management.Automation;

namespace PESpy.PowerShell.View
{
    [Cmdlet(VerbsCommon.Get, "FileView")]
    internal class GetFileView : FileCmdlet
    {
        protected override void ProcessRecord()
        {
            throw new NotImplementedException();
        }
    }
}
