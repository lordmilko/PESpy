using System.Threading;

namespace PESpy.View
{
    internal class PDB1FileAnalyzer : FileAnalyzer
    {
        private readonly PDB1File _pdbFile;

        public PDB1FileAnalyzer(
            PDB1FileAccessor fileAccessor,
            IFileAnalyzerProgress? progress,
            bool trackXRefs,
            CancellationToken cancellationToken) : base(fileAccessor, progress, trackXRefs, cancellationToken, null, LocatorHttpPolicy.None)
        {
            _pdbFile = fileAccessor.PDBFile;
        }

        protected override ViewWriter CreateViewWriter()
        {
            //CreateViewWriter is called by the base ctor
            var pdbFile = (PDB1File) _fileAccessor.File;

            return new ViewByteViewWriter(
                new SimpleViewWriterHelper(pdbFile),
                pdbFile.CreateByteViewProvider(_fileAccessor),
                ViewMode.Default,
                _fileAccessor,
                null,
                this,
                LocatorHttpPolicy.None,
                _progress
            );
        }

        public override void Execute() => ExecuteData();
    }
}
