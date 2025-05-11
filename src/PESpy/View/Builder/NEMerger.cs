#if PEFAST
using System;
using System.Collections.Generic;
using PESpy.NE;

namespace PESpy.View.Builder
{
    internal class NEMerger : Merger
    {
        private readonly NEFile neFile;

        public NEMerger(
            NEFile neFile,
            List<IView> sortedStructs,
            Extension extension) : base(sortedStructs, null, new List<DirectoryInfo>(), extension)
        {
            this.neFile = neFile;
        }

        internal override IView[] Merge()
        {
            var results = new List<IView>();

            var os2Header = neFile.OS2Header;

            //The NE Header may be followed by several additional sections at locations relative to the start of the NE Header itself

            var sizeOfHeaders = neFile.DosHeader.FileAddressOfNewExeHeader + ImageOS2Header.StructSize;

            var headerMetadata = new HeaderView(sizeOfHeaders, BuildSection(0, sizeOfHeaders));
            results.Add(headerMetadata);

            var lastSectionEnd = sizeOfHeaders;

            ReadTable("Segment Table",          tableOffset: os2Header.OffsetOfSegmentTable,      os2Header.OffsetOfResourceTable, os2Header, ref lastSectionEnd, results);
            ReadTable("Resource Table",         tableOffset: os2Header.OffsetOfResourceTable,     os2Header.OffsetOfResidentNameTable, os2Header, ref lastSectionEnd, results);
            ReadTable("Resident Name Table",    tableOffset: os2Header.OffsetOfResidentNameTable, os2Header.OffsetOfModuleReferenceTable, os2Header, ref lastSectionEnd, results);
            ReadTable("Module Reference Table", tableOffset: os2Header.OffsetOfModuleReferenceTable, os2Header.OffsetOfImportedNamesTable, os2Header, ref lastSectionEnd, results);
            ReadTable("Imported Names Table",   tableOffset: os2Header.OffsetOfImportedNamesTable, os2Header.OffsetOfEntryTable, os2Header, ref lastSectionEnd, results);
            ReadTable("Entry Table",            tableOffset: os2Header.OffsetOfEntryTable, os2Header.OffsetOfNonResidentNamesTable, os2Header, ref lastSectionEnd, results);

            //Non-Resident Name Table is last, so its length must be computed using a count, rather than
            //the position of the table after it
            ReadNonResidentNameTable(os2Header, ref lastSectionEnd, results);

            ReadSegmentData(os2Header, ref lastSectionEnd, results);

            ReadOMFData(lastSectionEnd, results);

            return results.ToArray();
        }

        private void ReadTable(
            string name,
            int tableOffset,
            int nextTableOffset,
            in ImageOS2Header os2Header,
            ref int lastSectionEnd,
            List<IView> results)
        {
            if (tableOffset == nextTableOffset)
                return; //Size is 0

            var start = os2Header.Offset + tableOffset;
            var length = nextTableOffset - tableOffset;
            var end = start + length;

            //Read any data that may exist between the main headers and the table. This shouldn't be possible, but you never know!
            ReadInterSectionData(lastSectionEnd, start, results);

            results.Add(new LogicalRegionView(start, name, BuildSection(start, end), ViewKind.Value, length));

            lastSectionEnd = end;
        }

        private void ReadNonResidentNameTable(in ImageOS2Header os2Header, ref int lastSectionEnd, List<IView> results)
        {
            if (os2Header.SizeOfNonResidentNameTable == 0)
                return; //There's no table after it, hence why there's an explicit size listed for it

            var start = os2Header.OffsetOfNonResidentNamesTable;
            var length = os2Header.SizeOfNonResidentNameTable;
            var end = start + length;

            //Read any data that may exist between the main headers and the table. This shouldn't be possible, but you never know!
            ReadInterSectionData(lastSectionEnd, start, results);

            results.Add(new LogicalRegionView(start, "Non-Resident Name Table", BuildSection(start, end), ViewKind.Value, length));

            lastSectionEnd = end;
        }

        private void ReadSegmentData(in ImageOS2Header os2Header, ref int lastSectionEnd, List<IView> results)
        {
            for (var i = 0; i < neFile.SegmentTable.Length; i++)
            {
                ref var segment = ref neFile.SegmentTable[i];
                var segmentStart = segment.ns_sector << os2Header.SegmentAlignmentShiftCount;
                var segmentLength = segment.ns_cbseg;
                var end = segmentStart + segmentLength;

                ReadInterSectionData(lastSectionEnd, segmentStart, results);

                var data = BuildSection(segmentStart, end);

                results.Add(new SectionView(segmentStart, $"Segment {i + 1}", data, segmentLength));

                lastSectionEnd = end;
            }
        }

        private void ReadOMFData(int lastSectionEnd, List<IView> results)
        {
            var omfData = neFile.OMFData;

            if (omfData != null)
            {
                if (lastSectionEnd < omfData.Offset)
                {
                    //There's some extra data prior to the beginning of the OMF data that we have to read.
                    //We don't read this as inter-section data

                    results.AddRange(BuildSection(lastSectionEnd, omfData.Offset));
                }

                //The rest of the file is OMF data
                var fileLength = (int) extension.GetInputLength();
                var omfLength = fileLength - omfData.Offset;

                string name;
                ViewKind kind;

                if (omfData is NB05Data d)
                {
                    name = $"{d.Sig} OMF Data";
                    kind = ViewKind.NB05Data;
                }
                else
                {
                    throw new NotImplementedException();
                }

                results.Add(new LogicalRegionView(omfData.Offset, name, BuildSection(omfData.Offset, fileLength, v => v, v => v, isOverlay: true), kind, omfLength));
            }
        }

        private void ReadInterSectionData(int lastSectionEnd, int start, List<IView> results)
        {
            //You can have data in between segments
            if (lastSectionEnd != -1 && start > lastSectionEnd)
            {
                var interSectionLength = start - lastSectionEnd;
                var children = BuildSection(lastSectionEnd, lastSectionEnd + interSectionLength, v => v, v => v);

                if (children.Length == 1)
                    results.Add(children[0]);
                else
                {
                    var interRegion = new LogicalRegionView(lastSectionEnd, "Inter-Section Data", children, ViewKind.Value, interSectionLength);
                    results.Add(interRegion);
                }
            }
        }
    }
}
#endif
