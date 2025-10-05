using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using PESpy.Native;
using PESpy.View;

namespace PESpy
{
    /// <summary>
    /// Represents the <see cref="IMAGE_EXPORT_DIRECTORY"/> structure, that describes the locations and components of the export table within the image.
    /// </summary>
    [DebuggerDisplay("Exports = {Exports.Length}")]
    public class ImageExportDirectory : IValue, IViewable //Structs return copies from properties, and ref properties don't display properly in the debugger
    {
        private const int NameOffset = 12;

        /// <summary>
        /// Reserved, must be 0.
        /// </summary>
        public int Characteristics => chunk.PeekInt32(0);

        /// <summary>
        /// The time and date that the export data was created.<para/>
        /// If this <see cref="PEFile"/> has an <see cref="ImageDebugDirectory"/> whose type is <see cref="ImageDebugType.Reproducible"/>, this value is not a TimeDateStamp, but rather a
        /// checksum derived from the executable's file contents, whose algorithm is an implementation detail of the tool that produced the file.
        /// </summary>
        public Timestamp TimeDateStamp => chunk.PeekUInt32(4);

        /// <summary>
        /// The major version number. The major and minor version numbers can be set by the user.
        /// </summary>
        public ushort MajorVersion => chunk.PeekUInt16(8);

        /// <summary>
        /// The minor version number.
        /// </summary>
        public ushort MinorVersion => chunk.PeekUInt16(10);

        private RVA<AnsiString>? lazyName;

        /// <summary>
        /// The address of the ASCII string that contains the name of the DLL. This address is relative to the image base.
        /// </summary>
        public RVA<AnsiString> Name
        {
            get
            {
                if (lazyName == null)
                {
                    var rva = chunk.PeekInt32(NameOffset);

                    if (chunk.PEFile().TryGetValueChunkFromSection(rva, out var valueChunk))
                    {
                        var str = valueChunk.PeekAnsiNullTerminatedString(0);
                        lazyName = new RVA<AnsiString>(rva, valueChunk.AbsoluteOffset, str);
                    }
                    else
                    {
                        lazyName = new RVA<AnsiString>(rva);
                    }
                }

                return lazyName.Value;
            }
        }

        /// <summary>
        /// The starting ordinal number for exports in this image. This field specifies the starting ordinal number for the export address table. It is usually set to 1.
        /// </summary>
        public int Base => chunk.PeekInt32(16);

        /// <summary>
        /// The number of entries in the export address table.
        /// </summary>
        public int NumberOfFunctions => chunk.PeekInt32(20);

          /// <summary>
        /// The number of entries in the name pointer table. This is also the number of entries in the ordinal table.
        /// </summary>
        public int NumberOfNames => chunk.PeekInt32(24);

        private RVA<ForwardOrAddress[]>? lazyAddressOfFunctions;

        /// <summary>
        /// The address of the export address table, relative to the image base.
        /// </summary>
        public RVA<ForwardOrAddress[]> AddressOfFunctions
        {
            get
            {
                if (lazyAddressOfFunctions == null)
                    lazyAddressOfFunctions = GetAddressOfFunctions(chunk.PeekInt32(28));

                return lazyAddressOfFunctions.Value;
            }
        }

        private RVA<RVA<AnsiString>[]>? lazyAddressOfNames;

        /// <summary>
        /// The address of the export name pointer table, relative to the image base. The table size is given by the Number of Name Pointers field.
        /// </summary>
        public RVA<RVA<AnsiString>[]> AddressOfNames
        {
            get
            {
                if (lazyAddressOfNames == null)
                    lazyAddressOfNames = GetAddressOfNames(chunk.PeekInt32(32));

                return lazyAddressOfNames.Value;
            }
        }

        private RVA<ushort[]>? lazyAddressOfNameOrdinals;

