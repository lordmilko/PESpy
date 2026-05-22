using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace PESpy
{
    internal abstract class NameMatcher : IDisposable
    {
        public static NameMatcher Create(string name)
        {
            /* *Foo* - Contains "Foo"
             * Foo* - Start with "Foo"
             * *Foo - Ends with "Foo"
             *
             * *Foo*Bar* - Contains "Foo" and "Bar" anywhere in the string, but Foo before Bar
             * Foo*Bar* - Starts with "Foo", then anywhere after it contains "Bar"
             * *Foo*Bar - Ends with "Bar", and anywhere before it is "Foo"
             * Foo*Bar - Starts with "Foo", ends with "Bar"
             *
             * *Foo*Bar*Baz* - contains "Foo", "Bar" and "Baz" anywhere in the string
             * Foo*Bar*Baz* - Starts with "Foo", contains "Bar" and "Baz" anywhere after it
             * *Foo*Bar*Baz - Ends with "Baz", contains "Foo" and "Bar" before it
             * Foo*Bar*Baz - Starts with "Foo", ends with "Bar", contains "Baz"
             */
            if (name.StartsWith("*"))
            {
                var nextWildcard = name.IndexOf('*', 1);

                if (nextWildcard == -1)
                    return new EndsWithIgnoreCaseNameMatcher(name.TrimStart('*'));

                if (nextWildcard == name.Length - 1)
                    return new ContainsIgnoreCaseNameMatcher(name.Trim('*'));

                return new RegexNameMatcher(name);
            }
            else if (name.EndsWith("*"))
            {
                var previousWildcard = name.IndexOf('*', 0, name.Length - 1);

                if (previousWildcard == -1)
                    return new StartsWithIgnoreCaseNameMatcher(name.TrimEnd('*'));

                Debug.Assert(previousWildcard != 0);

                return new RegexNameMatcher(name);
            }
            else if (name.Contains("*"))
            {
                return new RegexNameMatcher(name);
            }
            else
            {
                return new IgnoreCaseNameMatcher(name);
            }
        }

        public abstract bool IsMatch(SymString name);

        public abstract void Dispose();

        internal class EndsWithIgnoreCaseNameMatcher : NameMatcher
        {
            protected IntPtr _target;
            protected int _length;

            internal EndsWithIgnoreCaseNameMatcher(string target)
            {
                _target = Marshal.StringToHGlobalAnsi(target);
                _length = target.Length;
            }

            public override unsafe bool IsMatch(SymString name) =>
                StringHelpers.EndsWithIgnoreCase(name.AsSpan(), new ReadOnlySpan<byte>((byte*) _target, _length));

            public override void Dispose()
            {
                Marshal.FreeHGlobal(_target);
            }
        }

        internal class ContainsIgnoreCaseNameMatcher : NameMatcher
        {
            protected IntPtr _target;
            protected int _length;

            internal ContainsIgnoreCaseNameMatcher(string target)
            {
                _target = Marshal.StringToHGlobalAnsi(target);
                _length = target.Length;
            }

            public override unsafe bool IsMatch(SymString name) =>
                StringHelpers.ContainsIgnoreCase(name.Value, new ReadOnlySpan<byte>((byte*) _target, _length));

            public override void Dispose()
            {
                Marshal.FreeHGlobal(_target);
            }
        }

        internal class RegexNameMatcher : NameMatcher
        {
            protected IntPtr _target;
            protected int _length;

            internal RegexNameMatcher(string target)
            {
                _target = Marshal.StringToHGlobalUni(target);
                _length = target.Length;
            }

            //It's assumed this is not an ST string
            public override unsafe bool IsMatch(SymString name) => !SymRegex.CompareRE(name.Value, (char*) _target, false);

            public override void Dispose()
            {
                Marshal.FreeHGlobal(_target);
            }
        }

        internal class StartsWithIgnoreCaseNameMatcher : NameMatcher
        {
            protected IntPtr _target;
            protected int _length;

            internal StartsWithIgnoreCaseNameMatcher(string target)
            {
                _target = Marshal.StringToHGlobalAnsi(target);
                _length = target.Length;
            }

            public override unsafe bool IsMatch(SymString name) =>
                StringHelpers.StartsWithIgnoreCase(name.AsSpan(), new ReadOnlySpan<byte>((byte*) _target, _length));

            public override void Dispose()
            {
                Marshal.FreeHGlobal(_target);
            }
        }

        class IgnoreCaseNameMatcher : NameMatcher
        {
            protected IntPtr _target;
            protected int _length;

            internal IgnoreCaseNameMatcher(string target)
            {
                _target = Marshal.StringToHGlobalAnsi(target);
                _length = target.Length;
            }

            //It's assumed this is not an ST string
            public override unsafe bool IsMatch(SymString name) =>
                StringHelpers.EqualsIgnoreCase(name.AsSpan(), new ReadOnlySpan<byte>((byte*) _target, _length));

            public override void Dispose()
            {
                Marshal.FreeHGlobal(_target);
            }
        }
    }
}
