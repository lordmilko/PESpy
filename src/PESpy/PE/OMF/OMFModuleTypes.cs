using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.PDB;
using PESpy.View;

namespace PESpy
{
    //Type is made up
    [DebuggerDisplay("{Signature} Types")]
    public class OMFModuleTypes : IValue, IViewable
    {
        public CV_SIGNATURE Signature { get; }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public TypTypeList List { get; }

        public int Offset { get; }

        internal OMFModuleTypes(
            in MemoryChunk valueChunk,
            CV_SIGNATURE signature,
            TypTypeList types,
            ICodeViewAccessor codeViewAccessor)
        {
            //We're not a module; we have types, not symbols
            SymbolMemoryTracker.RegisterCVSymbolMemory(valueChunk, codeViewAccessor, null);

            Offset = valueChunk.AbsoluteOffset;
            Signature = signature;
            List = types;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            Debug.Assert(Signature is CV_SIGNATURE.C7 or CV_SIGNATURE.C11 or CV_SIGNATURE.C13);

            writer.WriteGlobal(Offset, Signature, sizeof(int), ViewKind.CvSignature);
            writer.WriteGlobal(Offset + 4, List);
        }

        IView? IViewable.WriteStruct(ViewWriter writer) => null;

        int IViewable.NumChildren() => throw new NotSupportedException();

        void IViewable.WriteChild(int index, ref StructWriter structWriter) => throw new NotSupportedException();
    }
}
