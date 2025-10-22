using System;

namespace PESpy.PDB
{
    //Type is made up
    public class InlineeSigAndLines
    {
        public CV_INLINEELINES_SIGNATURE Signature { get; }

        /// <summary>
        /// Represents an array of either <see cref="InlineeSourceLine"/> or <see cref="InlineeSourceLineEx"/> entries, based on the value
        /// found in <see cref="Signature"/>.
        /// </summary>
        public Array Lines { get; }

        internal InlineeSigAndLines(CV_INLINEELINES_SIGNATURE signature, Array lines)
        {
            Lines = lines;
        }
    }
}
