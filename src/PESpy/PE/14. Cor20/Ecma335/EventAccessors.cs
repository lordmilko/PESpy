namespace PESpy.Ecma335
{
    public readonly struct EventAccessors
    {
        //Apparently the JIT doesn't generate good code if we use nested structs here?

        private readonly int _adder;
        private readonly int _remover;
        private readonly int _fire;

        public MethodDefIndex Adder => (MethodDefIndex) _adder;

        public MethodDefIndex Remover => (MethodDefIndex) _remover;

        public MethodDefIndex Fire => (MethodDefIndex) _fire;

        public MethodDefIndex[] Others { get; }

        public EventAccessors(int adder, int remover, int fire, MethodDefIndex[] others)
        {
            _adder = adder;
            _remover = remover;
            _fire = fire;
            Others = others;
        }
    }
}
