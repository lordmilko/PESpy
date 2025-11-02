namespace PESpy
{
    public struct OSVersion
    {
        public int MajorVersion { get; }

        public int MinorVersion { get; }

        public OSVersionKind Kind
        {
            get
            {
                return MajorVersion switch
                {
                    3 => MinorVersion switch
                    {
                        10 => OSVersionKind.WindowsNT3_1,
                        50 => OSVersionKind.WindowsNT3_50,
                        51 => OSVersionKind.WindowsNT3_51,
                        _ => OSVersionKind.Unknown
                    },
                    4 => MinorVersion switch
                    {
                        0 => OSVersionKind.Windows95,
                        10 => OSVersionKind.Windows98,
                        90 => OSVersionKind.WindowsME,
                        _ => OSVersionKind.Unknown
                    },
                    5 => MinorVersion switch
                    {
                        0 => OSVersionKind.Windows2000,
                        1 => OSVersionKind.WindowsXP,
                        2 => OSVersionKind.WindowsServer2003
                    },
                    6 => MinorVersion switch
                    {
                        0 => OSVersionKind.WindowsVista,
                        1 => OSVersionKind.Windows7,
                        2 => OSVersionKind.Windows8,
                        3 => OSVersionKind.Windows8_1,
                        _ => OSVersionKind.Unknown
                    },
                    10 => OSVersionKind.Windows10,
                    _ => OSVersionKind.Unknown
                };
            }
        }

        public OSVersion(int majorVersion, int minorVersion)
        {
            MajorVersion = majorVersion;
            MinorVersion = minorVersion;
        }

        public override string ToString()
        {
            using var builder = new ValueStringBuilder();

            var str = Kind switch
            {
                OSVersionKind.WindowsNT3_1      => "Windows NT 3.1",
                OSVersionKind.WindowsNT3_50     => "Windows 3.50",
                OSVersionKind.WindowsNT3_51     => "Windows 3.51",
                OSVersionKind.Windows95         => "Windows 95",
                OSVersionKind.Windows98         => "Windows 98",
                OSVersionKind.WindowsME         => "Windows ME",
                OSVersionKind.Windows2000       => "Windows 2000",
                OSVersionKind.WindowsXP         => "Windows XP",
                OSVersionKind.WindowsServer2003 => "Windows Server 2003",
                OSVersionKind.WindowsVista      => "Windows Vista",
                OSVersionKind.Windows7          => "Windows 7",
                OSVersionKind.Windows8          => "Windows 8",
                OSVersionKind.Windows8_1        => "Windows 8.1",
                OSVersionKind.Windows10         => "Windows 10",
                OSVersionKind.Unknown           => "Unknown"
            };

            builder.Append(str);
            builder.Append(" (");
            builder.Append(MajorVersion);
            builder.Append('.');

            if (MinorVersion < 10)
                builder.Append('0');

            builder.Append(MinorVersion);

            builder.Append(')');

            return builder.ToString();
        }
    }
}