        /// <summary>
        /// The address of the ordinal table, relative to the image base.
        /// </summary>
        public RVA<ushort[]> AddressOfNameOrdinals
        {
            get
            {
                if (lazyAddressOfNameOrdinals == null)
                    lazyAddressOfNameOrdinals = GetAddressOfNameOrdinals(chunk.PeekInt32(36));

                return lazyAddressOfNameOrdinals.Value;
            }
        }

        private Export[]? exports;

        /// <summary>
        /// Gets the exports defined in this <see cref="ImageExportDirectory"/>.<para/>
        /// This field is not part of <see cref="IMAGE_EXPORT_DIRECTORY"/>, and merely aggregates the separate pieces of information
        /// contained in <see cref="AddressOfFunctions"/>, <see cref="AddressOfNames"/> and <see cref="AddressOfNameOrdinals"/>.<para/>
        /// If any of these three fields do not point to a valid RVA, this property will return an empty array.
        /// </summary>
        public Export[] Exports
        {
            get
            {
                if (exports == null)
                    exports = GetExports();

                return exports;
            }
        }

        public int Offset => chunk.AbsoluteOffset;

        public const int StructSize =
            sizeof(int) + //Characteristics
            sizeof(int) + //TimeDateStamp
            sizeof(ushort) + //MajorVersion
            sizeof(ushort) + //MinorVersion
            sizeof(int) + //Name
            sizeof(int) + //Base
            sizeof(int) + //NumberOfFunctions
            sizeof(int) + //NumberOfNames
            sizeof(int) + //AddressOfFunctions
            sizeof(int) + //AddressOfNames
            sizeof(int); //AddressOfNameOrdinals

        private readonly MemoryChunk chunk;

