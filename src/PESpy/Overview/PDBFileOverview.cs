using System;
using ClrDebug.DIA;
using ClrDebug.PDB;
using PESpy.PDB;

namespace PESpy
{
    public class PDBFileOverview
    {
        public FileKind Kind => FileKind.PDB;

        public PDBFileKind PDBKind { get; }

        public int PageSize { get; }

        public PDBIMPV PDBImpv { get; }

        public DBIImpv DBIImpv { get; }

        public TPIImpv TPIImpv { get; }

        public TPIImpv IPIImpv { get; }

        public int Age { get; }

        public Guid Guid { get; }

        public Timestamp Signature { get; }

        public PdbFeature[] Features { get; }

        public bool HasStrippedFlag { get; }

        public bool IsStripped { get; }

        public bool HasSourceLink { get; }

        public bool HasSrcSrv { get; }

        public PDBFileOverview(PDBFile pdbFile,  ISymbolAccessor symbolAccessor)
        {
            PDBKind = pdbFile.PDBKind;

            PageSize = pdbFile.PageSize;

            var pdb = pdbFile.PDB;

            if (pdb != null)
            {
                var header = pdb.PDBHeader;

                PDBImpv = header.ImplementationVersion;

                Age = header.Age;
                Signature = header.Signature;

                if (header is PDBStream70 h)
                    Guid = h.Guid;

                Features = pdb.Features.ToArray();
            }

            var dbi = pdbFile.DBI;

            if (dbi != null)
            {
                if (dbi.DbiHdr is NewDBIHdr h)
                {
                    DBIImpv = h.verHdr;
                    HasStrippedFlag = h.flags.fStripped;
                }

                /* dbghelp!diaIsStrippedPdb considers us to be not stripped if we have a data symbol
                 * at global scope, or if we have line number info. Having a function at global scope
                 * Does not prove anything; it is normal to have function refs in globals, which in turn
                 * point to S_LPROC32 in modules. */
                IsStripped = !(HasPrivateSymbols(pdbFile) || HasLines(dbi));
            }

            var tpi = pdbFile.TPI;

            if (tpi != null)
            {
                if (tpi.Hdr is HDR_16t h16)
                    TPIImpv = h16.vers;
                else if (tpi.Hdr is HDR h)
                    TPIImpv = h.vers;
                else
                    throw new NotImplementedException();
            }

            var ipi = pdbFile.IPI;

            if (ipi != null )
            {
                if (tpi.Hdr is HDR_16t h16)
                    IPIImpv = h16.vers;
                else if (tpi.Hdr is HDR h)
                    IPIImpv = h.vers;
                else
                    throw new NotImplementedException();
            }

            HasSourceLink = pdbFile.SourceLink.Count > 0;
            HasSrcSrv = pdbFile.SrcSrv.Length > 0;
        }

        private bool HasPrivateSymbols(PDBFile pdbFile)
        {
            var gsiSymbols = pdbFile.GSI?.Symbols;

            if (gsiSymbols == null)
                return false;
            else
            {
                foreach (var symbol in gsiSymbols)
                {
                    if (symbol.GetSymTagEnum() == SymTagEnum.Data)
                        return true;
                }
            }

            return false;
        }

        private bool HasLines(MsfStream.DBI dbi)
        {
            var modules = dbi.Modules;

            if (modules == null)
                return false;

            foreach (var module in modules)
            {
                if (module.C11Lines != null)
                    return true;

                var c13 = module.C13Lines;

                if (c13 != null)
                {
                    foreach (var section in c13)
                    {
                        switch (section.Type)
                        {
                            case DEBUG_S_SUBSECTION_TYPE.DEBUG_S_LINES:
                            case DEBUG_S_SUBSECTION_TYPE.DEBUG_S_IL_LINES:
                            case DEBUG_S_SUBSECTION_TYPE.DEBUG_S_INLINEELINES: //Not sure if I should look at this one too
                                return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}
