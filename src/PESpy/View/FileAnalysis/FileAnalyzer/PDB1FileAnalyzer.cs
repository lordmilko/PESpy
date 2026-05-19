namespace PESpy.View
{
    internal class PDB1FileAnalyzer : FileAnalyzer
    {
        private readonly PDB1File _pdbFile;

        public PDB1FileAnalyzer(
            DataFileAccessor fileAccessor,
            in FileAnalyzerOptions options) : base(fileAccessor, options)
        {
            _pdbFile = (PDB1File) fileAccessor.File;
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