        internal ImageExportDirectory(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        private RVA<ForwardOrAddress[]> GetAddressOfFunctions(int addressOfFunctions)
        {
            var peFile = chunk.PEFile();

            if (peFile.TryGetValueChunkFromSection(addressOfFunctions, out var addressOfFunctionsChunk))
            {
                var exportTableDirectory = peFile.OptionalHeader.ExportTableDirectory;

                var exportTableStart = exportTableDirectory.VirtualAddress;
                var exportTableEnd = exportTableStart + exportTableDirectory.Size;

                var numberOfFunctions = NumberOfFunctions;

                var functionAddresses = addressOfFunctionsChunk.PeekSpan<int>(0, numberOfFunctions);

                var addressAndForwarderNames = new ForwardOrAddress[numberOfFunctions];

                for (var i = 0; i < numberOfFunctions; i++)
                {
                    //If the actual address of the function is within the bounds of the export table (rather than a random place
                    //in the module) that means that a. addressOfFunction is pointing to a string and b. function actually comes
                    //from another module. Lookup _that_ external module.
                    //https://reverseengineering.stackexchange.com/questions/16023/exports-that-redirects-to-other-library

                    var functionAddress = functionAddresses[i];

                    if (functionAddress >= exportTableStart && functionAddress <= exportTableEnd)
                    {
                        //It's a name
                        if (peFile.TryGetValueChunkFromSection(functionAddress, out var valueChunk))
                        {
                            //Even if we're not holding onto the chunk, the memory that the name points to will still exist
                            var redirectName = valueChunk.PeekAnsiNullTerminatedString(0);
                            addressAndForwarderNames[i] = new ForwardOrAddress(new RVA<AnsiString>(functionAddress, valueChunk.AbsoluteOffset, redirectName));
                        }
                        else
                        {
                            //Bad
                            addressAndForwarderNames[i] = new ForwardOrAddress(functionAddress);
                        }
                    }
                    else
                    {
                        //It's just an address
                        addressAndForwarderNames[i] = new ForwardOrAddress(functionAddress);
                    }
                }

                return new RVA<ForwardOrAddress[]>(addressOfFunctions, addressOfFunctionsChunk.AbsoluteOffset, addressAndForwarderNames);
            }
            else
            {
                //Bad
                return new RVA<ForwardOrAddress[]>(addressOfFunctions);
            }
        }

        private RVA<RVA<AnsiString>[]> GetAddressOfNames(int addressOfNames)
        {
            var peFile = chunk.PEFile();

            if (peFile.TryGetValueChunkFromSection(addressOfNames, out var addressOfNamesChunk))
            {
                var numberOfNames = NumberOfNames;

                var functionNameAddresses = addressOfNamesChunk.PeekSpan<int>(0, numberOfNames);

                var names = new RVA<AnsiString>[numberOfNames];

                for (var i = 0; i < numberOfNames; i++)
                {
                    var functionNameAddress = functionNameAddresses[i];

                    //Often the section.PointerToRawData will cause the functionNameAddress RVA to be correct
                    //in both loaded and unloaded images, but this is a gotcha: often, but not always!
                    if (peFile.TryGetValueChunkFromSection(functionNameAddress, out var nameChunk))
                    {
                        var functionName = nameChunk.PeekAnsiNullTerminatedString(0);
                        names[i] = new RVA<AnsiString>(functionNameAddress, nameChunk.AbsoluteOffset, functionName);
                    }
                    else
                    {
                        //Bad
                        names[i] = new RVA<AnsiString>(functionNameAddress);
                    }
                }

                return new RVA<RVA<AnsiString>[]>(addressOfNames, addressOfNamesChunk.AbsoluteOffset, names);
            }
            else
            {
                //Bad
                return new RVA<RVA<AnsiString>[]>(addressOfNames);
            }
        }

        private RVA<ushort[]> GetAddressOfNameOrdinals(int addressOfNameOrdinals)
        {
            var peFile = chunk.PEFile();

            if (peFile.TryGetValueChunkFromSection(addressOfNameOrdinals, out var addressOfNameOrdinalsChunk))
            {
                var numberOfNames = NumberOfNames;

                var functionOrdinals = addressOfNameOrdinalsChunk.PeekSpan<ushort>(0, numberOfNames);

                return new RVA<ushort[]>(addressOfNameOrdinals, addressOfNameOrdinalsChunk.AbsoluteOffset, functionOrdinals.ToArray());
            }
            else
            {
                //Bad
                return new RVA<ushort[]>(addressOfNameOrdinals);
            }
        }

        private Export[] GetExports()
        {
            if (AddressOfFunctions.IsValid && AddressOfNames.IsValid && AddressOfNameOrdinals.IsValid)
            {
                var exports = new Export[NumberOfFunctions];

                //Function ordinals are not guaranteed to be in order, so we need to store them in a map. While we're at it,
                //we may as well map each ordinal to the name of the function it corresponds to (rather than just mapping the ordinal
                //to the index at which it resides)
                var ordinalToNameAddressMap = new Dictionary<int, RVA<AnsiString>>();

                for (var i = 0; i < AddressOfNameOrdinals.Value.Length; i++)
                    ordinalToNameAddressMap.Add(AddressOfNameOrdinals.Value[i], AddressOfNames.Value[i]);

                for (var i = 0; i < AddressOfFunctions.Value.Length; i++)
                {
                    /* Exports can be exported by ordinal, or by both name and ordinal. The ordinal value of a function has no relation
                     * to its actual position within the ordinal list */

                    var functionAddressOrName = AddressOfFunctions.Value[i];
                    var ordinalPlusBase = i + Base;

                    AnsiString exportName = default;

                    if (ordinalToNameAddressMap.TryGetValue(i, out var nameValue) && nameValue.IsValid)
                        exportName = nameValue.Value;

                    if (functionAddressOrName.IsForward)
                    {
                        //It's a forwarder. e.g. "NTDLL.RtlAcquireSRWLockExclusive"
                        exports[i] = new Export(exportName, i, functionAddressOrName, ordinalPlusBase);
                    }
                    else
                    {
                        //It's a regular export

                        //It's not our job to transform the address into an absolute address; if the caller wants that, they need to add in the base address. This also solves the problem that
                        //the ImageBase in a PE file may not actually be the address that the file was loaded at (if it was memory mapped outside of LoadLibrary)
                        exports[i] = new Export(exportName, i, functionAddressOrName, ordinalPlusBase);
                    }
                }

                return exports;
            }
            else
                return Array.Empty<Export>();
        }

        public bool TryGetExport(string name, out Export export)
        {
            export = default;

            if (exports != null)
            {
                //We already have exports, try and match against one that already exists

                foreach (var item in exports)
                {
                    if (item.Name == name)
                    {
                        export = item;
                        return true;
                    }
                }

                return false;
            }

            var names = AddressOfNames.ValueOrDefault;

            if (names == null)
                return false;

            for (var i = 0; i < names.Length; i++)
            {
                var itemName = names[i];

                if (itemName.IsValid && itemName.Value == name)
                    return TryProcessExportAtIndex(itemName.Value, i, out export);
            }

            return false;
        }

        private bool TryProcessExportAtIndex(AnsiString name, int i, out Export export)
        {
            //The name of this function exists at index "i". The ordinal that is also at index "i"
            //is the index into the AddressOfFunctions array that contains our function address
            var addressOfNameOrdinals = chunk.PeekInt32(36);

            var peFile = chunk.PEFile();

            if (peFile.TryGetValueChunkFromSection(addressOfNameOrdinals, out var addressOfNameOrdinalsChunk))
            {
                var ordinal = addressOfNameOrdinalsChunk.PeekUInt16(i * sizeof(short));

                var addressOfFunctions = chunk.PeekInt32(28);

                if (peFile.TryGetValueChunkFromSection(addressOfFunctions, out var addressOfFunctionsChunk))
                {
                    var functionAddress = addressOfFunctionsChunk.PeekInt32(ordinal * sizeof(int));

                    //We got the address, but now the question is: is it a forwarder or not!
                    var exportTableStart = peFile.OptionalHeader.ExportTableDirectory.VirtualAddress;
                    var exportTableEnd = exportTableStart + peFile.OptionalHeader.ExportTableDirectory.Size;

                    ForwardOrAddress forwardOrAddress;

                    if (functionAddress >= exportTableStart && functionAddress <= exportTableEnd)
                    {
                        //It's a name
                        if (peFile.TryGetValueChunkFromSection(functionAddress, out var nameChunk))
                        {
                            var redirectName = nameChunk.PeekAnsiNullTerminatedString(0);
                            forwardOrAddress = new ForwardOrAddress(new RVA<AnsiString>(functionAddress, nameChunk.AbsoluteOffset, redirectName));
                        }
                        else
                        {
                            //Bad
                            forwardOrAddress = new ForwardOrAddress(functionAddress);
                        }
                    }
                    else
                    {
                        //It's just an address
                        forwardOrAddress = new ForwardOrAddress(functionAddress);
                    }

                    export = new Export(name, ordinal, forwardOrAddress, ordinal + Base);
                    return true;
                }
            }

            export = default;
            return false;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            writer.WriteRVAAnsiNullTerminatedField(Name, ViewKind.ImageExportDirectory_Name, fieldOffset: NameOffset);

            if (AddressOfFunctions.IsValid)
            {
                using var r = writer.CreateRegion(AddressOfFunctions.ActualOffset, "Export Address Table", ViewKind.ExportAddressTable);

                for (var i = 0; i < AddressOfFunctions.Value.Length; i++)
                {
                    var value = AddressOfFunctions.Value[i];

                    if (value.IsForward)
                        r.WriteAnsiNullTerminatedValue(value.ForwardName);
                    else
                        r.WriteValue((int) value.Address);
                }
            }

            if (AddressOfNames.IsValid)
            {
                using var r = writer.CreateRegion(AddressOfNames.ActualOffset, "Export Names Table", ViewKind.ExportNamesTable);

                for (var i = 0; i < AddressOfNames.Value.Length; i++)
                {
                    var value = AddressOfNames.Value[i];

                    r.WriteAnsiNullTerminatedValue(value);
                }
            }

            if (AddressOfNameOrdinals.IsValid)
            {
                using var r = writer.CreateRegion(AddressOfNameOrdinals.ActualOffset, "Export Ordinals Table", ViewKind.ExportOrdinalsTable);

                r.WriteValues(AddressOfNameOrdinals.Value);
            }
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.IMAGE_EXPORT_DIRECTORY, this, ViewKind.ImageExportDirectory, StructSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(Characteristics), Characteristics);
            s.WriteField(nameof(TimeDateStamp), TimeDateStamp);
            s.WriteField(nameof(MajorVersion), MajorVersion);
            s.WriteField(nameof(MinorVersion), MinorVersion);
            s.WriteRVAAnsiNullTerminatedField(nameof(Name), Name);
            s.WriteField(nameof(Base), Base);
            s.WriteField(nameof(NumberOfFunctions), NumberOfFunctions);
            s.WriteField(nameof(NumberOfNames), NumberOfNames);
            s.WriteField(nameof(AddressOfFunctions), (int) AddressOfFunctions.ListedOffset);
            s.WriteField(nameof(AddressOfNames), (int) AddressOfNames.ListedOffset);
            s.WriteField(nameof(AddressOfNameOrdinals), (int) AddressOfNameOrdinals.ListedOffset);

            return s.ToArray();
        }

