using System;
using System.Runtime.CompilerServices;
using ClrDebug.PDB;
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
            Signature = signature;
            Lines = lines;
        }

        public InlineeSourceLine this[int index]
        {
            get
            {
                if (Signature == CV_INLINEELINES_SIGNATURE.CV_INLINEE_SOURCE_LINE_SIGNATURE)
                    return ((InlineeSourceLine[]) Lines)[index];

                //Implicit cast
                return ((InlineeSourceLineEx[]) Lines)[index];
            }
        }

        /// <summary>
        /// Gets the line whose <see cref="InlineeSourceLine.inlinee"/> or <see cref="InlineeSourceLineEx.inlinee"/>
        /// contains the specified inlinee item ID.
        /// </summary>
        /// <param name="inlinee">The inlinee item ID to match against.</param>
        /// <param name="line">The line that was found.</param>
        /// <param name="extraFileIds">If <see cref="Signature"/> is <see cref="CV_INLINEELINES_SIGNATURE.CV_INLINEE_SOURCE_LINE_SIGNATURE_EX"/>, gets any extra files that are associated with the line.</param>
        /// <returns>True if a line could be found containing the specified inlinee. Otherwise, false.</returns>
        public bool TryGetLineForInlinee(CV_ItemId inlinee, out InlineeSourceLine line, out NativeSpan<CV_off32_t> extraFileIds)
        {
            if (Signature == CV_INLINEELINES_SIGNATURE.CV_INLINEE_SOURCE_LINE_SIGNATURE)
            {
                var lines = (InlineeSourceLine[]) Lines;

                for (var i = 0; i < lines.Length; i++)
                {
                    ref var item = ref lines[i];

                    if (item.inlinee == inlinee)
                    {
                        line = item;
                        extraFileIds = default;
                        return true;
                    }
                }
            }
            else
            {
                var lines = (InlineeSourceLineEx[]) Lines;

                for (var i = 0; i < lines.Length; i++)
                {
                    ref var item = ref lines[i];

                    if (item.inlinee == inlinee)
                    {
                        line = item;
                        extraFileIds = item.extraFileId;
                        return true;
                    }
                }
            }

            line = default;
            extraFileIds = default;
            return false;
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
