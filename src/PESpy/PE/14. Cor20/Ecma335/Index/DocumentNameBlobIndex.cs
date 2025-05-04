namespace PESpy.Ecma335
{
    public readonly struct DocumentNameBlobIndex
    {
        public readonly int Offset;

        private readonly BlobHeap? blobHeap;

        internal DocumentNameBlobIndex(int offset, BlobHeap? blobHeap)
        {
            Offset = offset;
            this.blobHeap = blobHeap;
        }

        public static explicit operator DocumentNameBlobIndex(int value) => new DocumentNameBlobIndex(value, default);

        //Can't use implicit operator here, as for some reason this has a backwards effect of allowing other indices to be passed to our tables, due to the presence of a general purpose int indexer
        public static explicit operator int(DocumentNameBlobIndex value) => value.Offset;

        public override string ToString()
        {
            if (blobHeap != null)
                return $"\"{blobHeap.GetDocumentName(Offset)}\"";

            return Offset.ToString();
        }
    }
}
