using System;

namespace PESpy
{
    public interface IString
    {
        int Length { get; }

        bool StartsWith(string value);

        bool EndsWith(string value);

        bool Contains(string value);

        void CopyTo(Span<char> destination);

        //IEquatable<string> / IComparable<string>

        bool Equals(string? other);

        bool Equals(ReadOnlySpan<char> other);

        int CompareTo(string other);
    }

    public interface IString<TString, TChar> :
        IEquatable<TString>,
        IComparable<TString>,
        IString
        //You're not allowed to explicitly implement IEquatable<string> in case TString is also string, which would mean we already have an IEquatable<string> above
        //IEquatable<string>,
        //IComparable<string>
    {
        void CopyTo(Span<TChar> destination);

        Span<TChar> AsSpan();

        int CompareToIgnoreCase(TString other);
    }
}
