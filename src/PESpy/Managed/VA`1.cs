using System;
using System.ComponentModel;
using System.Diagnostics;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy
{
    [EditorBrowsable(EditorBrowsableState.Never)] //Hide from IntelliSense as it's very annoying trying to type "Value" and getting suggested VA instead
    public interface IVA : IValue //If this is called IVA then we'll get very annoying intellisense when we try to type IValue
    {
        long ListedAddress { get; }

        RawOffset ActualOffset { get; }

        bool IsValid { get; }
    }

    /// <summary>
    /// Encapsulates a Virtual Address (VA) (i.e. ImageBase + an VA) and the value that it points to.
    /// </summary>
    /// <typeparam name="T">The type of value that the VA points to.</typeparam>
    [EditorBrowsable(EditorBrowsableState.Never)] //Hide from IntelliSense as it's very annoying trying to type "Value" and getting suggested VA instead
    public readonly struct VA<T> : IVA
    {
        public long ListedAddress { get; }

        public int ActualOffset { get; }

        public bool IsValid { get; }

        public bool IsEmpty => !IsValid && ListedAddress == 0;

        private readonly T? value;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T Value
        {
            get
            {
                if (!IsValid)
                    throw new InvalidOperationException($"Cannot get value from VA 0x{ListedAddress:X}: address is not valid");

                return value;
            }
        }

        public T ValueOrDefault
        {
            get
            {
                if (!IsValid)
                    return default;

                return value;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        RawOffset IValue.Offset => ActualOffset; //This is the position in the FileReader that the value came from

        public VA(long listedAddress, RawOffset actualOffset, T value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            ListedAddress = listedAddress;
            ActualOffset = actualOffset;
            this.value = value;
            IsValid = true;
        }

        public VA(long listedAddress)
        {
            ListedAddress = listedAddress;
            IsValid = false;
            ActualOffset = (RawOffset)0;
            this.value = default;
        }

        #region VA == VA

        public static bool operator ==(VA<T> left, VA<T> right)
        {
            if (!left.IsValid)
            {
                if (!right.IsValid)
                {
                    //They're both invalid
                    if (left.ListedAddress != right.ListedAddress)
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

            if (left.ListedAddress != right.ListedAddress)
                return false;

            if (left.Value == null)
            {
                if (right == null)
                    return true;

                return false;
            }

            return left.Value.Equals(right.Value);
        }

        public static bool operator !=(VA<T> left, VA<T> right) => !(left == right);

        #endregion
        #region VA == T

        public static bool operator ==(VA<T> left, T right)
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

        public static bool operator !=(VA<T> left, T right) => !(left == right);

        #endregion
        #region T == VA

        public static bool operator ==(T left, VA<T> right) => right == left;

        public static bool operator !=(T left, VA<T> right) => right == left;

        #endregion

        public override bool Equals(object obj)
        {
            if (obj is T t)
                return this == t;

            if (obj is VA<T> r)
                return this == r;

            return false;
        }

        public bool Equals(VA<T> other) => this == other;

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + ActualOffset.GetHashCode();
                hash = hash * 31 + ListedAddress.GetHashCode();

                if (Value != null)
                    hash = hash * 31 + Value.GetHashCode();

                return hash;
            }
        }

        public override string ToString()
        {
            if (!IsValid)
            {
                if (ListedAddress == 0)
                    return "<empty>";

                return "0x" + ListedAddress.ToString("X") + " <bad>";
            }

            if (Value is string s)
                return s;

            if (typeof(T).IsArray)
                return "0x" + ListedAddress.ToString("X") + $" : (Length: {((Array) (object) Value).Length})";

            if (Value != null)
            {
                if (Value is ulong ul)
                    return "0x" + ul.ToString("X");

                return Value.ToString();
            }

            return "0x" + ListedAddress.ToString("X") + " : " + typeof(T).Name;
        }
    }
}
