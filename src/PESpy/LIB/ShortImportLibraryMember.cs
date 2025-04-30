#if PEFAST
using PESpy.View;

namespace PESpy.LIB
{
    public class ShortImportLibraryMember : IImportLibraryMember, IValue,IViewable
    {
        public ImageArchiveMemberHeader ArchiveHeader => new ImageArchiveMemberHeader(chunk);

        public ImportObjectHeader ImportHeader => new ImportObjectHeader(chunk.Slice(ImageArchiveMemberHeader.StructSize));

        public AnsiString ImportName => chunk.PeekAnsiNullTerminatedString(ImageArchiveMemberHeader.StructSize + ImportObjectHeader.StructSize);

        public AnsiString DllName => chunk.PeekAnsiNullTerminatedString(ImageArchiveMemberHeader.StructSize + ImportObjectHeader.StructSize + ImportName.Length + 1);

        public int Offset => chunk.AbsoluteOffset;

        private readonly MemoryChunk chunk;

        internal ShortImportLibraryMember(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct("Import Library Member (Short)", this, ViewKind.ShortImportLibraryMember);

            s.WriteInline(ArchiveHeader);
            s.WriteInline(ImportHeader);
            s.WriteInlineAnsiNullTerminated(ImportName);
        }

        public override string ToString()
        {
            return $"{DllName}!{ImportName}";
        }
    }
}
#endif
