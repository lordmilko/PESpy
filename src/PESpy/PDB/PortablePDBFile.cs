using System;
using System.IO;
using PESpy.View;

namespace PESpy
{
    public class PortablePDBFile : IFile, IViewable, IDisposable //Not a PDBFile, as the format is not similar in any way what-so-ever
    {
        public static PortablePDBFile FromFile(string path)
        {
            using var fs = File.OpenRead(path);

            var mmf = new MemoryMappedFileHolder(fs);

            try
            {
                return new PortablePDBFile(fs.Name, mmf);
            }
            catch
            {
                mmf.Dispose();

                throw;
            }
        }

        /// <summary>
        /// Locates a file on the symbol server and opens it as a <see cref="PortablePDBFile"/>.
        /// </summary>
        /// <param name="symStoreKey">The <see cref="SymStoreKey"/> describing the file that should be located and opened.</param>
        /// <returns>A <see cref="PortablePDBFile"/> that provides access to the contents of the specified file.</returns>
        /// <exception cref="ArgumentException">The specified <see cref="SymStoreKey"/> cannot be opened as a <see cref="PortablePDBFile"/>.</exception>
        public static PortablePDBFile FromKey(SymStoreKey symStoreKey)
        {
            switch (symStoreKey.Kind)
            {
                case SymStoreKeyKind.PDB:
                case SymStoreKeyKind.PortablePDB:
                    var path = Locator.Locate(symStoreKey);

                    return FromFile(path);

                default:
                    throw new ArgumentException($"{nameof(SymStoreKey)} '{symStoreKey}' of type '{symStoreKey.Kind}' cannot be opened as a {nameof(PortablePDBFile)}");
            }
        }

        /// <inheritdoc/>
        public string? Name { get; private set; }

        /// <inheritdoc/>
        public string? FileName { get; private set; }

        /// <inheritdoc/>
        public FileKind Kind => FileKind.PortablePDB;

        public int Length => globalBlock.Length;

        public EcmaMetadata EcmaMetadata { get; }

        private MemoryMappedFileHolder mmf;
        private GlobalMemoryBlock globalBlock;

        private bool disposed;

        internal unsafe PortablePDBFile(string fileName, in MemoryMappedFileHolder mmf)
        {
            this.mmf = mmf;

            FileName = fileName;
            Name = Path.GetFileName(fileName);

            globalBlock = new GlobalMemoryBlock(mmf.Address, (int) mmf.Length, this);

            EcmaMetadata = new EcmaMetadata(new MemoryChunk(globalBlock, 0));
        }

        ~PortablePDBFile()
        {
            Dispose(false);
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            writer.WriteGlobal(EcmaMetadata);
        }

        IView? IViewable.WriteStruct(ViewWriter writer) => null;

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter) => throw new NotSupportedException();

        public void Dispose()
        {
            Dispose(true);
        }

        protected void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
                GC.SuppressFinalize(this);

            mmf.Dispose();

            disposed = true;
        }

        public override string ToString()
        {
            if (Name != null)
                return Name.ToString();

            return base.ToString();
        }
    }
}
