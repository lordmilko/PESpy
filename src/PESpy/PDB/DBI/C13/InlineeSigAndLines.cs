using System;
using PESpy.View;

namespace PESpy.PDB
{
    //Type is made up
    public class InlineeSigAndLines : IViewableValue
    {
        public CV_INLINEELINES_SIGNATURE Signature { get; }

        /// <summary>
        /// Represents an array of either <see cref="InlineeSourceLine"/> or <see cref="InlineeSourceLineEx"/> entries, based on the value
        /// found in <see cref="Signature"/>.
        /// </summary>
        public Array Lines { get; }

        public int Offset { get; }

        internal InlineeSigAndLines(int offset, CV_INLINEELINES_SIGNATURE signature, Array lines)
        {
            Offset = offset;
            Lines = lines;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            throw new System.NotImplementedException();

        int IViewable.NumChildren() => throw new System.NotImplementedException();

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            throw new System.NotImplementedException();
        }
    }
}
