using System;
using System.Management.Automation;

namespace PESpy.PowerShell.View
{
    [Cmdlet(VerbsCommon.Show, "FileView")]
    internal class ShowFileView : FileCmdlet
    {
        //Pretty print a tree showing the hierarchy of the file

        protected override void ProcessRecord()
        {
            throw new NotImplementedException();
        }
    }
}
