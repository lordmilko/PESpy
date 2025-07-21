namespace PESpy
{
    public static partial class Demangler
    {
        enum NameBackRefBehavior : byte
        {
            None = 0,          // don't save any names as backrefs.
            Template = 1 << 0, // save template instanations.
            Simple = 1 << 1,   // save simple names.
        }
    }
}
