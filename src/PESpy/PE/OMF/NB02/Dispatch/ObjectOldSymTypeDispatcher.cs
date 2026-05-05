namespace PESpy
{
    public sealed class ObjectOldSymTypeDispatcher : OldSymTypeDispatcher<object>
    {
        public static readonly ObjectOldSymTypeDispatcher Instance = new();

        protected override object OldSymType(OldSymType value) => value;

        protected override object BlkSymType(BlkSymType value) => value;

        protected override object ProcSymType(ProcSymType value) => value;

        protected override object BPSymType(BPSymType value) => value;

        protected override object LocSymType(LocSymType value) => value;

        protected override object LabSymType(LabSymType value) => value;

        protected override object WithSymType(WithSymType value) => value;

        protected override object RegSymType(RegSymType value) => value;

        protected override object ConSymType(ConSymType value) => value;

        protected override object TypeDefSymType(TypeDefSymType value) => value;

        protected override object ThunkSymType(ThunkSymType value) => value;

        protected override object CV4BlkSymType(CV4BlkSymType value) => value;

        protected override object CV4WithSymType(CV4WithSymType value) => value;

        protected override object CV4LabSymType(CV4LabSymType value) => value;
    }
}
