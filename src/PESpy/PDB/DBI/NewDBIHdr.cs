using System;
using System.Diagnostics;
using ClrDebug;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    public class NewDBIHdr : IDBIHdr, IValue, IViewable
    {
        //In DBIHdr the first member is snGSSyms. I imagine that DBI will always
        //create a snGSSyms, which would mean that it would never be snNil. Therefore,
        //I think the expectation is that when verSignature is hdrSignature (-1) this means
        //we _must_ be NewDBIHdr
        public const int hdrSignature = -1;

        private const int verSignatureOffset = 0;
        private const int verHdrOffset = 4;
        private const int ageOffset = 8;
        private const int snGSSymsOffset = 12;
        private const int usVerAllOffset = 14;
        private const int snPSSymsOffset = 16;
        private const int usVerPdbDllBuildOffset = 18;
        private const int snSymRecsOffset = 20;
        private const int usVerPdbDllRBldOffset = 22;
        private const int cbGpModiOffset = 24;
        private const int cbSCOffset = 28;
        private const int cbSecMapOffset = 32;
        private const int cbFileInfoOffset = 36;
        private const int cbTSMapOffset = 40;
        private const int iMFCOffset = 44;
        private const int cbDbgHdrOffset = 48;
        private const int cbECInfoOffset = 52;
        private const int flagsOffset = 56;
        private const int wMachineOffset = 58;
        private const int rgulReservedOffset = 60;

        //verSignature. Value is always hdrSignature (-1)
        public int verSignature
        {
            get => chunk.PeekInt32(verSignatureOffset);
            set => chunk.PokeInt32(verSignatureOffset, value);
        }

        //verHdr
        public DBIImpv verHdr
        {
            get => (DBIImpv) chunk.PeekUInt32(verHdrOffset);
            set => chunk.PokeUInt32(verHdrOffset, (uint) value);
        }

        //age
        public int age
        {
            get => chunk.PeekInt32(ageOffset);
            set => chunk.PokeInt32(ageOffset, value);
        }

        //snGSSyms
        public SN snGSSyms
        {
            get => chunk.PeekUInt16(snGSSymsOffset);
            set => chunk.PokeUInt16(snGSSymsOffset, value);
        }

        //usVerAll
        public DbiHdrVersion usVerAll
        {
            get => chunk.PeekUInt16(usVerAllOffset);
            set => chunk.PokeUInt16(usVerAllOffset, value);
        }

        //snPSSyms
        public SN snPSSyms
        {
            get => chunk.PeekUInt16(snPSSymsOffset);
            set => chunk.PokeUInt16(snPSSymsOffset, value);
        }

        /// <summary>
        /// build version of the pdb dll that built this pdb last.
        /// </summary>
        public ushort usVerPdbDllBuild
        {
            get => chunk.PeekUInt16(usVerPdbDllBuildOffset);
            set => chunk.PokeUInt16(usVerPdbDllBuildOffset, value);
        }

        public SN snSymRecs
        {
            get => chunk.PeekUInt16(snSymRecsOffset);
            set => chunk.PokeUInt16(snSymRecsOffset, value);
        }

        /// <summary>
        /// rbld version of the pdb dll that built this pdb last.
        /// </summary>
        public ushort usVerPdbDllRBld
        {
            get => chunk.PeekUInt16(usVerPdbDllRBldOffset);
            set => chunk.PokeUInt16(usVerPdbDllRBldOffset, value);
        }

        /// <summary>
        /// size of rgmodi substream
        /// </summary>
        public int cbGpModi
        {
            get => chunk.PeekInt32(cbGpModiOffset);
            set => chunk.PokeInt32(cbGpModiOffset, value);
        }

        /// <summary>
        /// size of Section Contribution substream
        /// </summary>
        public int cbSC
        {
            get => chunk.PeekInt32(cbSCOffset);
            set => chunk.PokeInt32(cbSCOffset, value);
        }

        public int cbSecMap
        {
            get => chunk.PeekInt32(cbSecMapOffset);
            set => chunk.PokeInt32(cbSecMapOffset, value);
        }

        public int cbFileInfo
        {
            get => chunk.PeekInt32(cbFileInfoOffset);
            set => chunk.PokeInt32(cbFileInfoOffset, value);
        }

        /// <summary>
        /// size of the Type Server Map substream
        /// </summary>
        public int cbTSMap
        {
            get => chunk.PeekInt32(cbTSMapOffset);
            set => chunk.PokeInt32(cbTSMapOffset, value);
        }

        /// <summary>
        /// index of MFC type server
        /// </summary>
        public int iMFC
        {
            get => chunk.PeekInt32(iMFCOffset);
            set => chunk.PokeInt32(iMFCOffset, value);
        }

        /// <summary>
        /// size of optional DbgHdr info appended to the end of the stream
        /// </summary>
        public int cbDbgHdr
        {
            get => chunk.PeekInt32(cbDbgHdrOffset);
            set => chunk.PokeInt32(cbDbgHdrOffset, value);
        }

        /// <summary>
        /// number of bytes in EC substream, or 0 if EC no EC enabled Mods
        /// </summary>
        public int cbECInfo
        {
            get => chunk.PeekInt32(cbECInfoOffset);
            set => chunk.PokeInt32(cbECInfoOffset, value);
        }

        public DbiHdrFlags flags
        {
            get => chunk.PeekUInt16(flagsOffset);
            set => chunk.PokeUInt16(flagsOffset, value);
        }

        /// <summary>
        /// machine type
        /// </summary>
        public IMAGE_FILE_MACHINE wMachine
        {
            get => (IMAGE_FILE_MACHINE) chunk.PeekUInt16(wMachineOffset);
            set => chunk.PokeUInt16(wMachineOffset, (ushort) value);
        }

        /// <summary>
        /// pad out to 64 bytes for future growth.
        /// </summary>
        public int rgulReserved
        {
            get => chunk.PeekInt32(rgulReservedOffset);
            set => chunk.PokeInt32(rgulReservedOffset, value);
        }

        int IDBIHdr.StructSize => StructSize;

        public long Offset => chunk.AbsoluteOffset;

        public const int StructSize =
            sizeof(int) + //verSignature
            sizeof(int) + //verHdr
            sizeof(int) + //age
            sizeof(ushort) + //snGSSyms
            sizeof(ushort) + //usVerAll
            sizeof(ushort) + //snPSSyms
            sizeof(ushort) + //usVerPdbDllBuild
            sizeof(ushort) + //snSymRecs
            sizeof(ushort) + //usVerPdbDllRBld
            sizeof(int) + //cbGpModi
            sizeof(int) + //cbSC
            sizeof(int) + //cbSecMap
            sizeof(int) + //cbFileInfo
            sizeof(int) + //cbTSMap
            sizeof(int) + //iMFC
            sizeof(int) + //cbDbgHdr
            sizeof(int) + //cbECInfo
            sizeof(ushort) + //flags
            sizeof(ushort) + //wMachine
            sizeof(int); //rgulReserved

        private readonly MemoryChunk chunk;

        internal NewDBIHdr(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.NewDbiHdr, StructSize);

        int IViewable.NumChildren() => 25;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(verSignature), verSignatureOffset, verSignature);
                    break;

                case 1:
                    structWriter.WriteField(nameof(verHdr), verHdrOffset, verHdr, sizeof(int));
                    break;

                case 2:
                    structWriter.WriteField(nameof(age), ageOffset, age);
                    break;

                case 3:
                    structWriter.WriteField(nameof(snGSSyms), snGSSymsOffset, snGSSyms);
                    break;

                #region BitField

                case 4:
                    if (usVerAll.vernew.fNewVerFmt)
                        structWriter.WriteBitField("usVerPdbDllMin", usVerAllOffset, usVerAll.vernew.usVerPdbDllMin, sizeof(ushort), 8);
                    else
                    {
                        //Assume it's just old and not really screwed up
                        structWriter.WriteBitField("usVerPdbDllRbld", usVerAllOffset, usVerAll.verold.usVerPdbDllRBld, sizeof(ushort), 4);
                    }
                    break;

                case 5:
                    if (usVerAll.vernew.fNewVerFmt)
                        structWriter.WriteBitField("usVerPdbDllMaj", usVerAllOffset, usVerAll.vernew.usVerPdbDllMaj, sizeof(ushort), 7);
                    else
                    {
                        //Assume it's just old and not really screwed up
                        structWriter.WriteBitField("usVerPdbDllMin", usVerAllOffset, usVerAll.verold.usVerPdbDllMin, sizeof(ushort), 7);
                    }
                    break;

                case 6:
                    if (usVerAll.vernew.fNewVerFmt)
                        structWriter.WriteBitField("fNewVerFmt", usVerAllOffset, usVerAll.vernew.fNewVerFmt, sizeof(ushort), 1);
                    else
                    {
                        //Assume it's just old and not really screwed up
                        structWriter.WriteBitField("usVerPdbDllMaj", usVerAllOffset, usVerAll.verold.usVerPdbDllMaj, sizeof(ushort), 5);
                    }
                    break;

                #endregion

                case 7:
                    structWriter.WriteField(nameof(snPSSyms), snPSSymsOffset, snPSSyms);
                    break;

                case 8:
                    structWriter.WriteField(nameof(usVerPdbDllBuild), usVerPdbDllBuildOffset, usVerPdbDllBuild);
                    break;

                case 9:
                    structWriter.WriteField(nameof(snSymRecs), snSymRecsOffset, snSymRecs);
                    break;

                case 10:
                    structWriter.WriteField(nameof(usVerPdbDllRBld), usVerPdbDllRBldOffset, usVerPdbDllRBld);
                    break;

                case 11:
                    structWriter.WriteField(nameof(cbGpModi), cbGpModiOffset, cbGpModi);
                    break;

                case 12:
                    structWriter.WriteField(nameof(cbSC), cbSCOffset, cbSC);
                    break;

                case 13:
                    structWriter.WriteField(nameof(cbSecMap), cbSecMapOffset, cbSecMap);
                    break;

                case 14:
                    structWriter.WriteField(nameof(cbFileInfo), cbFileInfoOffset, cbFileInfo);
                    break;

                case 15:
                    structWriter.WriteField(nameof(cbTSMap), cbTSMapOffset, cbTSMap);
                    break;

                case 16:
                    structWriter.WriteField(nameof(iMFC), iMFCOffset, iMFC);
                    break;

                case 17:
                    structWriter.WriteField(nameof(cbDbgHdr), cbDbgHdrOffset, cbDbgHdr);
                    break;

                case 18:
                    structWriter.WriteField(nameof(cbECInfo), cbECInfoOffset, cbECInfo);
                    break;

                #region BitField

                case 19:
                    structWriter.WriteBitField("fIncLink", flagsOffset, flags.fIncLink, sizeof(ushort), 1);
                    break;

                case 20:
                    structWriter.WriteBitField("fStripped", flagsOffset, flags.fStripped, sizeof(ushort), 1);
                    break;

                case 21:
                    structWriter.WriteBitField("fCTypes", flagsOffset, flags.fCTypes, sizeof(ushort), 1);
                    break;

                case 22:
                    structWriter.WriteBitField("unused", flagsOffset, flags.unused, sizeof(ushort), 13);
                    break;

                #endregion

                case 23:
                    structWriter.WriteField(nameof(wMachine), wMachineOffset, wMachine, sizeof(short));
                    break;

                case 24:
                    structWriter.WriteField(nameof(rgulReserved), rgulReservedOffset, rgulReserved);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
