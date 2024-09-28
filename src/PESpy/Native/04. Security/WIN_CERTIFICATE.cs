namespace PESpy.Native
{
    //WinCertificate
    internal unsafe struct WIN_CERTIFICATE
    {
        public int dwLength;
        public WinCertRevision wRevision;
        public WinCertType wCertificateType;
        public fixed byte bCertificate[1];
    }
}