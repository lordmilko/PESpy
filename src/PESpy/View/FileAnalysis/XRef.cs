namespace PESpy.View
{
    public enum XRefKind
    {
        From,
        To
    }

    public readonly struct XRef
    {
        public int Self { get; }
        public int Other { get; }
        public XRefKind Kind { get; }

        public XRef(int self, int other, XRefKind kind)
        {
            Self = self;
            Other = other;
            Kind = kind;
        }
    }
}
