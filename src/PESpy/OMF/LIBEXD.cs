using System.Diagnostics;
using PESpy.View;

namespace PESpy.OMF
{
    public readonly unsafe struct LIBEXD : IViewable
    {
        private readonly byte* value;

        public OMFRecordType RecordType => *(OMFRecordType*) value;

        public ushort RecordLength => *(ushort*) (value + 1);

        public ushort NumModulesInLibrary => *(ushort*) (value + 3);

        public NativeSpan<Entry> ModuleTable => new NativeSpan<Entry>(value + 5, NumModulesInLibrary + 1); //Last entry is null

        public ModuleDependencyList[] ModuleDependencyLists
        {
            get
            {
                var moduleTable = ModuleTable;

                var results = new ModuleDependencyList[moduleTable.Length - 1];

                //Last entry is null
                for (var i = 0; i < moduleTable.Length - 1; i++)
                {
                    /* The spec says that each OffsetToRequiredModules is relative to the "start of the extended dictionary",
                     * which is defined as being just before the count of the number of modules in the library, which occurs
                     * after the first 3 bytes that describe the record type (LIBEXD) and the total length of the record. However,
                     * it seems to me that this is wrong. When you look at all of the remaining bytes after parsing the module table,
                     * the first OffsetToRequiredModules would point into the terminating null module table entry if you were to just
                     * do OffsetToRequiredModules + 3. Therefore, I conclude that the module dependency list must be value+5, not value+3 */

                    results[i] = new ModuleDependencyList(value + moduleTable[i].OffsetToRequiredModules + sizeof(byte) + sizeof(short) + sizeof(short));
                }

                return results;
            }
        }

        public LIBEXD(byte* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(this, ViewKind.LIBEXD, OMFRecord.GetStructSize(value));

        int IViewable.NumChildren() => OMFRecord.GetDefaultNumChildrenNoChecksum();

        void IViewable.WriteChild(int index, ref StructWriter structWriter) =>
            OMFRecord.WriteDefaultChild(new OMFRecord(value), index, ref structWriter);

        public override string ToString()
        {
            return RecordType.ToString();
        }

        [DebuggerDisplay("ModulePage = {ModulePage}, OffsetToRequiredModules = {OffsetToRequiredModules}")]
        public readonly struct Entry
        {
            public readonly ushort ModulePage;
            public readonly ushort OffsetToRequiredModules;
        }

        [DebuggerDisplay("Length = {Length}")]
        public readonly struct ModuleDependencyList
        {
            private readonly byte* value;

            public ushort Length => *(ushort*) value;

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public NativeSpan<ushort> Dependencies => new NativeSpan<ushort>(value + sizeof(short), Length);

            public ModuleDependencyList(byte* value)
            {
                this.value = value;
            }
        }
    }
}
