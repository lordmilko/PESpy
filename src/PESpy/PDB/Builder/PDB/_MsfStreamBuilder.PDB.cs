using System;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    public partial class MsfStreamBuilder
    {
        internal static void SetValue<T>(ref T field, T value, ref bool changed)
        {
            if (Equals(field, value))
                return;

            field = value;
            changed = true;
        }

        public class PDB
        {
            internal bool Changed;

            private readonly PDBFileBuilder _pdbFileBuilder;

            internal PDB(PDBFileBuilder pdbFileBuilder, Guid? guid)
            {
                _pdbFileBuilder = pdbFileBuilder;

                //Creating this object constitutes a change, if it's then assigned to the PDBFileBuilder
                Changed = true;

                //Set defaults
                _implementationVersion = PDBIMPV.PDBImpvVC70;
                _age = 1;
                _signature = (uint) DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                _guid = guid ?? Guid.NewGuid();

                _streamNameTable = new NMTNIBuilder();

                _features = new[] {PdbFeature.impvVC140};

#if PDB1_COMPATIBILITY
                //This does update the ref, you don't need to store it in a variable
                pdbFileBuilder.StreamTable[SN.PDB].ByteCount = 0;
                pdbFileBuilder.StreamTable[SN.TPI].ByteCount = 0;
                pdbFileBuilder.StreamTable[SN.DBI].ByteCount = 0;
                pdbFileBuilder.StreamTable[SN.IPI].ByteCount = 0;
#endif
            }

            internal PDB(MsfStream.PDB pdb, PDBFileBuilder pdbFileBuilder)
            {
                _pdbFileBuilder = pdbFileBuilder;

                Changed = false;

                var pdbHeader = (PDBStream70) pdb.PDBHeader;

                _implementationVersion = pdbHeader.ImplementationVersion;
                _age = pdbHeader.Age;
                _signature = pdbHeader.Signature;
                _guid = pdbHeader.Guid;

                _streamNameTable = new NMTNIBuilder(pdb.StreamNameTable);

                _features = pdb.Features.ToArray();
            }

            #region PDBStream70
            #region ImplementationVersion

            private PDBIMPV _implementationVersion;

            /// <summary>
            /// Gets or sets <see cref="PDBStream.ImplementationVersion"/>.
            /// </summary>
            public PDBIMPV ImplementationVersion
            {
                get => _implementationVersion;
                set => SetValue(ref _implementationVersion, value, ref Changed);
            }

            #endregion
            #region Signature

            private uint _signature;

            /// <summary>
            /// Gets or sets <see cref="PDBStream.Signature"/>
            /// </summary>
            public uint Signature
            {
                get => _signature;
                set => SetValue(ref _signature, value, ref Changed);
            }

            #endregion
            #region Age

            private int _age;

            /// <summary>
            /// Gets or sets <see cref="PDBStream.Age"/>.
            /// </summary>
            public int Age
            {
                get => _age;
                set => SetValue(ref _age, value, ref Changed);
            }

            #endregion
            #region Guid

            private Guid _guid;

            /// <summary>
            /// Gets or sets <see cref="PDBStream70.Guid"/>.
            /// </summary>
            public Guid Guid
            {
                get => _guid;
                set => SetValue(ref _guid, value, ref Changed);
            }

            #endregion
            #endregion
            #region NMTNIBuilder

            private NMTNIBuilder _streamNameTable;

            public NMTNIBuilder StreamNameTable
            {
                get => _streamNameTable;
                set => SetValue(ref _streamNameTable, value, ref Changed);
            }

            #endregion
            #region Features

            private PdbFeature[]? _features;

            public PdbFeature[]? Features
            {
                get => _features;
                set => SetValue(ref _features, value, ref Changed);
            }

            #endregion

            internal void Measure()
            {
                if (_streamNameTable == null)
                    throw new NotImplementedException(); //nmtni cannot be null

                var size = PDBStream70.StructSize + _streamNameTable.Measure();

                if (_features != null)
                    size += _features.Length * sizeof(int);

                //The PDB stream is saved by "replacing" it, which means that the previous stream gets deleted
                _pdbFileBuilder.StreamTable.DeleteStream(SN.PDB);

                _pdbFileBuilder.AllocPages(SN.PDB, size);

                //A PDB is saved by calling PDB1::Commit, which means in our MSF test we're going to be committing the PDB1 again
                //and thus changing its page. Therefore, the PDB always needs to be changed, so we can move it to a new page
                Changed = true;
            }

            internal void Serialize()
            {
                if (!Changed)
                    return;

                var chunk = _pdbFileBuilder.SlicePaged(SN.PDB);

                _ = new PDBStream70(chunk)
                {
                    ImplementationVersion = ImplementationVersion,
                    Signature = Signature,
                    Age = Age,
                    Guid = _guid
                };

                var offset = PDBStream70.StructSize;

                var nmtniSize = _streamNameTable.Serialize(chunk.Slice(offset));
                offset += nmtniSize;

                if (_features != null)
                    chunk.PokeSpan<PdbFeature>(offset, _features.Length, _features);

                Changed = false;
            }
        }
    }
}
