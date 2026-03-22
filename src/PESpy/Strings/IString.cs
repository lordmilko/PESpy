using System;

namespace PESpy
{
    public interface IString<TString, TChar> :
        IEquatable<TString>,
        IComparable<TString>
        //You're not allowed to explicitly implement IEquatable<string> in case TString is also string, which would mean we already have an IEquatable<string> above
        //IEquatable<string>,
        //IComparable<string>
    {
        int Length { get; }

        bool StartsWith(string value);

        bool EndsWith(string value);

        bool Contains(string value);

        void CopyTo(Span<TChar> destination);

        void CopyTo(Span<char> destination);

        Span<TChar> AsSpan();

        //IEquatable<string> / IComparable<string>

        bool Equals(string? other);

        bool Equals(ReadOnlySpan<char> other);

        int CompareTo(string other);
    }
}
