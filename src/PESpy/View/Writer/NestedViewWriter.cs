using PESpy.View.Builder;

namespace PESpy.View
{
    internal class NestedViewWriter : ViewWriter
    {
        private ViewWriter outerWriter;

        internal unsafe NestedViewWriter(
            ViewWriter parentWriter,
            IViewWriterHelper helper,
            ByteViewProvider byteViewProvider) : base(parentWriter, helper, byteViewProvider)
        {
            this.outerWriter = parentWriter;
        }

        //This method is not super ideal, because we have to pay for a copy of it for every single struct in PESpy,
        //rather than just the structs that are a child of a PEFile, increasing our file size by a bit in NativeAOT.
        //If we rework NewStruct to just take an offset for 99% of structs, this will help reduce this impact
        protected internal override IView? NewStruct<T>(in T value, ViewKind kind, int structSize)
        {
            var previousWriter = outerWriter.NestedViewWriter;
            var previousRegion = outerWriter.FromRegion;

            outerWriter.NestedViewWriter = this;
            outerWriter.FromRegion = FromRegion;

            try
            {
                return outerWriter.NewStruct(value, kind, structSize);
            }
            finally
            {
                outerWriter.NestedViewWriter = previousWriter;
                outerWriter.FromRegion = previousRegion;
            }
        }

        protected internal override IView? NewUnmanagedStruct<T>(in T value, ViewKind kind, int structSize)
        {
            throw new System.NotImplementedException();
        }

        protected internal override IView? NewValue<T>(long offset, in T value, int size, ViewKind kind, bool fromRegion)
        {
            return outerWriter.NewValue(offset, value, size, kind, fromRegion);
        }

        public override void WriteOffsetXRef(long structOffset, int fieldOffset, long targetOffset)
        {
            outerWriter.WriteOffsetXRef(structOffset, fieldOffset, targetOffset);
        }

        public override void WriteRVAXRef(long structOffset, int fieldOffset, int targetRVA)
        {
            outerWriter.WriteRVAXRef(structOffset, fieldOffset, targetRVA);
        }

        public override void WriteVAXRef(long structOffset, int fieldOffset, long targetVA)
        {
            outerWriter.WriteVAXRef(structOffset, fieldOffset, targetVA);
        }

        internal override RegionWriter CreateRegion(
            long offset,
            long structOffset,
            int fieldOffset,
            string name,
            ViewKind kind,
            bool global
#if DEBUG
#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)
            , long listedAddress
#pragma warning restore CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)
#endif
            , ViewWriter nestedViewWriter)
        {
            return outerWriter.CreateRegion(
                offset,
                structOffset,
                fieldOffset,
                name,
                kind,
                global,
#if DEBUG
                listedAddress,
#endif
                this
            );
        }

        internal override RegionWriter CreateRegion(long offset, string name, ViewKind kind, bool global = false, ViewWriter nestedViewWriter = null)
        {
            return outerWriter.CreateRegion(offset, name, kind, global, this);
        }

        internal override RegionWriter CreateScopedRegion(
            long offset,
            long structOffset,
            int fieldOffset,
            string name,
            ViewKind kind,
            ViewKind scopeKind
#if DEBUG
#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)
            , int listedOffset
#pragma warning restore CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)
#endif
            , ViewWriter nestedViewWriter)
        {
            return outerWriter.CreateScopedRegion(
                offset,
                structOffset,
                fieldOffset,
                name,
                kind,
                scopeKind,
#if DEBUG
                listedOffset,
#endif
                this
            );
        }

        internal override void ExitRegion()
        {
            outerWriter.ExitRegion();
        }
    }
}
