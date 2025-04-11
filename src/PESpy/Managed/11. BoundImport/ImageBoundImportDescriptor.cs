using System.Collections.Generic;
using PESpy.Native;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
using RVA = System.Int32;
#endif

namespace PESpy
{
    /// <summary>
    /// Represents the <see cref="IMAGE_BOUND_IMPORT_DESCRIPTOR"/> structure.
    /// </summary>
    public readonly struct ImageBoundImportDescriptor : IValue, IViewable
    {
        public uint TimeDateStamp { get; init; }
        public ushort OffsetModuleName { get; init; }
        public ushort NumberOfModuleForwarderRefs { get; init; }

        public ImageBoundForwarderRef[] Refs { get; init; }

        /// <summary>
        /// Gets the name of the descriptor. This value is external to the normal <see cref="IMAGE_BOUND_IMPORT_DESCRIPTOR"/> type.
        /// </summary>
        public RVA<string> Name { get; }

        public RawOffset Offset { get; }

        //The number of refs is variable
        internal const int FixedStructSize =
            sizeof(int) + //TimeDateStamp
            sizeof(ushort) + //OffsetModuleName
            sizeof(ushort); //NumberOfModuleForwarderRefs

        internal ImageBoundImportDescriptor(IFileReader reader, PEFile peFile)
        {
            Offset = (RawOffset) reader.Position;

            reader.FillBuffer(FixedStructSize);

            TimeDateStamp = reader.ReadUInt32();
            OffsetModuleName = reader.ReadUInt16();
            NumberOfModuleForwarderRefs = reader.ReadUInt16();

            var refs = new List<ImageBoundForwarderRef>();

            var pos = reader.Position;

            //IMAGE_BOUND_FORWARDER_REF looks exactly the same as IMAGE_BOUND_IMPORT_DESCRIPTOR.
            //Each forwarder ref immediately follows the descriptor

            for (var i = 0; i < NumberOfModuleForwarderRefs; i++)
            {
                if (i > 0)
                    reader.Seek(pos);

                refs.Add(new ImageBoundForwarderRef(reader, peFile));

                pos += ImageBoundForwarderRef.StructSize;
            }

            var nameRVA = (RawOffset) peFile.OptionalHeader.BoundImportTableDirectory.VirtualAddress + OffsetModuleName;

            reader.Seek(nameRVA);
            var str = reader.ReadAnsiNullTerminatedString();
            Name = new RVA<string>((RVA) OffsetModuleName, nameRVA, str);

            Refs = refs.ToArray();
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct(nameof(IMAGE_BOUND_IMPORT_DESCRIPTOR), this, ViewKind.ImageBoundImportDescriptor);

            s.WriteField(nameof(TimeDateStamp), TimeDateStamp);

            s.WriteField(nameof(OffsetModuleName), OffsetModuleName);

            if (Name.IsValid && Name.ListedOffset != 0)
                writer.WriteGlobal(Name.ActualOffset, Name.Value, Name.Value.Length + 1, ViewKind.String);

            s.WriteField(nameof(NumberOfModuleForwarderRefs), NumberOfModuleForwarderRefs);
            
            s.WriteInline(Refs);
        }

        public override string ToString()
        {
            return Name.ToString();
        }
    }
}
