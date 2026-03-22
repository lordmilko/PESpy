namespace PESpy.Ecma335
{
    public readonly struct PropertyAccessors
    {
        //Apparently the JIT doesn't generate good code if we use nested structs here?

        private readonly int _getter;
        private readonly int _setter;

        public MethodDefIndex Getter => (MethodDefIndex) _getter;

        public MethodDefIndex Setter => (MethodDefIndex) _setter;


        public MethodDefIndex[] Others { get; }

        public PropertyAccessors(int getter, int setter, MethodDefIndex[] others)
        {
            _getter = getter;
            _setter = setter;
            Others = others;
        }
    }
}
