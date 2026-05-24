using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace PESpy.PowerShell.PE
{
    [Cmdlet(VerbsCommon.Get, "PEExport")]
    public class GetPEExport : FileCmdlet<PEFile>
    {
        [Parameter(Mandatory = false, Position = 0)]
        public string Name { get; set; }

        [Parameter(Mandatory = false)]
        public ushort Ordinal { get; set; }

        protected override void ProcessRecordEx()
        {
            var exportTable = File.ExportTable;

            if (exportTable == null)
                return;

            if (HasParameter(nameof(Ordinal)))
            {
                //Fast path for the specified ordinal

                if (!exportTable.TryGetExport(Ordinal, out var export))
                    return;

                if (Name != null)
                {
                    var nameMatcher = NameMatcher.Create(Name);

                    if (nameMatcher.IsMatch(export.Name))
                        WriteObject(export);
                }
                else
                    WriteObject(export);
            }
            else
            {
                IEnumerable<ImageExportDirectory.Export> exports = exportTable.Exports;

                if (Name != null)
                {
                    var nameMatcher = NameMatcher.Create(Name);

                    exports = exports.Where(v => nameMatcher.IsMatch((SymString) v.Name));
                }

                foreach (var export in exports)
                    WriteObject(export);
            }
        }
    }
}
