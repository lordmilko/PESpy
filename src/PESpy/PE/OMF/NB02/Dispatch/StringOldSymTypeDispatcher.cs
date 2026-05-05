namespace PESpy
{
    internal sealed class StringOldSymTypeDispatcher : OldSymTypeDispatcher<string>
    {
        public static readonly StringOldSymTypeDispatcher Instance = new();

        protected override string OldSymType(OldSymType value) => value.rectyp.ToString();

        protected override string BlkSymType(BlkSymType value) => value.ToString();

        protected override string ProcSymType(ProcSymType value) => value.ToString();

        protected override string BPSymType(BPSymType value) => value.ToString();

        protected override string LocSymType(LocSymType value) => value.ToString();

        protected override string LabSymType(LabSymType value) => value.ToString();

        protected override string WithSymType(WithSymType value) => value.ToString();

        protected override string RegSymType(RegSymType value) => value.ToString();

        protected override string ConSymType(ConSymType value) => value.ToString();

        protected override string TypeDefSymType(TypeDefSymType value) => value.ToString();

        protected override string ThunkSymType(ThunkSymType value) => value.ToString();

        protected override string CV4BlkSymType(CV4BlkSymType value) => value.ToString();

        protected override string CV4WithSymType(CV4WithSymType value) => value.ToString();

        protected override string CV4LabSymType(CV4LabSymType value) => value.ToString();
    }
}
