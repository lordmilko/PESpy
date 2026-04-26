using System;
using ClrDebug.OMF;
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
            writer.WriteGlobalField(Offset + 4, LfoDir, sizeof(int), ViewKind.LfoDir);

            //Data comes before the headers

            var isLEFile = writer is LEViewWriter;

            for (var i = 0; i < DirEntries.Length; i++)
            {
                ref var entry = ref DirEntries[i];

                var data = entry.Data;

                switch (entry.SubSection)
                {
                    case SST.SSTMODULE: //smd (16-bit) / smd32 (32-bit)
                        writer.WriteGlobal((IViewable) data);
                        break;

                    case SST.SSTPUBLIC: //RawValue<pbi[]> (16-bit) / RawValue<pbi[]> (32-bit)
                        if (isLEFile)
                            writer.WriteGlobal(((RawValue<pbi32[]>) data).Value);
                        else
                            writer.WriteGlobal(((RawValue<pbi[]>) data).Value);
                        break;

                    case SST.SSTTYPES:
                    case SST.SSTCOMPACTED: //RawValue<OldTypType[]>
                        var oldTypType = (RawValue<OldTypType[]>) data;
                        writer.WriteGlobal(oldTypType.Offset, oldTypType.Value, entry.cb, ViewKind.OldTypType);
                        break;

                    case SST.SSTSYMBOLS: //RawValue<OldSymType[]>
                        var oldSymType = (RawValue<OldSymType[]>) data;
                        writer.WriteGlobal(oldSymType.Offset, oldSymType.Value, entry.cb, ViewKind.OldSymType);
                        break;

                    case SST.SSTLIBRARIES: //RawValue<FixedAnsiString[]>
                        var libraries = (RawValue<FixedAnsiString[]>) data;
                        writer.WriteGlobal(libraries.Offset, libraries.Value, entry.cb, ViewKind.LibraryName);
                        break;

                    case SST.SSTIMPORTS:
                        throw new NotImplementedException();

                    case SST.SSTSRCLINES:
                    case SST.SSTSRCLNSEG:
                        if (isLEFile)
                            writer.WriteGlobal(((RawValue<loe32[]>) data).Value);
                        else
                            writer.WriteGlobal(((RawValue<loe[]>) data).Value);
                        break;

                    default:
                        throw new NotImplementedException();
                }
            }

            writer.WriteGlobalField(Offset + LfoDir, cDir, sizeof(ushort), ViewKind.cDir);
            writer.WriteGlobal(DirEntries);

            //The file ends with an OMFSignature containing the same signature as is at the start of the OMF data, and lfoBase.
            //We don't use OMFSignature, because it calls the offset "filepos". But the filepos is specifically either lfoDir or
            //lfoBase depending on whether the signature is at the start or end of the file

            writer.WriteGlobalField(Offset + LfoBase - 8, LfoBase, sizeof(int), ViewKind.LfoBase);
            writer.WriteGlobal(Offset + LfoBase - 4, Signature, sizeof(int), ViewKind.CodeViewSig);
        }

        IView? IViewable.WriteStruct(ViewWriter writer) => null;

        int IViewable.NumChildren() => throw new NotSupportedException();

        void IViewable.WriteChild(int index, ref StructWriter structWriter) => throw new NotSupportedException();
    }
}
