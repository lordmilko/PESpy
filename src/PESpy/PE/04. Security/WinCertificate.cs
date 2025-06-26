using PESpy.Native;
using PESpy.View;
using System.Diagnostics;
#if !DEBUG_POSITION
using RVA = System.Int32;
using RawOffset = System.Int32;
#endif

namespace PESpy
{
    public struct WinCertificate : IValue, IViewable
    {
        /// <summary>
        /// Specifies the length, in bytes, of the signature.
        /// </summary>
#if PEFAST
        public int Length => chunk.PeekInt32(0);
#else
        public int Length { get; }
#endif

/// <summary>
/// Specifies the certificate revision.
/// </summary>
#if PEFAST
        public WinCertRevision Revision => (WinCertRevision) chunk.PeekUInt16(4);
#else
        public WinCertRevision Revision { get; }
#endif

#if PEFAST
        public WinCertType CertificateType => (WinCertType) chunk.PeekUInt16(6);
#else
        public WinCertType CertificateType { get; }
#endif

#if PEFAST
        private IValue? certificate;

        public IValue Certificate
        {
            get
            {
                if (certificate == null)
                {
                    var certificateLength = Length - 8; //Exclude the Length + Revision + CertificateType that have already been read

                    switch (CertificateType)
                    {
                        case WinCertType.SignedData:
                            //PKCS SignedData is in ASN.1 format, which is a crazy complicated encoding. BouncyCastle.Cryptography can parse these values easily with new X509CertificateParser().ReadCertificate(bytes);
                            //There's also a new .NET library System.Formats.Asn1 that you can use to parse ASN.1 values yourself, however it is very unintuitive. You can easily create a certificate without any external
                            //libraries by doing new X509Certificate2(bytes), but the whole point is that we want to describe what each byte is doing.
                            certificate = new SignedData(chunk.Slice(8), certificateLength);
                            break;

                        default:
                            Debug.Assert(false, $"Don't know how to parse a certificate of type {CertificateType}");
                            certificate = new ByteBlob(chunk.Slice(8), certificateLength);
                            break;
                    }
                }

                return certificate;
            }
        }
#else
        public IValue Certificate { get; }
#endif

#if PEFAST
        public RawOffset Offset => chunk.AbsoluteOffset;
#else
        public RawOffset Offset { get; }
#endif

#if PEFAST
        private readonly MemoryChunk chunk;

        internal WinCertificate(in MemoryChunk chunk)
        {
            this.chunk = chunk;
            certificate = default;
        }
#else
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
#endif

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(nameof(WIN_CERTIFICATE), this, ViewKind.WinCertificate, Length); //Length includes the fields before the data

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField("dwLength", Length);
            s.WriteField("wRevision", Revision, sizeof(short));
            s.WriteField("wCertificateType", CertificateType, sizeof(short));
            s.WriteInline((IViewable) Certificate);

            return s.ToArray();
        }

        public override string ToString()
        {
            return $"{CertificateType}: {Certificate}";
        }
    }
}
