using System;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using PESpy.View;

namespace PESpy
{
    public class SignedData : IViewableValue
    {
        private const int BytesOffset = 0;

        public NativeSpan<byte> Bytes => chunk.PeekNativeSpan<byte>(BytesOffset, length);

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
                {
#if NET
                    //They say loading the certificate through the ctor is obsolute, but I tried to use X509CertificateLoader.LoadCertificate
                    //and got an error "cannot find the requested object" so I'm going to use the normal ctor instead
                    certificate = new X509Certificate2((ReadOnlySpan<byte>) Bytes); //There is a ctor that takes a span but it's only available in .NET 5+
#else
                    certificate = new X509Certificate2(Bytes.ToArray()); //There is a ctor that takes a span but it's only available in .NET 5+
#endif
                }

                return certificate;
            }
        }

        public long Offset => chunk.AbsoluteOffset;

        private readonly MemoryChunk chunk;
        private readonly int length;

        internal SignedData(in MemoryChunk chunk, int length)
        {
            this.chunk = chunk;
            this.length = length;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.SignedData, length);

        int IViewable.NumChildren() => 1;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField("Bytes", BytesOffset, Bytes);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }

        //If we're compiling for Native AOT, touching the certificate
        //will bring in a whole bunch of stuff we don't want
#if !NATIVEAOT
        public override string ToString()
        {
            return Certificate.ToString();
        }
#endif
    }
}
