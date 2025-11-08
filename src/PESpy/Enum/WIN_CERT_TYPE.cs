using PESpy.Native;

namespace PESpy
{
    /// <summary>
    /// Represents the WIN_CERT_TYPE_* enumeration that describes the type of certificate found in a <see cref="WIN_CERTIFICATE"/>
    /// </summary>
    public enum WIN_CERT_TYPE : short
    {
        /// <summary>
        /// The certificate contains an X.509 certificate.
        /// </summary>
        WIN_CERT_TYPE_X509 = 0x0001,

        /// <summary>
        /// The certificate contains a PKCS SignedData structure.
        /// </summary>
        WIN_CERT_TYPE_PKCS_SIGNED_DATA = 0x0002,

        /// <summary>
        /// Reserved.
        /// </summary>
        WIN_CERT_TYPE_RESERVED_1 = 0x0003,

        /// <summary>
        /// Terminal Server Protocol Stack Certificate signing
        /// </summary>
        WIN_CERT_TYPE_TS_STACK_SIGNED = 0x0004,

        /// <summary>
        /// The certificate contains PKCS1_MODULE_SIGN fields.
        /// </summary>
        WIN_CERT_TYPE_PKCS1_SIGN = 0x0009
    }
}
