namespace PESpy
{
    public static partial class Bundle
    {
        //Bundle File Info Flags
        public enum file_type_t : byte
        {
            unknown,
            assembly,
            native_binary,
            deps_json,
            runtime_config_json,
            symbols,
        }
    }
}
