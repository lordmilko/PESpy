namespace PESpy.Native
{
    //ImageBDDDynamicRelocation
    internal struct IMAGE_BDD_DYNAMIC_RELOCATION
    {
        public short Left;                // Index of FALSE edge in BDD array
        public short Right;               // Index of TRUE edge in BDD array
        public int Value;               // Either FeatureNumber or Index into RVAs array
    }
}
