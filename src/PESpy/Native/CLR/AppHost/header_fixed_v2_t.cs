namespace PESpy.Native
{
    internal struct header_fixed_v2_t
    {
        public location_t deps_json_location;
        public location_t runtimeconfig_json_location;
        public Bundle.header_flags_t flags;
    }
}
