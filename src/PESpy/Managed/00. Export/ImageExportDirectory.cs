using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
using RVA = System.Int32;
#endif

namespace PESpy
{
    /// <summary>
    /// Represents the <see cref="IMAGE_EXPORT_DIRECTORY"/> structure, that describes the locations and components of the export table within the image.
    /// </summary>
    [DebuggerDisplay("Exports = {Exports.Length}")]
    public class ImageExportDirectory : IValue, IViewable //Structs return copies from properties, and ref properties don't display properly in the debugger
    {
        /// <summary>
        /// Reserved, must be 0.
        /// </summary>
        public int Characteristics { get; init; }

        /// <summary>
        /// The time and date that the export data was created.<para/>
        /// If this <see cref="PEFile"/> has an <see cref="ImageDebugDirectory"/> whose type is <see cref="ImageDebugType.Reproducible"/>, this value is not a TimeDateStamp, but rather a
        /// checksum derived from the executable's file contents, whose algorithm is an implementation detail of the tool that produced the file.
        /// </summary>
        public uint TimeDateStamp { get; init; }

        /// <summary>
        /// The major version number. The major and minor version numbers can be set by the user.
        /// </summary>
        public ushort MajorVersion { get; init; }

        /// <summary>
        /// The minor version number.
        /// </summary>
        public ushort MinorVersion { get; init; }

        /// <summary>
        /// The address of the ASCII string that contains the name of the DLL. This address is relative to the image base.
        /// </summary>
        public RVA<string> Name { get; init; }

        /// <summary>
        /// The starting ordinal number for exports in this image. This field specifies the starting ordinal number for the export address table. It is usually set to 1.
        /// </summary>
        public int Base { get; init; }

        /// <summary>
        /// The number of entries in the export address table.
        /// </summary>
        public int NumberOfFunctions { get; init; }

        /// <summary>
        /// The number of entries in the name pointer table. This is also the number of entries in the ordinal table.
        /// </summary>
        public int NumberOfNames { get; init; }

        /// <summary>
        /// The address of the export address table, relative to the image base.
        /// </summary>
        public RVA<ForwardOrAddress[]> AddressOfFunctions { get; init; }

        /// <summary>
        /// The address of the export name pointer table, relative to the image base. The table size is given by the Number of Name Pointers field.
        /// </summary>
        public RVA<RVA<string>[]> AddressOfNames { get; init; }

        /// <summary>
        /// The address of the ordinal table, relative to the image base.
        /// </summary>
        public RVA<ushort[]> AddressOfNameOrdinals { get; init; }

        /// <summary>
        /// Gets the exports defined in this <see cref="ImageExportDirectory"/>.<para/>
        /// This field is not part of <see cref="IMAGE_EXPORT_DIRECTORY"/>, and merely aggregates the separate pieces of information
        /// contained in <see cref="AddressOfFunctions"/>, <see cref="AddressOfNames"/> and <see cref="AddressOfNameOrdinals"/>.<para/>
        /// If any of these three fields do not point to a valid RVA, this property will return an empty array.
        /// </summary>
        public Export[] Exports { get; }

        public RawOffset Offset { get; }

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

