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

            private readonly PDBFileBuilder pdbFileBuilder;

            internal PDB(PDBFileBuilder pdbFileBuilder, Guid? guid)
            {
                this.pdbFileBuilder = pdbFileBuilder;

                //Creating this object constitutes a change, if it's then assigned to the PDBFileBuilder
                Changed = true;

                //Set defaults
                ImplementationVersion = PDBIMPV.PDBImpvVC70;
                Age = 1;
                Signature = (uint) DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                Guid = guid ?? Guid.NewGuid();

                StreamNameTable = new NMTNIBuilder();

                Features = new[] {PdbFeature.impvVC140};

                //This does update the ref, you don't need to store it in a variable
                pdbFileBuilder.StreamTable[SN.PDB].ByteCount = 0;
                pdbFileBuilder.StreamTable[SN.TPI].ByteCount = 0;
                pdbFileBuilder.StreamTable[SN.DBI].ByteCount = 0;
                pdbFileBuilder.StreamTable[SN.IPI].ByteCount = 0;
            }

            #region PDBStream70
            #region ImplementationVersion

            private PDBIMPV implementationVersion;

            /// <summary>
            /// Gets or sets <see cref="PDBStream.ImplementationVersion"/>.
            /// </summary>
            public PDBIMPV ImplementationVersion
            {
                get => implementationVersion;
                set => SetValue(ref implementationVersion, value, ref Changed);
            }

            #endregion
            #region Signature

            private uint signature;

            /// <summary>
            /// Gets or sets <see cref="PDBStream.Signature"/>
            /// </summary>
            public uint Signature
            {
                get => signature;
                set => SetValue(ref signature, value, ref Changed);
            }

            #endregion
            #region Age

            private int age;

            /// <summary>
            /// Gets or sets <see cref="PDBStream.Age"/>.
            /// </summary>
            public int Age
            {
                get => age;
                set => SetValue(ref age, value, ref Changed);
            }

            #endregion
            #region Guid

            private Guid guid;

            /// <summary>
            /// Gets or sets <see cref="PDBStream70.Guid"/>.
            /// </summary>
            public Guid Guid
            {
                get => guid;
                set => SetValue(ref guid, value, ref Changed);
            }

            #endregion
            #endregion
            #region NMTNIBuilder

            private NMTNIBuilder streamNameTable;

            public NMTNIBuilder StreamNameTable
            {
                get => streamNameTable;
                set => SetValue(ref streamNameTable, value, ref Changed);
            }

            #endregion
            #region Features

            private PdbFeature[]? features;

            public PdbFeature[]? Features
            {
                get => features;
                set => SetValue(ref features, value, ref Changed);
            }

            #endregion

            internal void Measure()
            {
                if (streamNameTable == null)
                    throw new NotImplementedException(); //nmtni cannot be null

                var size = PDBStream70.StructSize + streamNameTable.Measure();

                if (features != null)
                    size += features.Length * sizeof(int);

                //The PDB stream is saved by "replacing" it, which means that the previous stream gets deleted
                pdbFileBuilder.StreamTable.DeleteStream(SN.PDB);

                pdbFileBuilder.AllocPages(SN.PDB, size);

                //A PDB is saved by calling PDB1::Commit, which means in our MSF test we're going to be committing the PDB1 again
                //and thus changing its page. Therefore, the PDB always needs to be changed, so we can move it to a new page
                Changed = true;
            }

            internal void Serialize()
            {
                if (!Changed)
                    return;

                var chunk = pdbFileBuilder.SlicePaged(SN.PDB);

                _ = new PDBStream70(chunk)
                {
                    ImplementationVersion = ImplementationVersion,
                    Signature = Signature,
                    Age = Age,
                    Guid = guid
                };

                var offset = PDBStream70.StructSize;

                var nmtniSize = streamNameTable.Serialize(chunk.Slice(offset));
                offset += nmtniSize;

                if (features != null)
                    chunk.PokeSpan<PdbFeature>(offset, features.Length, features);

                Changed = false;
            }
        }
    }
}
