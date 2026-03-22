using PESpy.View.Builder;

namespace PESpy.View
{
    internal class NestedPEViewWriter : PEViewWriter
    {
        private PEViewWriter outerWriter;

        internal unsafe NestedPEViewWriter(PEViewWriter parentWriter, ByteViewProvider byteViewProvider, PEFile peFile) : base(parentWriter, peFile, byteViewProvider)
        {
            this.outerWriter = parentWriter;
        }

        //This method is not super ideal, because we have to pay for a copy of it for every single struct in PESpy,
        //rather than just the structs that are a child of a PEFile, increasing our file size by a bit in NativeAOT.
        //If we rework NewStruct to just take an offset for 99% of structs, this will help reduce this impact
        protected internal override IView? NewStruct<T>(FixedUtf8String name, in T value, ViewKind kind, int structSize)
        {
            var previous = outerWriter.NestedViewWriter;
            outerWriter.NestedViewWriter = this;

            try
            {
                return outerWriter.NewStruct(name, value, kind, structSize);
            }
            finally
            {
                outerWriter.NestedViewWriter = previous;
            }
        }

        protected internal override IView? NewValue<T>(int offset, in T value, int size, ViewKind kind, bool fromRegion)
        {
            return outerWriter.NewValue(offset, value, size, kind, fromRegion);
        }

        public override void WriteOffsetXRef(int structOffset, int fieldOffset, int targetOffset)
        {
            outerWriter.WriteOffsetXRef(structOffset, fieldOffset, targetOffset);
        }

        public override void WriteRVAXRef(int structOffset, int fieldOffset, int targetRVA)
        {
            outerWriter.WriteRVAXRef(structOffset, fieldOffset, targetRVA);
        }

        public override void WriteVAXRef(int structOffset, int fieldOffset, int targetVA)
        {
            outerWriter.WriteVAXRef(structOffset, fieldOffset, targetVA);
        }
    }
}
