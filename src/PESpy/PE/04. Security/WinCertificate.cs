using PESpy.Native;
using PESpy.View;
using System.Diagnostics;

namespace PESpy
{
    public struct WinCertificate : IValue, IViewable
    {
        /// <summary>
        /// Specifies the length, in bytes, of the signature.
        /// </summary>
        public int Length => chunk.PeekInt32(0);

/// <summary>
/// Specifies the certificate revision.
/// </summary>
        public WinCertRevision Revision => (WinCertRevision) chunk.PeekUInt16(4);

        public WinCertType CertificateType => (WinCertType) chunk.PeekUInt16(6);

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

        public int Offset => chunk.AbsoluteOffset;

        private readonly MemoryChunk chunk;

        internal WinCertificate(in MemoryChunk chunk)
        {
            this.chunk = chunk;
            certificate = default;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.WIN_CERTIFICATE, this, ViewKind.WinCertificate, Length); //Length includes the fields before the data

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField("dwLength", Length);
            s.WriteField("wRevision", Revision, sizeof(short));
            s.WriteField("wCertificateType", CertificateType, sizeof(short));
            s.WriteInline((IViewable) Certificate);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }

        public override string ToString()
        {
            return $"{CertificateType}: {Certificate}";
        }
    }
}
