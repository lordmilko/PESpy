#if PEFAST
using System.IO;
using ClrDebug;
using PESpy.Native;
using PESpy.PDB;

//Having out IFile? is confusing from an API standpoint because the caller has to keep doing file! whenever they use it when we returned true.
//Attributes to say we have a value when we return true haven't worked for me in the past
#nullable disable

namespace PESpy
{
    static class Detector
    {
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
                var twoLetterSignature = *(ushort*) mmf.Address;

                if (length >= ImageDosHeader.StructSize && twoLetterSignature == ImageDosHeader.IMAGE_DOS_SIGNATURE) //MZ
                {
                    //COFF, PE or NE

                    var fileAddressOfNewExeHeader = *(int*) (mmf.Address + 60);

                    if (fileAddressOfNewExeHeader >= length)
                        return false; //Probably a DOS file

                    var sig = *(uint*) (mmf.Address + fileAddressOfNewExeHeader);

                    if (sig == ImageNtHeaders.IMAGE_NT_SIGNATURE)
                    {
                        file = new PEFile(fs.Name, mmf);
                        return true;
                    }

                    sig &= 0xFFFF; //NE header is 2 bytes not 4. First two bytes will be junk due to little endian read

                    if (sig == ImageNtHeaders.IMAGE_OS2_SIGNATURE)
                    {
                        file = new NEFile(fs.Name, mmf);
                        return true;
                    }
                }
                else if (length >= ImageSeparateDebugHeader.StructSize && twoLetterSignature == ImageSeparateDebugHeader.IMAGE_SEPARATE_DEBUG_SIGNATURE) //DI
                {
                    file = new DBGFile(fs.Name, mmf);
                    return true;
                }

                if (length >= IMAGE_ARCHIVE_MEMBER_HEADER.IMAGE_ARCHIVE_START_SIZE) //!<arch>\n
                {
                    var libSig = new FixedAnsiString(mmf.Address, IMAGE_ARCHIVE_MEMBER_HEADER.IMAGE_ARCHIVE_START_SIZE);

                    if (libSig == IMAGE_ARCHIVE_MEMBER_HEADER.IMAGE_ARCHIVE_START)
                    {
                        file = new LIBFile(fs.Name, mmf);
                        return true;
                    }
                }

                if (length >= BigMsfHdr.BigHdrMagic.Length) //Microsoft C/C++ MSF 7.00\r\n\u001aDS\0\0\0
                {
                    var bigHdr = new FixedAnsiString(mmf.Address, BigMsfHdr.BigHdrMagic.Length);

                    if (bigHdr == BigMsfHdr.BigHdrMagic)
                    {
                        file = new PDB7File(fs.Name, mmf);
                        return true;
                    }

                    if (length >= MsfHdr.HdrMagic.Length) //V1 and V2 are both 44 bytes
                    {
                        var hdr = new FixedAnsiString(mmf.Address, MsfHdr.HdrMagic.Length);

                        if (hdr == MsfHdr.HdrMagic)
                        {
                            file = new PDB2File(fs.Name, mmf);
                            return true;
                        }

                        if (hdr == OHDR.OHdrMagic)
                        {
                            file = new PDB1File(fs.Name, mmf);
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
                            file = new OBJFile(fs.Name, mmf);
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
                        file = new OBJFile(fs.Name, mmf);
                        return true;
                }

                //Unknown value. Not a valid file
                return false;
            }
            finally
            {
                //If we got a file, ownership of the MMF transfers to the file
                if (file == null)
                    mmf.Close();
            }
        }
    }
}
#endif