        internal ImageExportDirectory(IFileReader reader, PEFile peFile)
        {
            Offset = (RawOffset) reader.Position;

            reader.FillBuffer(StructSize);

            Characteristics = reader.ReadInt32();
            TimeDateStamp = reader.ReadUInt32();
            MajorVersion = reader.ReadUInt16();
            MinorVersion = reader.ReadUInt16();
            var name = (RVA) reader.ReadInt32();
            Base = reader.ReadInt32();
            NumberOfFunctions = reader.ReadInt32();
            NumberOfNames = reader.ReadInt32();
            var addressOfFunctions = (RVA) reader.ReadInt32();
            var addressOfNames = (RVA) reader.ReadInt32();
            var addressOfNameOrdinals = (RVA) reader.ReadInt32();

            //Read the RVAs

            //Stores the offset of whatever RVA we're currently parsing
            RawOffset offset;

            var exportTableStart = peFile.OptionalHeader.ExportTableDirectory.VirtualAddress;
            var exportTableEnd = exportTableStart + peFile.OptionalHeader.ExportTableDirectory.Size;

            #region Name

            if (peFile.TryGetOffset(name, out offset))
            {
                reader.Seek(offset);

                var str = reader.ReadAnsiNullTerminatedString();

                Name = new RVA<string>(name, offset, str);
            }
            else
            {
                //Bad
                Name = new RVA<string>(name);
            }

            #endregion
            #region AddressOfFunctions

            if (peFile.TryGetOffset(addressOfFunctions, out offset))
            {
                //We overwrite offset below
                var addressOfFunctionsOffset = offset;

                reader.Seek(offset);

                var functionAddresses = new RVA[NumberOfFunctions];

                for (var i = 0; i < NumberOfFunctions; i++)
                    functionAddresses[i] = (RVA) reader.ReadInt32();

                var addressAndForwarderNames = new ForwardOrAddress[NumberOfFunctions];

                for (var i = 0; i < NumberOfFunctions; i++)
                {
                    //If the actual address of the function is within the bounds of the export table (rather than a random place
                    //in the module) that means that a. addressOfFunction is pointing to a string and b. function actually comes
                    //from another module. Lookup _that_ external module.
                    //https://reverseengineering.stackexchange.com/questions/16023/exports-that-redirects-to-other-library

                    var functionAddress = functionAddresses[i];

                    if (functionAddress >= exportTableStart && functionAddress <= exportTableEnd)
                    {
                        //It's a name

                        if (peFile.TryGetOffset(functionAddress, out offset))
                        {
                            reader.Seek(offset);
                            var redirectName = reader.ReadAnsiNullTerminatedString();
                            addressAndForwarderNames[i] = new ForwardOrAddress(new RVA<string>(functionAddress, offset, redirectName));
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

                AddressOfFunctions = new RVA<ForwardOrAddress[]>(addressOfFunctions, addressOfFunctionsOffset, addressAndForwarderNames);
            }
            else
            {
                //Bad
                AddressOfFunctions = new RVA<ForwardOrAddress[]>(addressOfFunctions);
            }

            #endregion
            #region AddressOfNames

            if (peFile.TryGetOffset(addressOfNames, out offset))
            {
                //We overwrite offset below
                var addressOfNamesOffset = offset;

                reader.Seek(offset);

                var functionNameAddresses = new RVA[NumberOfNames];

                for (var i = 0; i < NumberOfNames; i++)
                    functionNameAddresses[i] = (RVA) reader.ReadInt32();

                var names = new RVA<string>[NumberOfNames];

                for (var i = 0; i < NumberOfNames; i++)
                {
                    var functionNameAddress = functionNameAddresses[i];

                    //Often the section.PointerToRawData will cause the functionNameAddress RVA to be correct
                    //in both loaded and unloaded images, but this is a gotcha: often, but not always!
                    if (peFile.TryGetOffset(functionNameAddress, out offset))
                    {
                        reader.Seek(offset);
                        var functionName = reader.ReadAnsiNullTerminatedString();
                        names[i] = new RVA<string>(functionNameAddress, offset, functionName);
                    }
                    else
                    {
                        //Bad
                        names[i] = new RVA<string>(functionNameAddress);
                    }
                }

                AddressOfNames = new RVA<RVA<string>[]>(addressOfNames, addressOfNamesOffset, names);
            }
            else
            {
                //Bad
                AddressOfNames = new RVA<RVA<string>[]>(addressOfNames);
            }

            #endregion
            #region AddressOfNameOrdinals

            if (peFile.TryGetOffset(addressOfNameOrdinals, out offset))
            {
                reader.Seek(offset);

                var functionOrdinals = new ushort[NumberOfNames];

                for (var i = 0; i < NumberOfNames; i++)
                    functionOrdinals[i] = reader.ReadUInt16();

                AddressOfNameOrdinals = new RVA<ushort[]>(addressOfNameOrdinals, offset, functionOrdinals);
            }
            else
            {
                //Bad
                AddressOfNameOrdinals = new RVA<ushort[]>(addressOfNameOrdinals);
            }

            #endregion

            if (AddressOfFunctions.IsValid && AddressOfNames.IsValid && AddressOfNameOrdinals.IsValid)
            {
                var exports = new Export[NumberOfFunctions];

                //Function ordinals are not guaranteed to be in order, so we need to store them in a map. While we're at it,
                //we may as well map each ordinal to the name of the function it corresponds to (rather than just mapping the ordinal
                //to the index at which it resides)
                var ordinalToNameAddressMap = new Dictionary<int, RVA<string>>();

                for (var i = 0; i < AddressOfNameOrdinals.Value.Length; i++)
                    ordinalToNameAddressMap.Add(AddressOfNameOrdinals.Value[i], AddressOfNames.Value[i]);

                for (var i = 0; i < AddressOfFunctions.Value.Length; i++)
                {
                    /* Exports can be exported by ordinal, or by both name and ordinal. The ordinal value of a function has no relation
                     * to its actual position within the ordinal list */

                    var functionAddressOrName = AddressOfFunctions.Value[i];
                    var ordinalPlusBase = i + Base;

                    string exportName = null;

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

                Exports = exports;
            }
            else
                Exports = Array.Empty<Export>();
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct(nameof(IMAGE_EXPORT_DIRECTORY), this, ViewKind.ImageExportDirectory);

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

        /// <summary>
        /// Represents either the address of a function, or the name of a function that this export forwards to.<para/>
        /// This structure models the fact that an export can point to one of two different types of data, and does not have a native equivalent.
        /// </summary>
        public readonly struct ForwardOrAddress
        {
            public RVA<string> ForwardName { get; }

            /// <summary>
            /// Gets the relative virtual address of the function that this forwarder points to.
            /// </summary>
            public RVA Address { get; }

            public bool IsForward { get; }

            public ForwardOrAddress(in RVA<string> name)
            {
                ForwardName = name;
                Address = (RVA) 0;
                IsForward = true;
            }

            public ForwardOrAddress(RVA address)
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

                    if (!string.IsNullOrEmpty(Name))
                        builder.Append($"[{Ordinal}] {Name}");
                    else
                        builder.Append($"[{Ordinal}]");

                    if (ForwardOrAddress.IsForward)
                        builder.Append(" -> ").Append(ForwardOrAddress);

                    return builder.ToString();
                }
            }

            public string? Name { get; }

            public int Index { get; }

            /// <summary>
            /// Gets the relative virtual address of the function that this export points to,
            /// or the module qualified name that this export forwards to.
            /// </summary>
            public ForwardOrAddress ForwardOrAddress { get; }

            public int Ordinal { get; }

            public Export(string? name, int index, ForwardOrAddress nameOrAddress, int ordinal)
            {
                Name = name;
                Index = index;
                ForwardOrAddress = nameOrAddress;
                Ordinal = ordinal;
            }

            public override string ToString()
            {
                if (!string.IsNullOrEmpty(Name))
                    return Name!;

                return Ordinal.ToString();
            }
        }
    }
}
