namespace PESpy.Native
{
    //WinCertificate
    internal unsafe struct WIN_CERTIFICATE
    {
        public int dwLength;
        public WIN_CERT_REVISION wRevision;
        public WIN_CERT_TYPE wCertificateType;
        public fixed byte bCertificate[1];
    }
}
