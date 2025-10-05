using System;

namespace PESpy.PDB
{
    //Type is made up
    public class InlineeSigAndLines
    {
        public CV_INLINEELINES_SIGNATURE Signature { get; }

        public Array Lines { get; }

        internal InlineeSigAndLines(CV_INLINEELINES_SIGNATURE signature, Array lines)
        {
            Lines = lines;
        }
    }
}
