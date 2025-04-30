using System;
using ClrDebug;
using PESpy.Native;
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

        /// <summary>
        /// Must be IMAGE_FILE_MACHINE_UNKNOWN
        /// </summary>
        public IMAGE_FILE_MACHINE Sig1 => (IMAGE_FILE_MACHINE) chunk.PeekUInt16(0);

        /// <summary>
        /// Must be 0xffff
        /// </summary>
        public short Sig2 => chunk.PeekInt16(2);

        /// <summary>
        /// >= 2 (implies the Flags field is present)
        /// </summary>
        public short Version => chunk.PeekInt16(4);

        /// <summary>
        /// Actual machine - IMAGE_FILE_MACHINE_xxx
        /// </summary>
        public IMAGE_FILE_MACHINE Machine => (IMAGE_FILE_MACHINE) chunk.PeekUInt16(6);

        public uint TimeDateStamp => chunk.PeekUInt32(8);

        /// <summary>
        /// <see cref="EXTENDED_COFF_OBJ_GUID"/> {D1BAA1C7-BAEE-4ba9-AF20-FAF66AA4DCB8}
        /// </summary>
        public Guid ClassID => chunk.PeekGuid(12);

        /// <summary>
        /// Size of data that follows the header
        /// </summary>
        public int SizeOfData => chunk.PeekInt32(28);

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

        void IViewable.WriteView(ViewWriter writer) => WriteView(writer);

        protected virtual void WriteView(ViewWriter writer)
        {
            var s = writer.CreateStruct(nameof(ANON_OBJECT_HEADER), this, ViewKind.AnonObjectHeader);

            WriteAnonObjectHeader(ref s);

            s.Dispose();
        }

        internal void WriteAnonObjectHeader(ref ViewWriter.StructWriter s)
        {
            s.WriteField(nameof(Sig1), Sig1, sizeof(short));
            s.WriteField(nameof(Sig2), Sig2);
            s.WriteField(nameof(Version), Version);
            s.WriteField(nameof(Machine), Machine, sizeof(short));
            s.WriteField(nameof(TimeDateStamp), TimeDateStamp);
            s.WriteField(nameof(ClassID), ClassID);
            s.WriteField(nameof(SizeOfData), SizeOfData);
        }
    }
}
