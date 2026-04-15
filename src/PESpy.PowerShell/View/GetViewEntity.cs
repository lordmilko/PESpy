using System;
using System.Management.Automation;

namespace PESpy.PowerShell.View
{
    [Cmdlet(VerbsCommon.Get, "ViewEntity")]
    internal class GetViewEntity : FileCmdlet
    {
        protected override void ProcessRecord()
        {
            throw new NotImplementedException();
        }
    }
}
