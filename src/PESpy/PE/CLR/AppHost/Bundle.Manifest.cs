using System;
using PESpy.View;

namespace PESpy
{
    public static partial class Bundle
    {
        public readonly struct Manifest : IValue, IViewable
        {
            public Bundle.HeaderFixed Header { get; }
            public BundleEncodedString BundleID { get; }
            public HeaderFixedV2? AdditionalContext { get; }

            public FileEntry[] Files { get; }

            public int Offset => chunk.AbsoluteOffset;

            public int StructSize => length;

            private readonly MemoryChunk chunk;
            private readonly int length;

            internal Manifest(in MemoryChunk chunk)
            {
                this.chunk = chunk;

                Header = new HeaderFixed(chunk);

                var read = HeaderFixed.StructSize;

                BundleID = new BundleEncodedString(chunk.Slice(read), out var bundleIdRead);
                read += bundleIdRead;

                if (Header.MajorVersion >= 2)
                {
                    AdditionalContext = new HeaderFixedV2(chunk.Slice(read));
                    read += HeaderFixedV2.StructSize;
                }
                else
                    AdditionalContext = default;

                //Immediately following the headers are file_entry_t records
                var files = new FileEntry[Header.NumEmbeddedFiles];

                var hasCompressedSize = Header.MajorVersion >= 6;

                for (var i = 0; i < files.Length; i++)
                {
                    files[i] = new FileEntry(chunk.Slice(read), hasCompressedSize, out var fileEntryRead);
                    read += fileEntryRead;
                }

                Files = files;
                this.length = read;
            }

            void IViewable.WriteGlobals(ViewWriter writer)
            {
                using var r = writer.CreateRegion(Header.Offset, "Bundle Manifest", ViewKind.BundleManifest);

                r.WriteValue(Header);
                r.WriteValue(BundleID);

                if (AdditionalContext != null)
                    r.WriteValue(AdditionalContext);

                r.WriteValues(Files);
            }

            IView? IViewable.WriteStruct(ViewWriter writer) => null;

            IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter) => throw new NotSupportedException();
        }
    }    
}
