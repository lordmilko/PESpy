#if PEFAST
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace PESpy.View.Builder
{
    internal class LEMerger : Merger
    {
        private readonly LEFile leFile;

        public LEMerger(
            LEFile leFile,
            List<IView> sortedStructs,
            Extension extension) : base(sortedStructs, null, new List<DirectoryInfo>(), extension)
        {
            this.leFile = leFile;
        }

        internal override IView[] Merge()
        {
            var results = new List<IView>();

            //The LE Header may be followed by several additional sections at locations relative to the start of the LE Header itself

            var sizeOfHeaders = leFile.DosHeader.FileAddressOfNewExeHeader + ImageVXDHeader.StructSize;

            results.Add(new HeaderView(sizeOfHeaders, BuildSection(0, sizeOfHeaders)));

            var vxdHeader = leFile.VXDHeader;

            var lastSectionEnd = sizeOfHeaders;

            //Some offsets are relative to the beginning of the file, while others are relative to the beginning of the LE header
            var offsets = new[]
            {
                vxdHeader.OffsetOfObjectTable,
                vxdHeader.OffsetOfObjectPageMap,
                vxdHeader.OffsetOfResourceTable,
                vxdHeader.OffsetOfResidentNameTable,
                vxdHeader.OffsetOfEntryTable,
                vxdHeader.OffsetOfModuleDirectiveTable,
                //Resident Directives Data?
                vxdHeader.OffsetOfPerPageChecksumTable,
                vxdHeader.OffsetOfFixupPageTable,
                vxdHeader.OffsetOfFixupRecordTable,
                vxdHeader.OffsetOfImportModuleNameTable,
                vxdHeader.OffsetOfEnumeratedDataPages   != 0 ? vxdHeader.OffsetOfEnumeratedDataPages   - vxdHeader.Offset : 0, //Preload pages? Demand load pages too?
                vxdHeader.OffsetOfIteratedDataMap       != 0 ? vxdHeader.OffsetOfIteratedDataMap       - vxdHeader.Offset : 0,
                vxdHeader.OffsetOfNonResidentNamesTable != 0 ? vxdHeader.OffsetOfNonResidentNamesTable - vxdHeader.Offset : 0,
                vxdHeader.OffsetOfDebugInfo
            };

#if DEBUG
            for (var i = 1; i < offsets.Length; i++)
            {
                var current = offsets[i];
                var previous = offsets[i - 1];

                Debug.Assert(current == 0 || current >= previous);
            }
#endif
            int index = 0;
            ReadTable("Object Table",             offsets, vxdHeader, ref index, ref lastSectionEnd, results);
            ReadTable("Object Page Map",          offsets, vxdHeader, ref index, ref lastSectionEnd, results);
            ReadTable("Resource Table",           offsets, vxdHeader, ref index, ref lastSectionEnd, results);
            ReadTable("Resident Name Table",      offsets, vxdHeader, ref index, ref lastSectionEnd, results);
            ReadTable("Entry Table",              offsets, vxdHeader, ref index, ref lastSectionEnd, results);
            ReadTable("Module Directive Table",   offsets, vxdHeader, ref index, ref lastSectionEnd, results);
            ReadTable("Per-Page Checksum",        offsets, vxdHeader, ref index, ref lastSectionEnd, results);
            ReadTable("Fixup Page Table",         offsets, vxdHeader, ref index, ref lastSectionEnd, results);
            ReadTable("Fixup Record Table",       offsets, vxdHeader, ref index, ref lastSectionEnd, results);
            ReadTable("Import Module Name Table", offsets, vxdHeader, ref index, ref lastSectionEnd, results);
            ReadTable("Enumerated Data Pages",    offsets, vxdHeader, ref index, ref lastSectionEnd, results);
            ReadTable("Iterated Data Map",        offsets, vxdHeader, ref index, ref lastSectionEnd, results);
            ReadTable("Non-Resident Names Table", offsets, vxdHeader, ref index, ref lastSectionEnd, results);
            ReadLastTable("Debug Info",           offsets, vxdHeader, ref index, ref lastSectionEnd, results, vxdHeader.DebugInfoLength);
            Debug.Assert(index == offsets.Length);

            return results.ToArray();
        }

        private void ReadTable(string name, int[] offsets, in ImageVXDHeader vxdHeader, ref int index, ref int lastSectionEnd, List<IView> results)
        {
            var current = offsets[index];
            index++;

            if (current == 0)
                return;

            var j = index; //We already did index + 1 above

            //Our length goes up to the next item that exists. So if an item has a length of 0 we need to skip over it. This assumes that the last item
            //does not have an offset of 0 (which would mess things up, if an earlier one is looking for its end and none was found)
            for (; j < offsets.Length; j++)
            {
                if (offsets[j] != 0)
                    break;
            }

            int next;

            if (j != offsets.Length)
            {
                next = offsets[j];

                if (current == next)
                    return; //current == next, which means current is empty
            }
            else
                next = (int) extension.GetInputLength() - vxdHeader.Offset;

            //Some offsets are relative to the start of the EXE file, others are relative to the beginning of the LE header
            var start = vxdHeader.Offset + current;
            var length = next - current;
            var end = start + length;

            //Read any data that may exist between the main headers and the table. This shouldn't be possible, but you never know!
            NEMerger.ReadInterSectionData(lastSectionEnd, start, this, results);

            results.Add(new LogicalRegionView(start, name, BuildSection(start, end), ViewKind.Value, length));

            lastSectionEnd = end;
        }

        private void ReadLastTable(string name, int[] offsets, in ImageVXDHeader vxdHeader, ref int index, ref int lastSectionEnd, List<IView> results, int length)
        {
            var current = offsets[index];
            Debug.Assert(index == offsets.Length - 1); //This should be the last entry
            index++;

            if (current == 0)
                return;

            var start = vxdHeader.Offset + current;
            var end = start + length;

            //Read any data that may exist between the main headers and the table. This shouldn't be possible, but you never know!
            NEMerger.ReadInterSectionData(lastSectionEnd, start, this, results);

            results.Add(new LogicalRegionView(start, name, BuildSection(start, end), ViewKind.Value, length));

            lastSectionEnd = end;
        }
    }
}
#endif
