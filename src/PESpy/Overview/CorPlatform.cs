using ClrDebug;
using static ClrDebug.COMIMAGE_FLAGS;

namespace PESpy
{
    public static partial class FileOverview
    {
        public class CorPlatform
        {
            public bool Is32BitRequired => (flags & COMIMAGE_FLAGS_32BITREQUIRED) != 0;

            public bool Is32BitPreferred => (flags & COMIMAGE_FLAGS_32BITPREFERRED) != 0;

            private COMIMAGE_FLAGS flags;
            private PEMagic magic;

            internal CorPlatform(COMIMAGE_FLAGS flags, PEMagic magic)
            {
                this.flags = flags;
                this.magic = magic;
            }

            public override string ToString()
            {
                if (magic == PEMagic.IMAGE_NT_OPTIONAL_HDR64_MAGIC)
                    return "x64";

                //The spec says it's illegal to say 32BITPREFERRED without also saying 32BITREQUIRED, however I tested this
                //and this is wrong https://github.com/dotnet/runtime/blob/7201a39b318e4916f704e3a5c2fa96e3d58bc352/src/coreclr/inc/corhdr.h#L82

                if (Is32BitPreferred)
                    return "AnyCPU (Prefer 32-bit)";

                if (Is32BitRequired)
                    return "x86";

                return "AnyCPU";
            }
        }
    }
}
