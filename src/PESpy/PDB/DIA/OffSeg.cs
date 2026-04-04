namespace PESpy.PDB
{
    //Represents a lightweight tuple of an offset and associated section number.
    //This type is mad eup
#pragma warning disable CS0660 // Type defines operator == or operator != but does not override Object.Equals(object o)
#pragma warning disable CS0661 // Type defines operator == or operator != but does not override Object.GetHashCode()
    internal struct OffSeg
#pragma warning restore CS0661 // Type defines operator == or operator != but does not override Object.GetHashCode()
#pragma warning restore CS0660 //#pragma warning restore CS0660 // Type defines operator == or operator != but does not override Object.Equals(object o)
    {
        public ISECT seg;
        public int off;

        public OffSeg(int off, ISECT seg)
        {
            this.off = off;
            this.seg = seg;
        }

        public static bool operator ==(OffSeg left, OffSeg right) => left.off == right.off && left.seg == right.seg;

        public static bool operator !=(OffSeg left, OffSeg right) => !(left == right);
    }
}
