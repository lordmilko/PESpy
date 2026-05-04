using System;
using PESpy.View;

namespace PESpy
{
    //The data generally appears to be in the same format as NB02
    public class DNRBData : ICodeViewData, IViewable //Type is made up
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

        public long Offset { get; }

        internal DNRBData(
            long offset,
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
            writer.WriteGlobalField(Offset, SecOffset, 5 * sizeof(int), ViewKind.DNRBSecOffset);
            writer.WriteGlobalField(Offset + (5 * sizeof(int)), Version, sizeof(short), ViewKind.DNRBVersion);

            writer.WriteGlobal(Modules);
            writer.WriteGlobal(Publics.Offset, Publics.Value, SecOffset[2] - SecOffset[1], ViewKind.DNRB_Publics);
            writer.WriteGlobal(Types.Offset, Types.Value, SecOffset[3] - SecOffset[2], ViewKind.DNRB_Types);
            writer.WriteGlobal(Symbols.Offset, Symbols.Value, SecOffset[4] - SecOffset[3], ViewKind.DNRB_Symbols);

            var sourceLinesLength = Length - (SecOffset[4] - Offset) - 8; //There's an 8 byte CVINFO at the end
            writer.WriteGlobal(SourceLines.Offset, SourceLines.Value, (int) sourceLinesLength, ViewKind.DNRB_SourceLines);

            var cvInfoOffset = (Offset + Length) - 8;
            writer.WriteGlobalField(cvInfoOffset, Signature, sizeof(int), ViewKind.DNRBSignature);
            writer.WriteGlobalField(cvInfoOffset + 4, Offset, sizeof(int), ViewKind.DNRBSecTblOffset); //The offset to the MemoryChunk comes from the OMFSignature filepos, which is secTblOffset
        }

        IView? IViewable.WriteStruct(ViewWriter writer) => null;

        int IViewable.NumChildren() => throw new NotSupportedException();

        void IViewable.WriteChild(int index, ref StructWriter structWriter) => throw new NotSupportedException();
    }
}
