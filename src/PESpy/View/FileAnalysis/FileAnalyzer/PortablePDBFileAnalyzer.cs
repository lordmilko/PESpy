namespace PESpy.View
{
    internal class PortablePDBFileAnalyzer : FileAnalyzer
    {
        private readonly PortablePDBFile _portablePDBFile;

        internal PortablePDBFileAnalyzer(
            PortablePDBFileAccessor fileAccessor,
            in FileAnalyzerOptions options) : base(fileAccessor, options)
        {
            _portablePDBFile = fileAccessor.PortablePDBFile;
        }

        protected override ViewWriter CreateViewWriter()
        {
            //CreateViewWriter is called by the base ctor
            var portablePDBFile = (PortablePDBFile) _fileAccessor.File;

            return new ViewByteViewWriter(
                new PortablePDBViewWriterHelper(portablePDBFile),
                portablePDBFile.CreateByteViewProvider(_fileAccessor),
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
