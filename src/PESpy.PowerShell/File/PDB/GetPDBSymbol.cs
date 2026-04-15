using System;
using System.Management.Automation;
using SymHelp.Symbols;
using PESpy.PDB;

namespace PESpy.PowerShell.PDB
{
    [Cmdlet(VerbsCommon.Get, "PDBSymbol")]
    [OutputType(typeof(SymType))]
    public class GetPDBSymbol : FileCmdlet<PDBFile>
    {
        [Parameter]
        public int RVA { get; set; }

        [Parameter(Mandatory = false, Position = 1)]
        public string[] Name { get; set; }

        [Parameter]
        public SwitchParameter TopLevel { get; set; }

        //Whether to undecorate/demangle symbols when searching by name
        [Parameter]
        [Alias("Undecorate")]
        internal SwitchParameter Demangle { get; set; }

        protected override void ProcessRecordEx()
        {
            if (HasParameter(nameof(RVA)))
            {
                if (File.TryGetSymbolByRVA(RVA, out var symType, out var displacement))
                {
                    var pso = new PSObject();
                    pso.Properties.Add(new PSNoteProperty("SymType", symType));
                    pso.Properties.Add(new PSNoteProperty("Displacement", displacement));

                    WriteObject(pso);
                }

                return;
            }

            if (Demangle)
            {
                if (Name == null)
                    throw new NotImplementedException(); //todo: illegal, need to specify some names to search demangled against

                throw new NotImplementedException();
            }

            //Don't use WildcardPattern; this will result in a lot of allocations (and arguably
            //also inefficient regex overhead) that together will really hurt performance on large PDBs.
            //Most wildcard patterns are fairly straightforward, so use a specialized NameMatcher instead

            if (Name != null)
            {
                //Special case having a single matcher for a faster inner loop
                if (Name.Length == 1)
                {
                    var nameMatcher = NameMatcher.Create(Name[0]);

                    foreach (var symType in File.EnumerateSymbols(TopLevel))
                    {
                        if (!symType.TryGetName(out var name, File))
                            continue;

                        if (nameMatcher.IsMatch(name))
                            WriteObject(symType);
                    }
                }
                else
                {
                    var nameMatchers = new NameMatcher[Name.Length];

                    for (var i = 0; i < Name.Length; i++)
                        nameMatchers[i] = NameMatcher.Create(Name[i]);

                    foreach (var symType in File.EnumerateSymbols(TopLevel))
                    {
                        if (!symType.TryGetName(out var name, File))
                            continue;

                        for (var i =0; i < nameMatchers.Length; i++)
                        {
                            var nameMatcher = nameMatchers[i];

                            if (nameMatcher.IsMatch(name))
                            {
                                WriteObject(symType);
                                break;
                            }
                        }
                    }
                }
            }
            else
            {
                //Just emit everything

                foreach (var symType in File.EnumerateSymbols(TopLevel))
                {
                    WriteObject(symType);
                }
            }
        }
    }
}
