using PESpy.View;

namespace PESpy
{
    [Source(SourceKind.ehdata4_export_h)]
    public readonly struct UnwindMapEntry4 : IViewableValue
    {
        /// <summary>
        /// State this action takes us to (in offset form, unlike FH3!)
        /// </summary>
        public int nextOffset { get; }

        /// <summary>
        /// Type of entry
        /// </summary>
        public Type type { get; }

        /// <summary>
        /// Image-relative offset of action, exists for all NoUW entry types
        /// </summary>
        public int action { get; }

        /// <summary>
        /// Frame offset of object pointer to be destroyed, exists for DtorWithObj and DtorWithPtrToObj types
        /// </summary>
        public int @object { get; }

        public int Offset { get; }

        internal int StructSize
        {
            get
            {
                var nextOffsetAndType = ((int) nextOffset << 2) | (int) type;
                var size = FuncInfo4.GetLength((uint) nextOffsetAndType);

                switch (type)
                {
                    case Type.DtorWithObj:
                    case Type.DtorWithPtrToObj:
                        size += sizeof(int); //action
                        size += FuncInfo4.GetLength((uint) @object);
                        break;

                    case Type.RVA:
                        size += sizeof(int); //action
                        break;
                }

                return size;
            }
        }

        internal unsafe UnwindMapEntry4(int offset, ref byte* pData)
        {
            Offset = offset;
            var nextOffsetAndType = FuncInfo4.ReadUnsigned(ref pData);
            nextOffset = (int) (nextOffsetAndType >> 2);
            type = (Type) (nextOffsetAndType & 3);

            switch (type)
            {
                case Type.DtorWithObj:
                case Type.DtorWithPtrToObj:
                    action = FuncInfo4.ReadInt(ref pData);
                    @object = (int) FuncInfo4.ReadUnsigned(ref pData);
                    break;

                case Type.RVA:
                    action = FuncInfo4.ReadInt(ref pData);
                    break;
            }
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals, but we do have xrefs

            int nextOffsetAndType;
            int read;

            //Multiple RUNTIME_FUNCTION entries could point to a given FuncInfo, which means we could potentially end up with duplicates.
            //These particular RVAs don't encode anything specific about the RUNTIME_FUNCTION.BeginAddress we're working with

            switch (type)
            {
                case Type.DtorWithObj:
                case Type.DtorWithPtrToObj:
                    nextOffsetAndType = ((int) nextOffset << 2) | (int) type;
                    read = FuncInfo4.GetLength((uint) nextOffsetAndType);

                    writer.WriteUniqueRVAXRef(Offset, read, action);
                    read += sizeof(int);

                    writer.WriteUniqueRVAXRef(Offset, read, @object);
                    break;

                case Type.RVA:
                    nextOffsetAndType = ((int) nextOffset << 2) | (int) type;
                    read = FuncInfo4.GetLength((uint) nextOffsetAndType);

                    writer.WriteUniqueRVAXRef(Offset, read, action);
                    break;
            }
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.UnwindMapEntry4, this, ViewKind.UnwindMapEntry4, StructSize);

        int IViewable.NumChildren() => throw StructWriter.GetEagerLoadOnlyException();

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            if (index != -1)
                throw StructWriter.GetEagerLoadOnlyException();

            using var s = structWriter.CreateEagerWriter();

            var nextOffsetAndType = ((int) nextOffset << 2) | (int) type;
            var nextOffsetAndTypeLength = FuncInfo4.GetLength((uint) nextOffsetAndType);

            using (var b = s.WriteBitFields(2, nextOffsetAndTypeLength))
            {
                b.WriteField(nameof(type), type, 2);
                b.WriteField(nameof(nextOffset), nextOffset, (nextOffsetAndTypeLength * 8) - 2);
            }

            switch (type)
            {
                case Type.DtorWithObj:
                case Type.DtorWithPtrToObj:
                    s.WriteField(nameof(action), action, sizeof(int));

                    //The "@" is not included in nameof
                    s.WriteField(nameof(@object), @object, FuncInfo4.GetLength((uint) @object));
                    break;

                case Type.RVA:
                    s.WriteField(nameof(action), action);
                    break;
            }

            structWriter.EagerFields = s.ToArray();
        }

        public enum Type
        {
            /// <summary>
            /// No unwind action associated with this state
            /// </summary>
            NoUW = 0,

            /// <summary>
            /// Dtor with an object offset
            /// </summary>
            DtorWithObj = 1,

            /// <summary>
            /// Dtor with an offset that contains a pointer to the object to be destroyed
            /// </summary>
            DtorWithPtrToObj = 2,

            /// <summary>
            /// Dtor that has a direct function that is called that knows where the object is and can perform more exotic destruction
            /// </summary>
            RVA = 3
        }
    }
}
