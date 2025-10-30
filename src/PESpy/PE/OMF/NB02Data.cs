using System;
using PESpy.View;

namespace PESpy
{
    public class NB02Data : ICodeViewData, IViewable
    {
        public int LfoDir { get; }

        public CodeViewSig Signature { get; }

        public int LfoBase { get; }

        public ushort cDir { get; }

        public dnt[] DirEntries { get; }

        public int Offset { get; }

        internal NB02Data(int offset, CodeViewSig sig, int lfoBase, int lfoDir, ushort cDir, dnt[] dirEntries)
        {
            Offset = offset;
            LfoDir = lfoDir;
            Signature = sig;
            LfoBase = lfoBase;
            this.cDir = cDir;
            DirEntries = dirEntries;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            writer.WriteGlobal(Offset, Signature, sizeof(int), ViewKind.CodeViewSig);
            writer.WriteGlobalField(Offset + 4, "lfoDir", LfoDir, sizeof(int));

            //Data comes before the headers

            for (var i = 0; i < DirEntries.Length; i++)
            {
                ref var entry = ref DirEntries[i];

                var data = entry.Data;

                if (data is IViewable v)
                    writer.WriteGlobal(v);
                else if (data is RawValue<OldSymType[]> r1)
                    writer.WriteGlobal(r1.Offset, r1.Value, entry.cb, ViewKind.Value); //todo: use better kind
                else if (data is RawValue<OldTypType[]> r2)
                    writer.WriteGlobal(r2.Offset, r2.Value, entry.cb, ViewKind.Value); //todo: use better kind
                else if (data is RawValue<pbi[]> r3)
                    writer.WriteGlobal(r3.Offset, r3.Value, entry.cb, ViewKind.Value); //todo: use better kind
                else if (data is RawValue<FixedAnsiString[]> r4)
                    writer.WriteGlobal(r4.Offset, r4.Value, entry.cb, ViewKind.Value); //todo: use better kind
                else if (data is RawValue<loe[]> r5)
                    writer.WriteGlobal(r5.Offset, r5.Value, entry.cb, ViewKind.Value); //todo: use better kind
                else
                {
                    if (data != null)
                        throw new NotImplementedException($"Don't know how to write a global of type '{data.GetType().Name}'");
                }
            }

            writer.WriteGlobalField(Offset + LfoDir, "cDir", cDir, sizeof(ushort));
            writer.WriteGlobal(DirEntries);

            //The file ends with an OMFSignature containing the same signature as is at the start of the OMF data, and lfoBase.
            //We don't use OMFSignature, because it calls the offset "filepos". But the filepos is specifically either lfoDir or
            //lfoBase depending on whether the signature is at the start or end of the file

            writer.WriteGlobalField(Offset + LfoBase - 8, "lfoBase", LfoBase, sizeof(int));
            writer.WriteGlobal(Offset + LfoBase - 4, Signature, sizeof(int), ViewKind.CodeViewSig);
        }

        IView? IViewable.WriteStruct(ViewWriter writer) => null;

        int IViewable.NumChildren() => throw new NotSupportedException();

        void IViewable.WriteChild(int index, ref StructWriter structWriter) => throw new NotSupportedException();
    }
}
