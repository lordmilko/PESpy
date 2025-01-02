using System.Security.Cryptography.X509Certificates;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy
{
    public class SignedData : IValue, IViewable
    {
        public byte[] Bytes { get; }

        private X509Certificate2? certificate;

        //Attempting to parse the ASN.1 encoded structure contained in the bytes is absolutely seriously way too insanely complicated.
        //Visualising how the ASN.1 data is physically represented in the PE would be kind of cool, but it's such an extreme amount of work
        //to implement what will potentially be the _full_ ASN.1 specification that it doesn't make sense to do; we can say what the certificate is,
        //I think that's good enough
        public X509Certificate2 Certificate
        {
            get
            {
                //https://blog.trailofbits.com/2020/05/27/verifying-windows-binaries-without-windows/
                //https://download.microsoft.com/download/9/c/5/9c5b2167-8017-4bae-9fde-d599bac8184a/Authenticode_PE.docx

                //While the authenticode spec apparently has some "non-standard" things compared to the normal SignedData definition, it seems to me like there's either different identifiers in places, or ASN.1 parsers automatically know how to parse things, whatever the shape

                if (certificate == null)
                    certificate = new X509Certificate2(Bytes);

                return certificate;
            }
        }

        public RawOffset Offset { get; }

        internal SignedData(IFileReader reader, int length)
        {
            Offset = (RawOffset) reader.Position;

            Bytes = reader.ReadBytes(length);
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct(nameof(SignedData), this, ViewKind.SignedData);

            s.WriteField("Bytes", Bytes);
        }

        public override string ToString()
        {
            return Certificate.ToString();
        }
    }
}
