using System.Management.Automation;

namespace PESpy.PowerShell
{
    public abstract class FileOverviewCmdlet<T> : FileCmdlet<T> where T : class, IFile
    {
        [Parameter]
        public SwitchParameter Overview { get; set; }

        protected override void ProcessRecordEx()
        {
            if (Overview)
            {
                var progress = new PowerShellLocatorProgress(this);

                var symbolAccessor = File.GetSymbolAccessor(progress: progress, cancellationToken: CancellationToken);

                var overview = CreateOverview(symbolAccessor);

                WriteObject(overview);
            }
            else
            {
                //Note that we have a custom TypeAdapter to strip properties that rely on symbols
                WriteObject(File);
            }
        }

        protected abstract object CreateOverview(ISymbolAccessor symbolAccessor);
    }
}
