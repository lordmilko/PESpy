using PESpy.Native;

namespace PESpy
{
    /// <summary>
    /// Represents the WIN_CERT_TYPE_* enumeration that describes the type of certificate found in a <see cref="WIN_CERTIFICATE"/>
    /// </summary>
    public enum WinCertType : short
    {
        /// <summary>
        /// The certificate contains an X.509 certificate.
        /// </summary>
        X509 = 0x0001,

        /// <summary>
        /// The certificate contains a PKCS SignedData structure.
        /// </summary>
        SignedData = 0x0002,

        /// <summary>
        /// Reserved.
        /// </summary>
        Reserved1 = 0x0003,

        /// <summary>
        /// The certificate contains PKCS1_MODULE_SIGN fields.
        /// </summary>
        PKCS1_Sign = 0x0009
    }
}