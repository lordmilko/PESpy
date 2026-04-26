using System;
using System.Diagnostics;
using ClrDebug;
using PESpy.View;

namespace PESpy
{
    public class AnonObjectHeader : IValue, IViewable
    {
        //Name comes from microsoft-pdb
        /* Microsoft-pdb makes reference to a GUID "EXTENDED_COFF_OBJ_GUID" but does not
         * provide a definition. cvdump.exe is provided however, and I checked in a deubgger:
         * EXTENDED_COFF_OBJ_GUID is indeed {D1BAA1C7-BAEE-4ba9-AF20-FAF66AA4DCB8} which is
         * referenced on ANON_OBJECT_HEADER_BIGOBJ.ClassID in winnt.h.
         *
         * Note that obj files compiled with LTCG (/GL) seem to have a different ClassID
         * have a ClassID {0CB3FE38-D9A5-4DAB-AC9B-D6B6222653C2} */
        internal static readonly Guid EXTENDED_COFF_OBJ_GUID = new Guid("D1BAA1C7-BAEE-4ba9-AF20-FAF66AA4DCB8"); //Used in c2!CoffTerm (called by coff_end)

        private const int Sig1Offset = 0;
        private const int Sig2Offset = 2;
        private const int VersionOffset = 4;
        private const int MachineOffset = 6;
        private const int TimeDateStampOffset = 8;
        private const int ClassIDOffset = 12;
        private const int SizeOfDataOffset = 28;

        /// <summary>
        /// Must be IMAGE_FILE_MACHINE_UNKNOWN
        /// </summary>
        public IMAGE_FILE_MACHINE Sig1 => (IMAGE_FILE_MACHINE) chunk.PeekUInt16(Sig1Offset);

        /// <summary>
        /// Must be 0xffff
        /// </summary>
        public short Sig2 => chunk.PeekInt16(Sig2Offset);

        /// <summary>
        /// >= 2 (implies the Flags field is present)
        /// </summary>
        public short Version => chunk.PeekInt16(VersionOffset);

        /// <summary>
        /// Actual machine - IMAGE_FILE_MACHINE_xxx
        /// </summary>
        public IMAGE_FILE_MACHINE Machine => (IMAGE_FILE_MACHINE) chunk.PeekUInt16(MachineOffset);

        public Timestamp TimeDateStamp => chunk.PeekUInt32(TimeDateStampOffset);

        /// <summary>
        /// <see cref="EXTENDED_COFF_OBJ_GUID"/> {D1BAA1C7-BAEE-4ba9-AF20-FAF66AA4DCB8}
        /// </summary>
        public Guid ClassID => chunk.PeekGuid(ClassIDOffset);

        /// <summary>
        /// Size of data that follows the header
        /// </summary>
        public int SizeOfData => chunk.PeekInt32(SizeOfDataOffset);

        public int Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(short) + //Sig1
            sizeof(short) + //Sig2
            sizeof(short) + //Version
            sizeof(short) + //Machine
            sizeof(int) + //TimeDateStamp
            16 + //ClassID
            sizeof(int); //SizeOfData

        internal readonly MemoryChunk chunk;

        internal AnonObjectHeader(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) => WriteStruct(writer);

        int IViewable.NumChildren() => NumChildren;

        void IViewable.WriteChild(int index, ref StructWriter structWriter) => WriteChild(index, ref structWriter);

        protected virtual IView? WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.AnonObjectHeader, StructSize);

        protected virtual int NumChildren => 7;

        protected virtual void WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(Sig1), Sig1Offset, Sig1, sizeof(short));
                    break;

                case 1:
                    structWriter.WriteField(nameof(Sig2), Sig2Offset, Sig2);
                    break;

                case 2:
                    structWriter.WriteField(nameof(Version), VersionOffset, Version);
                    break;

                case 3:
                    structWriter.WriteField(nameof(Machine), MachineOffset, Machine, sizeof(short));
                    break;

                case 4:
                    structWriter.WriteField(nameof(TimeDateStamp), TimeDateStampOffset, TimeDateStamp);
                    break;

                case 5:
                    structWriter.WriteField(nameof(ClassID), ClassIDOffset, ClassID);
                    break;

                case 6:
                    structWriter.WriteField(nameof(SizeOfData), SizeOfDataOffset, SizeOfData);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
