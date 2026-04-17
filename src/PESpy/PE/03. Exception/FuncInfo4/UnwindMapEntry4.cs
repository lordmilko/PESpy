namespace PESpy
{
    [Source(SourceKind.ehdata4_export_h)]
    public readonly struct UnwindMapEntry4
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

        internal unsafe UnwindMapEntry4(int offset, ref byte* pData)
        {
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
