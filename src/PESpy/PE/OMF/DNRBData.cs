using System;
using PESpy.View;

namespace PESpy
{
    //The data generally appears to be in the same format as NB02
    public class DNRBData : ICodeView, IViewable //Type is made up
    {
        public CodeViewSig Signature { get; }

        public int Length { get; }

        //The list of offsets from the CVSECTBL at the start of the CV data
        public NativeSpan<int> SecOffset { get; }

        public ushort Version { get; }

        //Section 0
        public DNRBModule[] Modules { get; }

        //Section 1
        public RawValue<pbi[]> Publics { get; }

        //Section 2
        public RawValue<OldTypType[]> Types { get; }

        //Section 3
        public RawValue<OldSymType[]> Symbols { get; }

        //Section 4
        public RawValue<loe[]> SourceLines { get; }

        public int Offset { get; }

        internal DNRBData(
            int offset,
            CodeViewSig sig,
            int length,
            ushort version,
            NativeSpan<int> secOffset,
            DNRBModule[] modules,
            RawValue<pbi[]> publics,
            RawValue<OldTypType[]> types,
            RawValue<OldSymType[]> symbols,
            RawValue<loe[]> sourceLines)
        {
            Offset = offset;
            Signature = sig;
            Length = length;
            Version = version;
            SecOffset = secOffset;
            Modules = modules;
            Publics = publics;
            Types = types;
            Symbols = symbols;
            SourceLines = sourceLines;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            writer.WriteGlobalField(Offset, "secOffset", SecOffset, 5 * sizeof(int));
            writer.WriteGlobalField(Offset + (5 * sizeof(int)), "version", Version, sizeof(short));

            writer.WriteGlobal(Modules);
            writer.WriteGlobal(Publics.Offset, Publics.Value, SecOffset[2] - SecOffset[1], ViewKind.Value);
            writer.WriteGlobal(Types.Offset, Types.Value, SecOffset[3] - SecOffset[2], ViewKind.Value);
            writer.WriteGlobal(Symbols.Offset, Symbols.Value, SecOffset[4] - SecOffset[3], ViewKind.Value);

            var sourceLinesLength = Length - (SecOffset[4] - Offset) - 8; //There's an 8 byte CVINFO at the end
            writer.WriteGlobal(SourceLines.Offset, SourceLines.Value, sourceLinesLength, ViewKind.Value);

            var cvInfoOffset = (Offset + Length) - 8;
            writer.WriteGlobalField(cvInfoOffset, "signature", Signature, sizeof(int));
            writer.WriteGlobalField(cvInfoOffset + 4, "secTblOffset", Offset, sizeof(int)); //The offset to the MemoryChunk comes from the OMFSignature filepos, which is secTblOffset
        }

        IView? IViewable.WriteStruct(ViewWriter writer) => null;

        int IViewable.NumChildren() => throw new NotSupportedException();

        void IViewable.WriteChild(int index, ref StructWriter structWriter) => throw new NotSupportedException();
    }
}
