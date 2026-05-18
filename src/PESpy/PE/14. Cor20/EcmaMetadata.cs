using System;
using System.Diagnostics;
using PESpy.Ecma335;
using PESpy.View;
using static PESpy.StorageStream;

namespace PESpy
{
    /// <summary>
    /// Encapsulates the data structures used to define ECMA-335 Metadata<para/>
    /// This wrapper type does not have a well-known native struct declaration<para/>
    /// EMCA-335 II.24.2
    /// </summary>
    [Source(SourceKind.Synthetic)]
    public class EcmaMetadata : IValue, IViewable
    {
        public StorageSignature Signature { get; }

        public StorageHeader Header { get; }

        #region Streams
        #region #~

        private ModelHeap? modelHeap;

        /// <summary>
        /// Provides access to the compressed or uncompressed model heap pointed to by the <see cref="StorageHeader"/> -> <see cref="StorageStream"/> whose name is either "#~" or "#-"
        /// </summary>
        public ModelHeap? ModelHeap
        {
            get
            {
                EnsureHeaps();
                return modelHeap;
            }
        }

        #endregion
        #region #Strings

        private StringHeap? stringHeap;

        /// <summary>
        /// Provides access to the Strings heap pointed to by the <see cref="StorageHeader"/> -> <see cref="StorageStream"/> whose name is "#Strings"
        /// </summary>
        public StringHeap? StringHeap
        {
            get
            {
                EnsureHeaps();
                return stringHeap;
            }
        }

        #endregion
        #region US

        private UserStringHeap? userStringHeap;

        /// <summary>
        /// Provides access to the User String heap pointed to by the <see cref="StorageHeader"/> -> <see cref="StorageStream"/> whose name is "#US"
        /// </summary>
        public UserStringHeap? UserStringHeap
        {
            get
            {
                EnsureHeaps();
                return userStringHeap;
            }
        }

        #endregion
        #region #Blob

        private BlobHeap? blobHeap;

        /// <summary>
        /// Provides access to the Blob heap pointed to by the <see cref="StorageHeader"/> -> <see cref="StorageStream"/> whose name is "#Blob"
        /// </summary>
        public BlobHeap? BlobHeap
        {
            get
            {
                EnsureHeaps();
                return blobHeap;
            }
        }

        #endregion
        #region #GUID

        private GuidHeap? guidHeap;

        /// <summary>
        /// Provides access to the Blob heap pointed to by the <see cref="StorageHeader"/> -> <see cref="StorageStream"/> whose name is "#GUID"
        /// </summary>
        public GuidHeap? GuidHeap
        {
            get
            {
                EnsureHeaps();
                return guidHeap;
            }
        }

        #endregion
        #region #Pdb

        private PdbHeap? pdbHeap;

        /// <summary>
        /// Provides access to the PDB heap pointed to by the <see cref="StorageHeader"/> -> <see cref="StorageStream"/> whose name is "#Pdb"<para/>
        /// This stream should only be present in Portable PDBs
        /// </summary>
        public PdbHeap? PdbHeap
        {
            get
            {
                EnsureHeaps();
                return pdbHeap;
            }
        }

        internal PdbHeap? GetPdbHeap()
        {
            //ModelHeap will want to ask for this, but that's a catch-22 because we may already be in the middle of ensuring heaps,
            //which will cause an infinite recursion and a stack overflow. As such, we need to manually search for the header

            //If we're not a PortablePDB, don't waste time searching for it
            if (chunk.File().Kind != FileKind.PortablePDB)
                return null;

            if (initialized)
                return pdbHeap;

            var streamHeaders = Header.StreamHeaders;

            for (var i = 0; i < streamHeaders.Length; i++)
            {
                ref var streamHeader = ref streamHeaders[i];

                if (streamHeader.Name == PdbStream)
                {
                    pdbHeap = (PdbHeap?) streamHeader.Data;
                    break;
                }
            }

            return pdbHeap;
        }

        #endregion
        #endregion

        public long Offset => chunk.AbsoluteOffset;
        private bool initialized;

        private readonly MemoryChunk chunk;

        internal EcmaMetadata(in MemoryChunk chunk)
        {
            this.chunk = chunk;

            Signature = new StorageSignature(chunk);
            Header = new StorageHeader(chunk.Slice((StorageSignature.FixedStructSize + Signature.VersionStringLength + 3) & ~3), (int) Offset, this); //Align to next 4 byte boundary
        }

        private void EnsureHeaps()
        {
            if (initialized)
                return;

            var streamHeaders = Header.StreamHeaders;

            for (var i = 0; i < streamHeaders.Length; i++)
            {
                ref var streamHeader = ref streamHeaders[i];

                switch (streamHeader.Name)
                {
                    case CompressedModelStream:
                    case EnCModelStream:
                        Debug.Assert(modelHeap == null); //I don't think you can have both at once? But I haven't tested with EnC
                        modelHeap = (ModelHeap?) streamHeader.Data;
                        break;

                    case StringPoolStream:
                        stringHeap = (StringHeap?) streamHeader.Data;
                        break;

                    case USBlobPoolStream:
                        userStringHeap = (UserStringHeap?) streamHeader.Data;
                        break;

                    case BlobPoolStream:
                        blobHeap = (BlobHeap?) streamHeader.Data;
                        break;

                    case GuidPoolStream:
                        guidHeap = (GuidHeap?) streamHeader.Data;
                        break;

                    case PdbStream:
                        pdbHeap = (PdbHeap?) streamHeader.Data;
                        break;
                }
            }

            initialized = true;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //A region will be created around the IMAGE_COR20_HEADER.Metadata ImageDataDirectory during merging

            writer.WriteGlobal(Signature);
            writer.WriteGlobal(Header);
        }

        IView? IViewable.WriteStruct(ViewWriter writer) => null;

        int IViewable.NumChildren() => throw new NotSupportedException();

        void IViewable.WriteChild(int index, ref StructWriter structWriter) => throw new NotSupportedException();
    }
}
