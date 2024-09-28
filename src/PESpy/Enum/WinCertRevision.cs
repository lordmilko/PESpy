using PESpy.Native;

namespace PESpy
{
    /// <summary>
    /// Represents the WIN_CERT_REVISION_* enumeration that specifies the revision of a <see cref="WIN_CERTIFICATE"/>.
    /// </summary>
    public enum WinCertRevision : short
    {
        WIN_CERT_REVISION_1_0 = 0x0100,
        WIN_CERT_REVISION_2_0 = 0x0200
    }
}