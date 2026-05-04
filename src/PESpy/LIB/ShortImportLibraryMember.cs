using System;
using System.Collections.Generic;
using PESpy.View;

namespace PESpy.LIB
{
    public class ShortImportLibraryMember : IImportLibraryMember, IValue,IViewable
    {
        public AnsiString FileName { get; }

        public AnsiString SymbolName { get; }

        public ImageArchiveMemberHeader ArchiveHeader => new ImageArchiveMemberHeader(chunk);

        public bool IsLong => false;

        public ImportObjectHeader ImportHeader => new ImportObjectHeader(chunk.Slice(ImageArchiveMemberHeader.StructSize));

        public AnsiString ImportName => chunk.PeekAnsiNullTerminatedString(ImageArchiveMemberHeader.StructSize + ImportObjectHeader.StructSize);

        public AnsiString DllName => chunk.PeekAnsiNullTerminatedString(ImageArchiveMemberHeader.StructSize + ImportObjectHeader.StructSize + ImportName.Length + 1);

        public long Offset => chunk.AbsoluteOffset;

        private readonly MemoryChunk chunk;

        internal ShortImportLibraryMember(in MemoryChunk chunk, AnsiString fileName, Dictionary<int, AnsiString> symbolNameMap)
        {
            this.chunk = chunk;
            FileName = fileName;

            //May not exist
            if (symbolNameMap.TryGetValue((int) Offset, out var symbolName))
                SymbolName = symbolName;
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

        int IViewable.NumChildren() => throw new NotSupportedException();

        void IViewable.WriteChild(int index, ref StructWriter structWriter) => throw new NotSupportedException();

        public override string ToString()
        {
            if (SymbolName.Length == 0)
                return $"{DllName}!{ImportName}";

            return $"{DllName}!{ImportName} -> {SymbolName}";
        }
    }
}
