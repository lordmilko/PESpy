using System.Management.Automation;

namespace PESpy.PowerShell.PE
{
    [Cmdlet(VerbsCommon.Get, "PEImport")]
    public class GetPEImport : FileCmdlet<PEFile>
    {
        protected override void ProcessRecordEx()
        {
            var importTable = File.ImportTable;

            if (importTable == null)
                return;

            for (var i = 0; i < importTable.Length; i++)
            {
                ref var imageImportDescriptor = ref importTable[i];

                if (imageImportDescriptor.FirstThunk.IsValid)
                {
                    var thunks = imageImportDescriptor.FirstThunk.Value;

                    foreach (var thunk in thunks)
                        WriteObject(thunk);
                }
            }
        }
    }
}
