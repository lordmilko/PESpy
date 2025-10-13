using System;
using System.IO;
using ClrDebug;
using PESpy.Native;
using PESpy.NE;
using PESpy.OMF;
using PESpy.PDB;

//Having out IFile? is confusing from an API standpoint because the caller has to keep doing file! whenever they use it when we returned true.
//Attributes to say we have a value when we return true haven't worked for me in the past
#nullable disable

namespace PESpy
{
    public static class Detector
    {
        public static IFile OpenFile(string path)
        {
            if (!TryOpenFile(path, out var file))
                throw new InvalidOperationException($"Failed to detect the type of file '{file}'");

            return file;
        }

        public static unsafe bool TryOpenFile(string path, out IFile file)
        {
            file = default;

            if (!File.Exists(path))
                return false;

            using var fs = File.OpenRead(path);

            var length = fs.Length;

            if (length < 2) //All signatures require at least 2 bytes
                return false;

            var mmf = new MemoryMappedFileHolder(fs);

            try
            {
                if (TryDetectFile(mmf, length, out var kind, out var subKind))
                {
                    switch (kind)
                    {
                        case FileKind.PE:
                            file = new PEFile(fs.Name, mmf);
                            return true;

                        case FileKind.NE:
                            file = new NEFile(fs.Name, mmf);
                            return true;

                        case FileKind.LE:
                            file = new LEFile(fs.Name, mmf);
                            return true;

                        case FileKind.DOS:
                            file = new DOSFile(fs.Name, mmf);
                            return true;

                        case FileKind.DBG:
                            file = new DBGFile(fs.Name, mmf);
                            return true;

                        case FileKind.LIB:
                            file = new LIBFile(fs.Name, mmf);
                            return true;

                        case FileKind.PDB:
                            switch ((PDBFileKind) subKind)
                            {
                                case PDBFileKind.V1:
                                    file = new PDB1File(fs.Name, mmf);
                                    return true;

                                case PDBFileKind.V2:
                                    file = new PDB2File(fs.Name, mmf);
                                    return true;

                                case PDBFileKind.V7:
                                    file = new PDB7File(fs.Name, mmf);
                                    return true;

                                default:
                                    throw new NotImplementedException($"Don't know how to handle a PDB of sub-type '{(PDBFileKind) subKind}'");
                            }

                        case FileKind.PortablePDB:
                            file = new PortablePDBFile(fs.Name, mmf);
                            return true;

                        case FileKind.OBJ:
                            file = new OBJFile(fs.Name, mmf);
                            return true;

                        case FileKind.OMF:
                            file = new OMFFile(fs.Name, mmf);
                            return true;

                        case FileKind.OMFLIB:
                            file = new OMFLIBFile(fs.Name, mmf);
                            return true;
                    }
                }

                file = default;
                return false;
            }
            finally
            {
                //If we got a file, ownership of the MMF transfers to the file
                if (file == null)
                    mmf.Dispose();
            }
        }

        public static unsafe bool TryDetectFile(
            string path,
            out FileKind fileKind,
            out int fileSubKind)
        {
            fileKind = default;
            fileSubKind = default;

            if (!File.Exists(path))
                return false;

            using var fs = File.OpenRead(path);

            var length = fs.Length;

            if (length < 2) //All signatures require at least 2 bytes
                return false;

            var mmf = new MemoryMappedFileHolder(fs);

            try
            {
                return TryDetectFile(mmf, length, out fileKind, out fileSubKind);
            }
            finally
            {
                //If we got a file, ownership of the MMF transfers to the file
                mmf.Dispose();
            }
        }

