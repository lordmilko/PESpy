using System.Management.Automation;
using ClrDebug.PDB;
using PESpy.PDB;

namespace PESpy.PowerShell.PDB
{
    [Cmdlet(VerbsCommon.Get, "PDBType")]
    public class GetPDBType : FileCmdlet<PDBFile>
    {
        protected override void ProcessRecordEx()
        {
            var tpi = File.TPI;

            if (tpi != null)
            {
                foreach (var type in tpi.Types)
                {
                    WriteObject(type);

                    if (type.leaf == LEAF_ENUM_e.LF_FIELDLIST)
                    {
                        var fieldList = (LfFieldList) type;

                        foreach (var field in fieldList.EnumerateFields(File))
                            WriteObject(field);
                    }
                    else if (type.leaf == LEAF_ENUM_e.LF_FIELDLIST_16t)
                    {
                        var fieldList16t = (LfFieldList16t) type;

                        foreach (var field in fieldList16t.EnumerateFields(File))
                            WriteObject(field);
                    }
                }
            }

            var ipi = File.IPI;

            if (ipi != null)
            {
                foreach (var type in ipi.Types)
                    WriteObject(type);
            }
        }
    }
}
