namespace PESpy.View
{
    internal class OMFFileAnalyzer : FileAnalyzer
    {
        private readonly OMFFile _omfFile;

        internal OMFFileAnalyzer(
            DataFileAccessor fileAccessor,
            in FileAnalyzerOptions options) : base(fileAccessor, options)
        {
            _omfFile = (OMFFile) fileAccessor.File;
        }

        protected override ViewWriter CreateViewWriter()
        {
            //CreateViewWriter is called by the base ctor
            var omfFile = (OMFFile) _fileAccessor.File;

            return new ViewByteViewWriter(
                new SimpleViewWriterHelper(omfFile),
                omfFile.CreateByteViewProvider(_fileAccessor),
                ViewMode.Default,
                _fileAccessor,
                null,
                this,
                LocatorHttpPolicy.None,
                _progress
            );
        }

        //Maybe LEDATA actually has code? Haven't explored this yet; but ostensibly yes OMF files should actually contain disassembly
        public override void Execute() => ExecuteData();
    }
}