        private static unsafe bool TryDetectFile(
            MemoryMappedFileHolder mmf,
            long length,
            out FileKind fileKind,
            out int fileSubKind)
        {
            fileKind = default;
            fileSubKind = default;

            var twoLetterSignature = *(ushort*) mmf.Address;

            if (length >= ImageDosHeader.StructSize && twoLetterSignature == ImageDosHeader.IMAGE_DOS_SIGNATURE) //MZ
            {
                //COFF, PE or NE

                var fileAddressOfNewExeHeader = *(int*) (mmf.Address + 60);

                if (fileAddressOfNewExeHeader >= length)
                {
                    //e_lfanew is garbage, indicating this is probably a DOS file
                    fileKind = FileKind.DOS;
                    return true;
                }

                var sig = *(uint*) (mmf.Address + fileAddressOfNewExeHeader);

                if (sig == ImageNtHeaders.IMAGE_NT_SIGNATURE)
                {
                    fileKind = FileKind.PE;
                    return true;
                }

                sig &= 0xFFFF; //LE/NE header is 2 bytes not 4. First two bytes will be junk due to little endian read

                switch (sig)
                {
                    case ImageOS2Header.IMAGE_OS2_SIGNATURE:
                        fileKind = FileKind.NE;
                        return true;

                    case ImageVXDHeader.IMAGE_VXD_SIGNATURE:
                        fileKind = FileKind.LE;
                        return true;
                }

                fileKind = FileKind.DOS;
                return true;
            }
            else if (length >= ImageSeparateDebugHeader.StructSize && twoLetterSignature == ImageSeparateDebugHeader.IMAGE_SEPARATE_DEBUG_SIGNATURE) //DI
            {
                //A file that simply starts with "DI" is insufficient grounds for saying something is a *.dbg file. Sanity check the IMAGE_FILE_MACHINE and
                //number of sections

                if (IsValidMachine((IMAGE_FILE_MACHINE) (*(ushort*) (mmf.Address + 4))))
                {
                    var numberOfSections = *(int*) (mmf.Address + 24);
                    var minNumBytes = ImageSeparateDebugHeader.StructSize + numberOfSections * ImageSectionHeader.StructSize;

                    if (minNumBytes < length)
                    {
                        fileKind = FileKind.DBG;
                        return true;
                    }
                }
            }
            else if (length >= 4 && *(uint*) mmf.Address == StorageSignature.STORAGE_MAGIC_SIG)
            {
                fileKind = FileKind.PortablePDB;
                return true;
            }

            if (length >= IMAGE_ARCHIVE_MEMBER_HEADER.IMAGE_ARCHIVE_START_SIZE) //!<arch>\n
            {
                var libSig = new FixedAnsiString(mmf.Address, IMAGE_ARCHIVE_MEMBER_HEADER.IMAGE_ARCHIVE_START_SIZE);

                if (libSig == IMAGE_ARCHIVE_MEMBER_HEADER.IMAGE_ARCHIVE_START)
                {
                    fileKind = FileKind.LIB;
                    return true;
                }
            }

            if (length >= BigMsfHdr.BigHdrMagic.Length) //Microsoft C/C++ MSF 7.00\r\n\u001aDS\0\0\0
            {
                var bigHdr = new FixedAnsiString(mmf.Address, BigMsfHdr.BigHdrMagic.Length);

                if (bigHdr == BigMsfHdr.BigHdrMagic)
                {
                    fileKind = FileKind.PDB;
                    fileSubKind = (int) PDBFileKind.V7;
                    return true;
                }

                if (length >= MsfHdr.HdrMagic.Length) //V1 and V2 are both 44 bytes
                {
                    var hdr = new FixedAnsiString(mmf.Address, MsfHdr.HdrMagic.Length);

                    if (hdr == MsfHdr.HdrMagic)
                    {
                        fileKind = FileKind.PDB;
                        fileSubKind = (int) PDBFileKind.V2;
                        return true;
                    }

                    if (hdr == OHDR.OHdrMagic)
                    {
                        fileKind = FileKind.PDB;
                        fileSubKind = (int) PDBFileKind.V1;
                        return true;
                    }
                }
            }

            //If we have a known machine type, assume OBJ

            //My best guess as to how to determine whether its an OBJ file or not is to check for known IMAGE_FILE_MACHINE values.
            //And indeed, that's what cvdump in microsoft-pdb does too

            switch ((IMAGE_FILE_MACHINE) twoLetterSignature)
            {
                case IMAGE_FILE_MACHINE.UNKNOWN:
                    if (length >= AnonObjectHeader.StructSize && *(short*) (mmf.Address + 2) == -1) //Sig1: IMAGE_FILE_MACHINE_UNKNOWN and Sig2: -1
                    {
                        //Anon Header Obj
                        fileKind = FileKind.OBJ;
                        return true;
                    }
                    break;

                case IMAGE_FILE_MACHINE.I386:
                case IMAGE_FILE_MACHINE.R3000:
                case IMAGE_FILE_MACHINE.R4000:
                case IMAGE_FILE_MACHINE.R10000:
                case IMAGE_FILE_MACHINE.WCEMIPSV2:
                case IMAGE_FILE_MACHINE.ALPHA:
                case IMAGE_FILE_MACHINE.SH3:
                case IMAGE_FILE_MACHINE.SH3DSP:
                case IMAGE_FILE_MACHINE.SH3E:
                case IMAGE_FILE_MACHINE.SH4:
                case IMAGE_FILE_MACHINE.SH5:
                case IMAGE_FILE_MACHINE.ARM:
                case IMAGE_FILE_MACHINE.THUMB:
                case IMAGE_FILE_MACHINE.ARMNT:
                case IMAGE_FILE_MACHINE.AM33:
                case IMAGE_FILE_MACHINE.POWERPC:
                case IMAGE_FILE_MACHINE.POWERPCFP:
                case IMAGE_FILE_MACHINE.IA64:
                case IMAGE_FILE_MACHINE.MIPS16:
                case IMAGE_FILE_MACHINE.ALPHA64: //Same value as AXP64
                case IMAGE_FILE_MACHINE.MIPSFPU:
                case IMAGE_FILE_MACHINE.MIPSFPU16:
                case IMAGE_FILE_MACHINE.TRICORE:
                case IMAGE_FILE_MACHINE.CEF:
                case IMAGE_FILE_MACHINE.EBC:
                case IMAGE_FILE_MACHINE.AMD64:
                case IMAGE_FILE_MACHINE.M32R:
                case IMAGE_FILE_MACHINE.ARM64:
                case IMAGE_FILE_MACHINE.CEE:
                    if (length >= ImageFileHeader.StructSize)
                    {
                        var numSections = (*(ushort*) (mmf.Address + 2));

                        //If NumberOfSections is garbage, it may indicate that there are more sections than the size of the file.
                        //We'll also have an additional sanity check that there's at least 1 section (in case we've got a random file with 0 in the number of sections slot)
                        var minNumBytes = ImageFileHeader.StructSize + (numSections * ImageSectionHeader.StructSize);

                        if (numSections > 0 && minNumBytes <= length)
                        {
                            fileKind = FileKind.OBJ;
                            return true;
                        }
                    }

                    break;
            }

            //Maybe an OMF file? Only the first byte is used, so it's important that this is after all other kinds that use
            //more than 1 byte
            switch (*(OMFRecordType*) mmf.Address)
            {
                //An OMF "file" can start with THEADR or LHEADR, however it should only start with LHEADR when the file
                //is embedded inside of an outer LIB file

                case OMFRecordType.THEADR:
                    if (length >= 5) //A THEADR record is at least 5 bytes (record type (1), record length (2), string length (1), checksum (1))
                    {
                        fileKind = FileKind.OMF;
                        return true;
                    }
                    break;

                case OMFRecordType.LIBHDR:
                    if (length >= 10) //A LIBHDR record is at least 10 bytes (record type (1), record length (2), dictionary offset (4), dictionary size (2), flags (1))
                    {
                        fileKind = FileKind.OMFLIB;
                        return true;
                    }
                    break;
            }

            //Unknown value. Not a valid file
            return false;
        }

