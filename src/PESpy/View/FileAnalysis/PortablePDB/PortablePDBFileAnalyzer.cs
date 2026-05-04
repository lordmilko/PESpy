using System;
using System.Threading;

namespace PESpy.View
{
    internal class PortablePDBFileAnalyzer : FileAnalyzer
    {
        private readonly PortablePDBFile _portablePDBFile;

        internal PortablePDBFileAnalyzer(
            PortablePDBFileAccessor fileAccessor,
            IFileAnalyzerProgress? progress,
            bool trackXRefs,
            CancellationToken cancellationToken) : base(fileAccessor, progress, trackXRefs, cancellationToken, null, LocatorHttpPolicy.None)
        {
            _portablePDBFile = fileAccessor.PortablePDBFile;
        }

        protected override ViewWriter CreateViewWriter()
        {
            //CreateViewWriter is called by the base ctor
            var portablePDBFile = (PortablePDBFile) _fileAccessor.File;

            return new ViewByteViewWriter(
                new SimpleViewWriterHelper(portablePDBFile),
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
