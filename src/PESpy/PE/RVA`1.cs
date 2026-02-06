using System;
using System.Diagnostics;

namespace PESpy
{
    /// <summary>
    /// Represents a value that is a reference to a value at another location.
    /// </summary>
    public interface IRVA : IValue
    {
        /// <summary>
        /// Gets the location of the value, as defined in the PE File.
        /// </summary>
        int ListedOffset { get; }

        /// <summary>
        /// Gets the actual location of the value, after adjusting for additional
        /// displacements and whether the PE File is loaded into memory or resides on disk.
        /// </summary>
        int ActualOffset { get; }

        bool IsValid { get; }
    }

    /// <summary>
    /// Encapsulates a Relative Virtual Address (RVA) and the value that it points to.
    /// </summary>
    /// <typeparam name="T">The type of value that the RVA points to.</typeparam>
    public readonly struct RVA<T> : IRVA
#if !NATIVEAOT
        , IEquatable<RVA<T>>
#endif
    {
        /// <summary>
        /// Gets the relative virtual address that the <see cref="Value"/> was listed as residing at in the bytes of the PE file.<para/>
        /// This value is the location that <see cref="Value"/> will be located at when loaded into memory. If the PE file that is being processed
        /// is being read from disk, this value is informational only, and <see cref="ActualOffset"/> contains the "real" location that the value was read from.
        /// </summary>
        public int ListedOffset { get; }

        /// <summary>
        /// Gets the actual offset that the <see cref="Value"/> resides at.<para/>, after adjusting the <see cref="ListedOffset"/> based on whether the PE file is being loaded from virtual memory or from disk.<para/>
        /// If the PE file resides in virtual memory, this value will be the same as <see cref="ListedOffset"/>.
        /// </summary>
        public int ActualOffset { get; }

        /// <summary>
        /// Gets whether the <see cref="ListedOffset"/> was successfully resolved to a readable address.<para/>
        /// If the field that this value represents is optional, in addition to not being valid, this value will also be <see cref="IsEmpty"/>.
        /// </summary>
        public bool IsValid { get; }

        /// <summary>
        /// Gets whether this object is empty, indicating that it represents an optional RVA that was not present.
        /// </summary>
        public bool IsEmpty => !IsValid && ListedOffset == 0;

        private readonly T? value;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T Value
        {
            get
            {
                if (!IsValid)
                    throw new InvalidOperationException($"Cannot get value from RVA 0x{ListedOffset:X}: address is not valid");

                return value!;
            }
        }

        public T? ValueOrDefault => value;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        int IValue.Offset => ActualOffset; //This is the position in the FileReader that the value came from

        public RVA(int listedOffset, int actualOffset, T value)
        {
            //Note: in unoptimized code it may show that a boxing occurs here for value types. I have tried different variations of "is object", "is null",
            //"is not", etc. They all box. But in optimized code this check will be removed
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            ListedOffset = listedOffset;
            ActualOffset = actualOffset;
            this.value = value;
            IsValid = true;
        }

        public RVA(int listedOffset)
        {
            ListedOffset = listedOffset;
            IsValid = false;
            ActualOffset = 0;
            this.value = default;
        }

#if !NATIVEAOT
        #region RVA == RVA

        public static bool operator ==(RVA<T> left, RVA<T> right)
        {
            if (!left.IsValid)
            {
                if (!right.IsValid)
                {
                    //They're both invalid
                    if (left.ListedOffset != right.ListedOffset)
                        return false;

                    return true;
                }
                else
                {
                    //Left is invalid, right is valid
                    return false;
                }
            }

            //They're both valid

            if (left.ActualOffset != right.ActualOffset)
                return false;

            if (left.ListedOffset != right.ListedOffset)
                return false;

            if (left.Value == null)
            {
                if (right.Value == null)
                    return true;

                return false;
            }

            return left.Value.Equals(right.Value);
        }

        public static bool operator !=(RVA<T> left, RVA<T> right) => !(left == right);

        #endregion
        #region RVA == T

        public static bool operator ==(RVA<T> left, T right)
        {
            if (!left.IsValid)
                return false;

            if (left.Value == null)
            {
                if (right == null)
                    return true;

                return false;
            }

            return left.Value.Equals(right);
        }

        public static bool operator !=(RVA<T> left, T right) => !(left == right);

        #endregion
        #region T == RVA

        public static bool operator ==(T left, RVA<T> right) => right == left;

        public static bool operator !=(T left, RVA<T> right) => right == left;

        #endregion

        public override bool Equals(object obj)
        {
            if (obj is T t)
                return this == t;

            if (obj is RVA<T> r)
                return this == r;

            return false;
        }

        public bool Equals(RVA<T> other) => this == other;

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + ActualOffset.GetHashCode();
                hash = hash * 31 + ListedOffset.GetHashCode();

                if (Value != null)
                    hash = hash * 31 + Value.GetHashCode();

                return hash;
            }
        }

        public override string ToString()
        {
            if (!IsValid)
            {
                if (ListedOffset == 0)
                    return "<empty>";

                return "0x" + ListedOffset.ToString("X") + " <bad>";
            }

            if (Value is string s)
                return s;

            if (typeof(T).IsArray)
                return "0x" + ListedOffset.ToString("X") + $" : (Length: {((Array) (object) Value!).Length})" ;

            if (Value != null)
            {
                if (Value is ulong ul)
                    return "0x" + ul.ToString("X");

                return Value.ToString();
            }

            return "0x" + ListedOffset.ToString("X") + " : " + typeof(T).Name;
        }
#endif
    }
}