        /// <summary>
        /// Represents either the address of a function, or the name of a function that this export forwards to.<para/>
        /// This structure models the fact that an export can point to one of two different types of data, and does not have a native equivalent.
        /// </summary>
        public readonly struct ForwardOrAddress
        {
            public RVA<AnsiString> ForwardName { get; }

            /// <summary>
            /// Gets the relative virtual address of the function that this forwarder points to.
            /// </summary>
            public int Address { get; }

            public bool IsForward { get; }

            public ForwardOrAddress(in RVA<AnsiString> name)
            {
                ForwardName = name;
                Address = 0;
                IsForward = true;
            }

            public ForwardOrAddress(int address)
            {
                Address = address;
                ForwardName = default;
                IsForward = false;
            }

            public override string ToString()
            {
                if (IsForward)
                    return ForwardName.ToString();

                return "0x" + Address.ToString("X");
            }
        }

        /// <summary>
        /// Encapsulates the details of a function export.<para/>
        /// This structure aggregates information from several different sources, and does not have a native equivalent.
        /// </summary>
        [DebuggerDisplay("{DebuggerDisplay,nq}")]
        public readonly struct Export
        {
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            private string DebuggerDisplay
            {
                get
                {
                    var builder = new StringBuilder();

                    var name = Name.ToString();

                    if (!string.IsNullOrEmpty(name))
                        builder.Append($"[{Ordinal}] {name}");
                    else
                        builder.Append($"[{Ordinal}]");

                    if (ForwardOrAddress.IsForward)
                    {
                        builder.Append(" -> ");
                        builder.Append(ForwardOrAddress.ToString());
                    }

                    return builder.ToString();
                }
            }

            public AnsiString Name { get; }

            public int Index { get; }

            /// <summary>
            /// Gets the relative virtual address of the function that this export points to,
            /// or the module qualified name that this export forwards to.
            /// </summary>
            public ForwardOrAddress ForwardOrAddress { get; }

            public int Ordinal { get; }

            public Export(AnsiString name, int index, ForwardOrAddress nameOrAddress, int ordinal)
            {
                Name = name;
                Index = index;
                ForwardOrAddress = nameOrAddress;
                Ordinal = ordinal;
            }

            public override string ToString()
            {
                var name = Name.ToString();

                if (!string.IsNullOrEmpty(name))
                    return name!;

                return Ordinal.ToString();
            }
        }
    }
}
