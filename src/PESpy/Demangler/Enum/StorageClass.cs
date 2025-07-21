namespace PESpy
{
    public static partial class Demangler
    {
        public enum StorageClass : byte
        {
            None,
            PrivateStatic,
            ProtectedStatic,
            PublicStatic,
            Global,
            FunctionLocalStatic,
        }
    }
}
