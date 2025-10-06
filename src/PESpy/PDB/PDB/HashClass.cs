using System;

namespace PESpy.PDB
{
    public abstract class HashClass<D> where D : IEquatable<D>
    {
        public virtual bool Equals(D d1, D d2) => d1.Equals(d2);

        public abstract uint GetHashableValue(D key);
    }
}
