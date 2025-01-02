using PESpy.Native;
using PESpy.View;
using System.Diagnostics;
#if !DEBUG_POSITION
using RVA = System.Int32;
using RawOffset = System.Int32;
#endif

namespace PESpy
{
    public readonly struct WinCertificate : IValue, IViewable
    {
        /// <summary>
        /// Specifies the length, in bytes, of the signature.
        /// </summary>
        public int Length { get; }

        /// <summary>
        /// Specifies the certificate revision.
        /// </summary>
        public WinCertRevision Revision { get; }

        public WinCertType CertificateType { get; }

        public IValue Certificate { get; }

        public RawOffset Offset { get; }

        internal WinCertificate(IFileReader reader)
        {
            Offset = (RawOffset) reader.Position;

            Length = reader.ReadInt32();
            Revision = (WinCertRevision) reader.ReadInt16();
            CertificateType = (WinCertType) reader.ReadInt16();

            var certificateLength = Length - 8; //Exclude the Length + Revision + CertificateType that have already been read

            switch (CertificateType)
            {
                case WinCertType.SignedData:
                    //PKCS SignedData is in ASN.1 format, which is a crazy complicated encoding. BouncyCastle.Cryptography can parse these values easily with new X509CertificateParser().ReadCertificate(bytes);
                    //There's also a new .NET library System.Formats.Asn1 that you can use to parse ASN.1 values yourself, however it is very unintuitive. You can easily create a certificate without any external
                    //libraries by doing new X509Certificate2(bytes), but the whole point is that we want to describe what each byte is doing.
                    Certificate = new SignedData(reader, certificateLength);
                    break;

                default:
                    Debug.Assert(false, $"Don't know how to parse a certificate of type {CertificateType}");
                    Certificate = new ByteBlob(reader, certificateLength);
                    break;
            }
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct(nameof(WIN_CERTIFICATE), this, ViewKind.WinCertificate);

            s.WriteField("dwLength", Length);
            s.WriteField("wRevision", Revision, sizeof(short));
            s.WriteField("wCertificateType", CertificateType, sizeof(short));
            s.WriteInline((IViewable) Certificate);
        }

        public override string ToString()
        {
            return $"{CertificateType}: {Certificate}";
        }
    }
}
