using System;

namespace PESpy.View
{
    internal class PDBFileAnalyzer : FileAnalyzer
    {
        private readonly PDBFile _pdbFile;

        internal PDBFileAnalyzer(PDBFileAccessor fileAccessor, IFileAnalyzerProgress? progress) : base(fileAccessor, null, progress)
        {
            _pdbFile = fileAccessor.PDBFile;
        }

        protected override ViewWriter CreateViewWriter() =>
                new PDBViewByteViewWriter(((PDBFileAccessor) _fileAccessor).PDBFile, _fileAccessor, this, ((PDBFileAccessor) _fileAccessor)._pageNumberToSIIndex);

        public override void Execute()
        {
            Log(FileAnalyzerProgressPhase.DiscoverGlobals);

            //Mark all data structures that our PDBFile knows about as being data
            ((IViewable) _pdbFile).WriteGlobals(_viewWriter);

            //We're not like PE Files where there might be random data structures we discovered;
            //in a PDB File, we should know how big everything is
            Finalize(expandUnknownData: false);
        }
    }
}