        private static bool IsValidMachine(IMAGE_FILE_MACHINE machine)
        {
            switch (machine)
            {
                case IMAGE_FILE_MACHINE.UNKNOWN:
                case IMAGE_FILE_MACHINE.I386:
                case IMAGE_FILE_MACHINE.R3000:
                case IMAGE_FILE_MACHINE.R4000:
                case IMAGE_FILE_MACHINE.R10000:
                case IMAGE_FILE_MACHINE.WCEMIPSV2:
                case IMAGE_FILE_MACHINE.ALPHA:
                case IMAGE_FILE_MACHINE.SH3:
                case IMAGE_FILE_MACHINE.SH3DSP:
                case IMAGE_FILE_MACHINE.SH3E:
                case IMAGE_FILE_MACHINE.SH4:
                case IMAGE_FILE_MACHINE.SH5:
                case IMAGE_FILE_MACHINE.ARM:
                case IMAGE_FILE_MACHINE.THUMB:
                case IMAGE_FILE_MACHINE.ARMNT:
                case IMAGE_FILE_MACHINE.AM33:
                case IMAGE_FILE_MACHINE.POWERPC:
                case IMAGE_FILE_MACHINE.POWERPCFP:
                case IMAGE_FILE_MACHINE.IA64:
                case IMAGE_FILE_MACHINE.MIPS16:
                case IMAGE_FILE_MACHINE.ALPHA64: //Same value as AXP64
                case IMAGE_FILE_MACHINE.MIPSFPU:
                case IMAGE_FILE_MACHINE.MIPSFPU16:
                case IMAGE_FILE_MACHINE.TRICORE:
                case IMAGE_FILE_MACHINE.CEF:
                case IMAGE_FILE_MACHINE.EBC:
                case IMAGE_FILE_MACHINE.AMD64:
                case IMAGE_FILE_MACHINE.M32R:
                case IMAGE_FILE_MACHINE.ARM64:
                case IMAGE_FILE_MACHINE.CEE:
                    return true;

                default:
                    return false;
            }
        }
    }
}
