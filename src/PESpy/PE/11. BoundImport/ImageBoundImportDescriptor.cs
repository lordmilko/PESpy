using System.Diagnostics;
using PESpy.Native;
using PESpy.View;

namespace PESpy
{
    /// <summary>
    /// Represents the <see cref="IMAGE_BOUND_IMPORT_DESCRIPTOR"/> structure.
    /// </summary>
    public struct ImageBoundImportDescriptor : IValue, IViewable
    {
        public Timestamp TimeDateStamp => chunk.PeekUInt32(0);
        public ushort OffsetModuleName => chunk.PeekUInt16(4);
        public ushort NumberOfModuleForwarderRefs => chunk.PeekUInt16(6);

        private ImageBoundForwarderRef[]? refs;

        public ImageBoundForwarderRef[] Refs
        {
            get
            {
                if (refs == null)
                {
                    //IMAGE_BOUND_FORWARDER_REF looks exactly the same as IMAGE_BOUND_IMPORT_DESCRIPTOR.
                    //Each forwarder ref immediately follows the descriptor

                    var numRefs = NumberOfModuleForwarderRefs;

                    var results = new ImageBoundForwarderRef[numRefs];

                    for (var i = 0; i < numRefs; i++)
                        results[i] = new ImageBoundForwarderRef(chunk.Slice(8 + (i * ImageBoundForwarderRef.StructSize)));

                    refs = results;
                }

                return refs;
            }
        }

        /// <summary>
        /// Gets the name of the descriptor. This value is external to the normal <see cref="IMAGE_BOUND_IMPORT_DESCRIPTOR"/> type.
        /// </summary>
        private RVA<AnsiString> name;

        public RVA<AnsiString> Name
        {
            get
            {
                if (name.ListedOffset == 0)
                {
                    var peFile = chunk.PEFile();

                    var nameRVA = peFile.OptionalHeader.BoundImportTableDirectory.VirtualAddress + OffsetModuleName;

                    if (peFile.TryGetValueChunkFromPhysicalOffset(nameRVA, out var valueChunk))
                    {
                        var str = valueChunk.PeekAnsiNullTerminatedString(0);
                        name = new RVA<AnsiString>(OffsetModuleName, nameRVA, str);
                    }
                    else
                        name = new RVA<AnsiString>(nameRVA);
                }

                return name;
            }
        }

        public int Offset => chunk.AbsoluteOffset;

        //The number of refs is variable
        internal const int FixedStructSize =
            sizeof(int) + //TimeDateStamp
            sizeof(ushort) + //OffsetModuleName
            sizeof(ushort); //NumberOfModuleForwarderRefs

        internal int StructSize =>
            FixedStructSize +
            (NumberOfModuleForwarderRefs * ImageBoundForwarderRef.StructSize);

        private readonly MemoryChunk chunk;

        internal ImageBoundImportDescriptor(in MemoryChunk chunk)
        {
            this.chunk = chunk;
            name = default;
            refs = default;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            if (Name.IsValid && Name.ListedOffset != 0)
                writer.WriteGlobal(Name.ActualOffset, Name.Value, Name.Value.Length + 1, ViewKind.ImageBoundImportDescriptor_Name);

            writer.RelayGlobals(Refs);
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.IMAGE_BOUND_IMPORT_DESCRIPTOR, this, ViewKind.ImageBoundImportDescriptor, StructSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(TimeDateStamp), TimeDateStamp);
            s.WriteField(nameof(OffsetModuleName), OffsetModuleName);

            s.WriteField(nameof(NumberOfModuleForwarderRefs), NumberOfModuleForwarderRefs);

            s.WriteInline(Refs);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }

        public override string ToString()
        {
            return Name.ToString();
        }
    }
}
