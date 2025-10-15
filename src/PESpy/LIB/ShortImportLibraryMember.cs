using System;
using PESpy.View;

namespace PESpy.LIB
{
    public class ShortImportLibraryMember : IImportLibraryMember, IValue,IViewable
    {
        public AnsiString Name { get; }

        public ImageArchiveMemberHeader ArchiveHeader => new ImageArchiveMemberHeader(chunk);

        public ImportObjectHeader ImportHeader => new ImportObjectHeader(chunk.Slice(ImageArchiveMemberHeader.StructSize));

        public AnsiString ImportName => chunk.PeekAnsiNullTerminatedString(ImageArchiveMemberHeader.StructSize + ImportObjectHeader.StructSize);

        public AnsiString DllName => chunk.PeekAnsiNullTerminatedString(ImageArchiveMemberHeader.StructSize + ImportObjectHeader.StructSize + ImportName.Length + 1);

        public int Offset => chunk.AbsoluteOffset;

        private readonly MemoryChunk chunk;

        internal ShortImportLibraryMember(in MemoryChunk chunk, AnsiString name)
        {
            this.chunk = chunk;
            Name = name;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            writer.WriteGlobal(ArchiveHeader);
            writer.WriteGlobal(ImportHeader);

            var importNameOffset = Offset + ImageArchiveMemberHeader.StructSize + ImportObjectHeader.StructSize;
            var importName = ImportName;
            var importNameLength = importName.Length + 1;
            writer.WriteGlobal(importNameOffset, importName, importNameLength, ViewKind.ShortImportLibrary_ImportName);

            var dllName = DllName;
            writer.WriteGlobal(importNameOffset + importNameLength, dllName, dllName.Length + 1, ViewKind.ShortImportLibrary_DllName);
        }

        IView? IViewable.WriteStruct(ViewWriter writer) => null;

        int IViewable.NumChildren => throw new NotSupportedException();

        void IViewable.WriteChild(int index, ref StructWriter structWriter) => throw new NotSupportedException();

        public override string ToString()
        {
            return $"{DllName}!{ImportName}";
        }
    }
}
