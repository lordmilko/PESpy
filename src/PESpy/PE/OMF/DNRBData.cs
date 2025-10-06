namespace PESpy
{
    //The data generally appears to be in the same format as NB02
    public class DNRBData : ICodeView
    {
        public CodeViewSig Signature { get; }

        public int Offset { get; }
    }
}
