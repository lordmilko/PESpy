using System;
using System.Diagnostics;
using ClrDebug.OMF;
using PESpy.View;

namespace PESpy
{
    public class NB05Data : ICodeViewData, IViewable
    {
        public int LfoDir { get; }

        public CodeViewSig Signature { get; }

        public int LfoBase { get; }

        private readonly OMFDirHeader dirHeader;

        public OMFDirHeader DirHeader => dirHeader;

        public OMFDirEntry[] DirEntries { get; }

        public int Offset => chunk.AbsoluteOffset;

        private ICodeViewAccessor codeViewAccessor;

        public ICodeViewAccessor GetCodeViewAccessor() => codeViewAccessor;

        private readonly MemoryChunk chunk;

        internal NB05Data(in MemoryChunk chunk, CodeViewSig sig, int lfoBase, int lfoDir, in OMFDirHeader dirHeader, OMFDirEntry[] dirEntries, ICodeViewAccessor codeViewAccessor)
        {
            this.chunk = chunk;
            LfoDir = lfoDir;
            Signature = sig;
            LfoBase = lfoBase;
            this.dirHeader = dirHeader;
            DirEntries = dirEntries;
            this.codeViewAccessor = codeViewAccessor;
        }

        public bool TryGetSegName(int offset, out AnsiString str)
        {
            var dirEntries = DirEntries;

            for (var i = dirEntries.Length - 1; i >= 0; i--)
            {
                ref var entry = ref dirEntries[i];

                if (entry.iMod != ushort.MaxValue)
                {
                    str = default;
                    return false;
                }

                if (entry.SubSection == ClrDebug.OMF.SST.sstSegName)
                {
                    str = chunk.PeekAnsiNullTerminatedString(entry.lfo + offset);
                    return true;
                }
            }

            str = default;
            return false;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            writer.WriteGlobal(Offset, Signature, sizeof(int), ViewKind.CodeViewSig);
            writer.WriteGlobalField(Offset + 4, LfoDir, sizeof(int), ViewKind.LfoDir);

            //Data comes before the headers

            for (var i = 0; i < DirEntries.Length; i++)
            {
                ref var entry = ref DirEntries[i];

                var data = entry.Data;

                switch (entry.SubSection)
                {
                    case SST.sstModule:
                        writer.WriteGlobal((OMFModule) data);
                        break;

                    case SST.sstTypes:
                        writer.WriteGlobal((OMFModuleTypes) data);
                        break;

                    case SST.sstPublic:
                        Debug.Assert(false);
                        break;

                    case SST.sstSymbols:
                    case SST.sstPublicSym:
                    case SST.sstAlignSym:
                        writer.WriteGlobal((OMFModuleSymbols) data);
                        break;

                    case SST.sstSrcLnSeg:
                        Debug.Assert(false);
                        break;

                    case SST.sstSrcModule:
                        writer.WriteGlobal((OMFSourceModule) data);
                        break;

                    case SST.sstLibraries:
                        var libraries = (RawValue<SymString[]>) data;
                        writer.WriteGlobal(libraries.Offset, libraries.Value, ViewKind.LibraryName);
                        break;

                    case SST.sstGlobalSym:
                    case SST.sstGlobalPub:
                    case SST.sstStaticSym:
                        writer.WriteGlobal((OMFHashedSymbols) data);
                        break;

                    case SST.sstGlobalTypes:
                        writer.WriteGlobal((OMFGlobalTypes) data);
                        break;

                    case SST.sstMPC:
                        throw new NotImplementedException();

                    case SST.sstSegMap:
                        writer.WriteGlobal((OMFSegMap) data);
                        break;

                    case SST.sstSegName:
                        var segmentNames = (RawValue<AnsiString[]>) data;
                        writer.WriteGlobal(segmentNames.Offset, segmentNames.Value, ViewKind.SegmentName);
                        break;

                    case SST.sstPreComp:
                    case SST.sstPreCompMap:
                    case SST.sstOffsetMap16:
                    case SST.sstOffsetMap32:
                        Debug.Assert(false);
                        break;

                    case SST.sstFileIndex:
                        writer.WriteGlobal((OMFFileIndex) data);
                        break;

                    default:
                        throw new NotImplementedException($"Don't know how to handle {nameof(SST)} '{entry.SubSection}'");
                }
            }

            writer.WriteGlobal(DirHeader);
            writer.WriteGlobal(DirEntries);

            writer.WriteGlobal(Offset + LfoBase - 8, Signature, sizeof(int), ViewKind.CodeViewSig);
            writer.WriteGlobalField(Offset + LfoBase - 4, LfoBase, sizeof(int), ViewKind.LfoBase);
        }

        IView? IViewable.WriteStruct(ViewWriter writer) => null;

        int IViewable.NumChildren() => throw new NotSupportedException();

        void IViewable.WriteChild(int index, ref StructWriter structWriter) => throw new NotSupportedException();
    }
}
