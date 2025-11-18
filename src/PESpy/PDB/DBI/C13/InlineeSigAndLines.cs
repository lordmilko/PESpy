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

        internal int StructSize
        {
            get
            {
                if (Signature == CV_INLINEELINES_SIGNATURE.CV_INLINEE_SOURCE_LINE_SIGNATURE)
                    return sizeof(int) + (Lines.Length * InlineeSourceLine.StructSize);

                var lines = (InlineeSourceLineEx[]) Lines;

                var size = sizeof(int);

                foreach (var line in lines)
                    size += line.StructSize;

                return size;
            }
        }

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
            writer.NewStruct(Strings.InlineeSigAndLines, this, ViewKind.InlineeSigAndLines, StructSize);

        int IViewable.NumChildren() => 1 + Lines.Length;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            if (index == 0)
                structWriter.WriteField(nameof(Signature), 0, Signature, sizeof(int));
            else
            {
                if (Signature == CV_INLINEELINES_SIGNATURE.CV_INLINEE_SOURCE_LINE_SIGNATURE)
                    structWriter.WriteInline(((InlineeSourceLine[]) Lines)[index - 1]);
                else
                    structWriter.WriteInline(((InlineeSourceLineEx[]) Lines)[index - 1]);
            }
        }
    }
}
