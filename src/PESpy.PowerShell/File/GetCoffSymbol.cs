using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using PESpy.LIB;

namespace PESpy.PowerShell
{
    [Cmdlet(VerbsCommon.Get, "CoffSymbol")]
    public class GetCoffSymbol : FileCmdlet<IFile>
    {
        [Parameter(Mandatory = false)]
        public string Name { get; set; }

        protected override void ProcessRecordEx()
        {
            switch (File.Kind)
            {
                case FileKind.PE:
                    //I don't know if it's guaranteed that both the FileHeader and DebugTable
                    //may point to the Coff Symbols if they're present, so we'll try both
                    var peFile = (PEFile) File;

                    if (TryGetSymbolsFromFileHeader(peFile.FileHeader))
                        return;

                    GetSymbolsFromDebugTable(peFile.DebugTable);
                    break;

                case FileKind.DBG:
                    GetSymbolsFromDebugTable(((DBGFile) File).DebugTable);
                    break;

                case FileKind.OBJ:
                    TryGetSymbolsFromFileHeader(((OBJFile) File).FileHeader);
                    break;

                case FileKind.LIB:
                    //I suppose we get symbols for each nested OBJ file!
                    var libFile = (LIBFile) File;

                    foreach (var member in libFile.ImportLibrary)
                    {
                        if (member.IsLong)
                        {
                            var longMember = (LongImportLibraryMember) member;

                            TryGetSymbolsFromFileHeader(longMember.FileHeader);
                        }
                    }

                    break;
            }
        }

        private bool TryGetSymbolsFromFileHeader(ImageFileHeader fileHeader)
        {
            if (fileHeader.PointerToSymbolTable.TryGetValue(out var coffSymbolTable))
            {
                var symbols = coffSymbolTable.Symbols;

                WriteSymbols(symbols);

                return true;
            }

            return false;
        }

        private void GetSymbolsFromDebugTable(ImageDebugDirectory[] debugTable)
        {
            if (debugTable == null)
                return;

            for (var i = 0; i < debugTable.Length; i++)
            {
                ref var debugDirectory = ref debugTable[i];

                if (debugDirectory.Type == IMAGE_DEBUG_TYPE.IMAGE_DEBUG_TYPE_COFF)
                {
                    var coffSymbolTable = (CoffSymbolTable) debugDirectory.Data;

                    var symbols = coffSymbolTable.Symbols;

                    WriteSymbols(symbols);
                }
            }
        }

        private void WriteSymbols(IEnumerable<ImageSymbol> symbols)
        {
            if (Name != null)
            {
                var nameMatcher = NameMatcher.Create(Name);

                symbols = symbols.Where(v => nameMatcher.IsMatch(v.Name.Name));
            }

            foreach (var symbol in symbols)
                WriteObject(symbol);
        }
    }
}
