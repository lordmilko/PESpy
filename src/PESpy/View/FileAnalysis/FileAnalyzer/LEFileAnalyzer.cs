namespace PESpy.View
{
    internal class LEFileAnalyzer : FileAnalyzer
    {
        private readonly LEFile _leFile;

        internal LEFileAnalyzer(
            LEFileAccessor fileAccessor,
            in FileAnalyzerOptions options) : base(fileAccessor, options)
        {
            _leFile = fileAccessor.LEFile;
        }

        protected override ViewWriter CreateViewWriter()
        {
            //CreateViewWriter is called by the base ctor
            var leFile = (LEFile) _fileAccessor.File;

            return new ViewByteViewWriter(
                new SimpleViewWriterHelper(leFile),
                leFile.CreateByteViewProvider(_fileAccessor),
                ViewMode.Default,
                _fileAccessor,
                null,
                this,
                LocatorHttpPolicy.None,
                _progress
            );
        }

        public override void Execute() => ExecuteCode();

        protected override void DiscoverCodeRoots()
        {
            //Not yet implemented
#if FALSE
            //Anything with OBJEXEC is code.

            var entryPoint = _leFile.EntryPoint;

            AddCode(entryPoint, _leFile.VXDHeader.e32_eip);

            //Just dump all objects that contain code?

            var objectTable = _leFile.ObjectTable;

            var objIndex = 1;

            foreach (var obj in objectTable)
            {
                if ((obj.o32_flags & ObjectTableFlags.OBJEXEC) != 0)
                {
                    //What if the object doesn't start with code? NT 4 just seems to dump
                    //from the start

                    //Maybe this is the address to set as the IP
                    var addr = obj.o32_base;

                    if ((obj.o32_flags & ObjectTableFlags.OBJALIAS16) != 0)
                    {
                        //NT4 shifts the object index to the left 16 and sets that as the addr?
                        //What's up with that
                        addr = objIndex << 16;
                    }

                    var physicalAddr = _leFile.GetPhysicalOffset(obj, 0);
                    AddCode(physicalAddr, addr);
                }

                objIndex++;
            }

            //Ostensibly, there should be exports that point to code,
            //but in a VXD there's only meant to be one export, which points to the DDB

            var items = _leFile.NonResidentNamesTable;
            
            var entryTable = _leFile.EntryTable;

            foreach (var item in items)
            {
                if (item.Ordinal > 0)
                {
                    var bundle = entryTable[item.Ordinal - 1];

                    var obj = objectTable[bundle.b32_obj - 1];

                    if ((obj.o32_flags & ObjectTableFlags.OBJEXEC) != 0)
                    {
                        switch (bundle.b32_type)
                        {
                            case E32BundleType.ENTRY32:
                                foreach (var entry in bundle.Entries)
                                {
                                    var physicalAddress = _leFile.GetPhysicalOffset(obj, entry.e32_variant.offset.offset32);
                                    AddCode(physicalAddress, entry.e32_variant.offset.offset32);
                                }
                                break;

                            default:
                                throw new NotImplementedException();
                        }
                    }
                }
            }
#endif
        }

        protected override void MarkRegions()
        {
            //The LE Header may be followed by several additional sections at locations relative to the start of the LE Header itself

            var sizeOfHeaders = _leFile.DosHeader.e_lfanew + ImageVXDHeader.StructSize;

            _extraRegions.Add(new RegionBuilder
            {
                Name = "HEADER",
                Kind = ViewKind.Header,
                Start = 0,
                End = sizeOfHeaders
            });

            var vxdHeader = _leFile.VXDHeader;

            var lastSectionEnd = sizeOfHeaders;

            foreach (var tableBound in _leFile.tableBounds)
            {
                if (!tableBound.IsPresent)
                    continue;

                if (tableBound.StartOffset != lastSectionEnd)
                {
                    //Read any data that may exist between the main headers and the table. This shouldn't be possible, but you never know!
                    MarkInterSectionData(lastSectionEnd, tableBound.StartOffset, canHaveRelocations: false);
                }

                SplitRegionBounds(tableBound.StartOffset, tableBound.EndOffset);

                _extraRegions.Add(new RegionBuilder
                {
                    Name = tableBound.Name,
                    Kind = tableBound.Kind,
                    Start = tableBound.StartOffset,
                    End = tableBound.EndOffset
                });

                lastSectionEnd = tableBound.EndOffset;
            }
        }
    }
}
