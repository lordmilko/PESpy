using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ClrDebug;
using PESpy.ISO;
using PESpy.Native;
using PESpy.NE;
using PESpy.OMF;
using PESpy.PDB;
using PESpy.SYM;
using static ClrDebug.IMAGE_FILE_MACHINE;

//Having out IFile? is confusing from an API standpoint because the caller has to keep doing file! whenever they use it when we returned true.
//Attributes to say we have a value when we return true haven't worked for me in the past
#nullable disable

namespace PESpy
{
    public static class Detector
    {
        #region OpenFile

        public static IFile OpenFile(string path)
        {
            IFile file;

            if ((file = TryOpenFile(path)) == null)
            {
                if (!File.Exists(path))
                    throw new FileNotFoundException($"Could not find file '{path}'");

                throw new InvalidOperationException($"Failed to detect the type of file '{path}'");
            }

            return file;
        }

        /// <summary>
        /// Attempts to open the specified file, or returns <see langword="null"/> if the file cannot be opened.<para/>
        /// Contrary to what you might expect, it is safe to define a <see langword="using"/> statement against a resource
        /// that may be null. This safety is guaranteed by ECMA-334 §13.14 which defines the behavior of the <see langword="using"/>
        /// statement.
        /// </summary>
        /// <param name="path">The path to the file to try and open</param>
        /// <returns>The file that was opened, or <see langword="null"/> if the file could not be opened.</returns>
        public static unsafe IFile TryOpenFile(string path)
        {
            TryOpenFile(path, out var file);
            return file;
        }
        
        public static unsafe bool TryOpenFile(string path, out IFile file)
        {
            file = null;

            if (!File.Exists(path))
                return false;

            using var fs = File.OpenRead(path);

            var length = fs.Length;

            if (length < 2) //All signatures require at least 2 bytes
                return false;

            var mmf = new MemoryMappedFileHolder(fs);

            try
            {
                return TryOpenFileFromMMF(path, mmf, length, out file);
            }
            finally
            {
                //If we got a file, ownership of the MMF transfers to the file
                if (file == null)
                    mmf.Dispose();
            }
        }

        private static unsafe bool TryOpenFileFromMMF(
            string path,
            in MemoryMappedFileHolder mmf,
            long length,
            out IFile file)
        {
            if (TryDetectFile(path, mmf, length, out var kind, out var subKind, out var decompressionInfo))
            {
                if (decompressionInfo.Bytes != null)
                {
                    file = OpenFileFromBytes(path, decompressionInfo, kind, subKind);
                }
                else
                {
                    file = OpenFileFromMMF(path, mmf, kind, subKind);
                    return true;
                }

            }

            file = default;
            return false;
        }

        public static unsafe bool TryOpenFile(DirectoryRecord directoryRecord, out IFile file)
        {
            file = default;

            var bytes = directoryRecord.Bytes;

            if (bytes.Length < 2) //All signatures require at least 2 bytes
                return false;

            var mmf = new MemoryMappedFileHolder(bytes, bytes.Length);

            return TryOpenFileFromMMF(directoryRecord.FullPath, mmf, bytes.Length, out file);
        }

        private static IFile OpenFileFromMMF(
            string fileName,
            in MemoryMappedFileHolder mmf,
            FileKind kind,
            int subKind)
        {
            return kind switch
            {
                FileKind.PE => new PEFile(fileName, mmf),
                FileKind.NE => new NEFile(fileName, mmf),
                FileKind.LE => new LEFile(fileName, mmf),
                FileKind.DOS => new DOSFile(fileName, mmf),
                FileKind.DBG => new DBGFile(fileName, mmf),
                FileKind.LIB => new LIBFile(fileName, mmf),
                FileKind.OBJ => new OBJFile(fileName, mmf),
                FileKind.OMF => new OMFFile(fileName, mmf),
                FileKind.SYM => new SYMFile(fileName, mmf),
                FileKind.PDB => ((PDBFileKind) subKind) switch
                {
                    PDBFileKind.V1 => new PDB1File(fileName, mmf),
                    PDBFileKind.V2 => new PDB2File(fileName, mmf),
                    PDBFileKind.V7 => new PDB7File(fileName, mmf),
                    _ => throw new NotImplementedException($"Don't know how to handle a PDB of sub-type '{(PDBFileKind) subKind}'")
                },
                FileKind.OMFLIB => new OMFLIBFile(fileName, mmf),
                FileKind.OMFDBG => new OMFDBGFile(fileName, mmf),
                FileKind.Resource => new ResourceFile(fileName, mmf),
                FileKind.PortablePDB => new PortablePDBFile(fileName, mmf)
            };
        }

        private static IFile OpenFileFromBytes(
            string fileName,
            DecompressionInfo decompressionInfo,
            FileKind kind,
            int subKind)
        {
            TryGetUncompressedFileName(fileName, decompressionInfo.ExtensionChar, out var name);

            var mmf = new MemoryMappedFileHolder(decompressionInfo.Bytes);

            try
            {
                return kind switch
                {
                    FileKind.PE => new PEFile(fileName, mmf, name: name),
                    FileKind.NE => new NEFile(fileName, mmf, name),
                    FileKind.LE => new LEFile(fileName, mmf, name),
                    FileKind.DOS => new DOSFile(fileName, mmf, name),
                    FileKind.DBG => new DBGFile(fileName, mmf, name),
                    FileKind.LIB => new LIBFile(fileName, mmf, name),
                    FileKind.OBJ => new OBJFile(fileName, mmf, name),
                    FileKind.OMF => new OMFFile(fileName, mmf, name),
                    FileKind.SYM => new SYMFile(fileName, mmf, name),
                    FileKind.PDB => ((PDBFileKind) subKind) switch
                    {
                        PDBFileKind.V1 => new PDB1File(fileName, mmf, name),
                        PDBFileKind.V2 => new PDB2File(fileName, mmf, name),
                        PDBFileKind.V7 => new PDB7File(fileName, mmf, name),
                        _ => throw new NotImplementedException($"Don't know how to handle a PDB of sub-type '{(PDBFileKind) subKind}'")
                    },
                    FileKind.OMFLIB => new OMFLIBFile(fileName, mmf, name),
                    FileKind.Resource => new ResourceFile(fileName, mmf, name: name),
                    FileKind.PortablePDB => new PortablePDBFile(fileName, mmf, name)
                };
            }
            catch
            {
                mmf.Dispose();

                throw;
            }
        }

        #endregion
        #region DetectFile

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

            if (length < 4) //The most basic signatures require 2-4 bytes. Nothing is happening in a file this small
                return false;

            var mmf = new MemoryMappedFileHolder(fs);

            try
            {
                return TryDetectFile(path, mmf, length, out fileKind, out fileSubKind, out _);
            }
            finally
            {
                //If we got a file, ownership of the MMF transfers to the file
                mmf.Dispose();
            }
        }

        public static unsafe bool TryDetectFile(
            DirectoryRecord directoryRecord,
            out FileKind fileKind,
            out int fileSubKind)
        {
            if ((directoryRecord.FileFlags & FileFlags.Directory) != 0)
            {
                fileKind = default;
                fileSubKind = default;
                return false;
            }

            var bytes = directoryRecord.Bytes;

            var mmf = new MemoryMappedFileHolder(bytes, bytes.Length);

            return TryDetectFile(string.Empty, mmf, mmf.Length, out fileKind, out fileSubKind, out _);
        }

        internal static unsafe bool TryDetectFile(
            string path,
            MemoryMappedFileHolder mmf,
            long length,
            out FileKind fileKind,
            out int fileSubKind,
            out DecompressionInfo decompressionInfo)
        {
            fileKind = default;
            fileSubKind = default;
            decompressionInfo = default;

            var twoLetterSignature = *(ushort*) mmf.Address;

            if (length >= ImageDosHeader.StructSize && twoLetterSignature == ImageDosHeader.IMAGE_DOS_SIGNATURE) //MZ
            {
                //COFF, PE or NE

                var fileAddressOfNewExeHeader = *(int*) (mmf.Address + ImageDosHeader.FileAddressOfNewExeHeaderOffset);

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

                if (IsValidMachine((IMAGE_FILE_MACHINE) (*(ushort*) (mmf.Address + ImageSeparateDebugHeader.MachineOffset))))
                {
                    var numberOfSections = *(int*) (mmf.Address + ImageSeparateDebugHeader.NumberOfSectionsOffset);
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

            if (TryExtract(mmf, out decompressionInfo))
            {
                fixed (byte* pBytes = decompressionInfo.Bytes)
                {
                    //On success, our decompressionInfo has already been stored in the out parameter above
                    return TryDetectFile(
                        path,
                        new MemoryMappedFileHolder(pBytes, decompressionInfo.Bytes.Length),
                        decompressionInfo.Bytes.Length,
                        out fileKind,
                        out fileSubKind,
                        out _
                    );
                }
            }

            //If we have a known machine type, assume OBJ

            //My best guess as to how to determine whether its an OBJ file or not is to check for known IMAGE_FILE_MACHINE values.
            //And indeed, that's what cvdump in microsoft-pdb does too

            switch ((IMAGE_FILE_MACHINE) twoLetterSignature)
            {
                case IMAGE_FILE_MACHINE_UNKNOWN:
                    if (length >= AnonObjectHeader.StructSize && *(short*) (mmf.Address + AnonObjectHeader.Sig2Offset) == -1) //Sig1: IMAGE_FILE_MACHINE_UNKNOWN and Sig2: -1
                    {
                        //Anon Object Header

                        var version = *(short*) (mmf.Address + AnonObjectHeader.VersionOffset);
                        int listedSize;

                        switch (version)
                        {
                            case 1:
                                listedSize = *(int*) (mmf.Address + AnonObjectHeader.SizeOfDataOffset) + AnonObjectHeader.StructSize;
                                
                                if (listedSize <= length)
                                {
                                    fileKind = FileKind.OBJ;
                                    return true;
                                }

                                break;

                            case 2:
                                if (length >= AnonObjectHeaderV2.StructSize)
                                {
                                    var guid = *(Guid*) (mmf.Address + AnonObjectHeader.ClassIDOffset);

                                    //If the listed size isn't valid, there's no hope

                                    //Not sure if SizeOfData is the size of everything after that field,
                                    //or the size after the whole big obj header, so we'll just go with the minimum
                                    //size which is the size of an AnonObjectHeader
                                    listedSize = *(int*) (mmf.Address + AnonObjectHeader.SizeOfDataOffset) + AnonObjectHeader.StructSize;

                                    if (listedSize <= length)
                                    {
                                        //We're potentially looking at a V2 or Big Obj
                                        //If it's a known GUID, apply additional constraints to try and check
                                        //validity; otherwise, accept as is

                                        if (guid == AnonObjectHeader.EXTENDED_COFF_OBJ_GUID || guid == AnonObjectHeader.LtcgObjGuid)
                                        {
                                            var numSections = *(uint*) (mmf.Address + AnonObjectHeaderBigObj.NumberOfSectionsOffset);
                                            var pointerToSymbolTable = *(int*) (mmf.Address + AnonObjectHeaderBigObj.PointerToSymbolTableOffset);

                                            var minNumBytes = AnonObjectHeaderBigObj.StructSize + (numSections * ImageSectionHeader.StructSize);

                                            if (pointerToSymbolTable < length && minNumBytes < length)
                                            {
                                                fileKind = FileKind.OBJ;
                                                return true;
                                            }
                                        }
                                        else
                                        {
                                            //We've done our best; accept as V2
                                            fileKind = FileKind.OBJ;
                                            return true;
                                        }
                                    }
                                }
                                break;
                        }                        
                    }
                    break;

                case IMAGE_FILE_MACHINE_I386:
                case IMAGE_FILE_MACHINE_R3000:
                case IMAGE_FILE_MACHINE_R4000:
                case IMAGE_FILE_MACHINE_R10000:
                case IMAGE_FILE_MACHINE_WCEMIPSV2:
                case IMAGE_FILE_MACHINE_ALPHA:
                case IMAGE_FILE_MACHINE_SH3:
                case IMAGE_FILE_MACHINE_SH3DSP:
                case IMAGE_FILE_MACHINE_SH3E:
                case IMAGE_FILE_MACHINE_SH4:
                case IMAGE_FILE_MACHINE_SH5:
                case IMAGE_FILE_MACHINE_ARM:
                case IMAGE_FILE_MACHINE_THUMB:
                case IMAGE_FILE_MACHINE_ARMNT:
                case IMAGE_FILE_MACHINE_AM33:
                case IMAGE_FILE_MACHINE_POWERPC:
                case IMAGE_FILE_MACHINE_POWERPCFP:
                case IMAGE_FILE_MACHINE_IA64:
                case IMAGE_FILE_MACHINE_MIPS16:
                case IMAGE_FILE_MACHINE_ALPHA64: //Same value as AXP64
                case IMAGE_FILE_MACHINE_MIPSFPU:
                case IMAGE_FILE_MACHINE_MIPSFPU16:
                case IMAGE_FILE_MACHINE_TRICORE:
                case IMAGE_FILE_MACHINE_CEF:
                case IMAGE_FILE_MACHINE_EBC:
                case IMAGE_FILE_MACHINE_AMD64:
                case IMAGE_FILE_MACHINE_M32R:
                case IMAGE_FILE_MACHINE_ARM64:
                case IMAGE_FILE_MACHINE_CEE:
                    if (length >= ImageFileHeader.StructSize)
                    {
                        var numSections = (*(ushort*) (mmf.Address + ImageFileHeader.NumberOfSectionsOffset));

                        //If NumberOfSections is garbage, it may indicate that there are more sections than the size of the file.
                        //We'll also have an additional sanity check that there's at least 1 section (in case we've got a random file with 0 in the number of sections slot)
                        var minNumBytes = ImageFileHeader.StructSize + (numSections * ImageSectionHeader.StructSize);

                        if (numSections > 0 && minNumBytes <= length)
                        {
                            //Finally, check whether the PointerToSymbolTable is valid
                            var pointerToSymbolTable = *(uint*) (mmf.Address + ImageFileHeader.PointerToSymbolTableOffset);

                            if (pointerToSymbolTable < length)
                            {
                                fileKind = FileKind.OBJ;
                                return true;
                            }
                        }
                    }

                    break;
            }

            //*.dbg files in DOS contain raw OMF data
            if (OMFReader.ContainsTrailingOMF(mmf.Address, (int) mmf.Length))
            {
                fileKind = FileKind.OMFDBG;
                return true;
            }

            //Maybe an OMF file? Only the first byte is used, so it's important that this is after all other kinds that use
            //more than 1 byte
            switch (*(OMFRecordType*) mmf.Address)
            {
                //An OMF "file" can start with THEADR or LHEADR, however it should only start with LHEADR when the file
                //is embedded inside of an outer LIB file

                case OMFRecordType.THEADR:
                    if (length >= 5 && *(ushort*) (mmf.Address + 1) >= 5) //A THEADR record is at least 5 bytes (record type (1), record length (2), string length (1), checksum (1))
                    {
                        fileKind = FileKind.OMF;
                        return true;
                    }
                    break;

                case OMFRecordType.LIBHDR:
                    if (length >= 10 && *(ushort*) (mmf.Address + 1) >= 10) //A LIBHDR record is at least 10 bytes (record type (1), record length (2), dictionary offset (4), dictionary size (2), flags (1))
                    {
                        fileKind = FileKind.OMFLIB;
                        return true;
                    }
                    break;
            }

            if (*((uint*) mmf.Address) == ResourceFile.MagicNumber)
            {
                fileKind = FileKind.Resource;
                return true;
            }

            var ext = Path.GetExtension(path);

            if (ext.Equals(".SYM", StringComparison.OrdinalIgnoreCase) && length > (mapdef_s.FixedStructSize + endmap_s.StructSize))
            {

                var addr = mmf.Address + length;

                var version = *(addr - 1);
                var release = *(addr - 2);

                //If it's a *.sym file, check the endmap_s for any supported version version >= 3.10 and <= 6.x
                //6.x was the last version. Versions earlier than 3.10 don't use paragraphs, which we don't currently support
                if ((version == 3 && release >= 10) || (version > 3 && version < 6) || (version == 6))
                {
                    fileKind = FileKind.SYM;
                    return true;
                }
            }

            //Unknown value. Not a valid file
            return false;
        }

        #endregion

        private static bool IsValidMachine(IMAGE_FILE_MACHINE machine)
        {
            switch (machine)
            {
                case IMAGE_FILE_MACHINE_UNKNOWN:
                case IMAGE_FILE_MACHINE_I386:
                case IMAGE_FILE_MACHINE_R3000:
                case IMAGE_FILE_MACHINE_R4000:
                case IMAGE_FILE_MACHINE_R10000:
                case IMAGE_FILE_MACHINE_WCEMIPSV2:
                case IMAGE_FILE_MACHINE_ALPHA:
                case IMAGE_FILE_MACHINE_SH3:
                case IMAGE_FILE_MACHINE_SH3DSP:
                case IMAGE_FILE_MACHINE_SH3E:
                case IMAGE_FILE_MACHINE_SH4:
                case IMAGE_FILE_MACHINE_SH5:
                case IMAGE_FILE_MACHINE_ARM:
                case IMAGE_FILE_MACHINE_THUMB:
                case IMAGE_FILE_MACHINE_ARMNT:
                case IMAGE_FILE_MACHINE_AM33:
                case IMAGE_FILE_MACHINE_POWERPC:
                case IMAGE_FILE_MACHINE_POWERPCFP:
                case IMAGE_FILE_MACHINE_IA64:
                case IMAGE_FILE_MACHINE_MIPS16:
                case IMAGE_FILE_MACHINE_ALPHA64: //Same value as AXP64
                case IMAGE_FILE_MACHINE_MIPSFPU:
                case IMAGE_FILE_MACHINE_MIPSFPU16:
                case IMAGE_FILE_MACHINE_TRICORE:
                case IMAGE_FILE_MACHINE_CEF:
                case IMAGE_FILE_MACHINE_EBC:
                case IMAGE_FILE_MACHINE_AMD64:
                case IMAGE_FILE_MACHINE_M32R:
                case IMAGE_FILE_MACHINE_ARM64:
                case IMAGE_FILE_MACHINE_CEE:
                    return true;

                default:
                    return false;
            }
        }

        private static ReadOnlySpan<byte> COMP_SIG => new byte[] { 0x53, 0x5A, 0x44, 0x44, 0x88, 0xF0, 0x27, 0x33 }; //"SZDD\x88\xf0\x27\x33"

        //Not described in NT 4, but is used in my WFH 3.11 install
        private static ReadOnlySpan<byte> KWAJ => new byte[] { 0x4B, 0x57, 0x41, 0x4A, 0x88, 0xF0, 0x27, 0xD1 }; //"KWAJ\x88\xf0\x27\xd1"

        public enum ALG : byte
        {
            ALG_FIRST = (byte) 'A',
            ALG_LZ = (byte) 'B',
            ALG_LZA = (byte) 'C',
        }

        internal readonly struct DecompressionInfo
        {
            public byte[] Bytes { get; init; }

            public char ExtensionChar { get; init; }
        }

        internal static bool TryGetUncompressedFileName(string path, char extChar, out string name)
        {
            name = null;

            //The extension char is often lowercase when it should be uppercase;
            //match whatever the case of the file is
            if (path.EndsWith("_"))
            {
                if (path.Length > 1)
                {
                    var secondLastChar = path[path.Length - 2];

                    extChar = char.IsUpper(secondLastChar) ? char.ToUpper(extChar) : char.ToLower(extChar);
                }

                var lastSlash = path.LastIndexOf(Path.DirectorySeparatorChar);

                if (lastSlash != -1)
                {
                    using var builder = new ValueStringBuilder();

                    builder.Append(path.AsSpan(lastSlash + 1));
                    builder[builder.Length - 1] = extChar;

                    name = builder.ToString();

                    return true;
                }
            }

            return false;
        }

        const int HEADER_LEN = 14; //cbulCompSize isn't counted in this
        const int COMP_SIG_LEN = 8;

        internal static unsafe bool TryExtract(MemoryMappedFileHolder mmf, out DecompressionInfo decompressionInfo)
        {
            decompressionInfo = default;

            if (mmf.Length < HEADER_LEN)
                return false;

            var sig = new Span<byte>(mmf.Address, COMP_SIG_LEN);

            if (sig.SequenceEqual(COMP_SIG))
                return TryExtractWinLZ(mmf, out decompressionInfo);

            if (sig.SequenceEqual(KWAJ))
                return TryExtractKWAJ(mmf, out decompressionInfo);

            return false;
        }

        private static unsafe bool TryExtractWinLZ(MemoryMappedFileHolder mmf, out DecompressionInfo decompressionInfo)
        {
            var byteAlgorithm = *(ALG*) (mmf.Address + COMP_SIG_LEN);
            var extChar = (char) *(mmf.Address + COMP_SIG_LEN + 1);
            var cbulUncompSize = *(uint*) (mmf.Address + COMP_SIG_LEN + 2);

            var ptr = mmf.Address + HEADER_LEN;
            var end = mmf.Address + mmf.Length;

            const int FIRST_MAX_MATCH_LEN = 16;
            const byte BUF_CLEAR_BYTE = (byte) ' ';
            const int RING_BUF_LEN = 4096;

            var outputPos = 0;
            var windowPos = RING_BUF_LEN - FIRST_MAX_MATCH_LEN;

            Span<byte> window = new byte[RING_BUF_LEN];

            Unsafe.InitBlockUnaligned(ref MemoryMarshal.GetReference(window), BUF_CLEAR_BYTE, RING_BUF_LEN);
            var output = new byte[cbulUncompSize];

            while (outputPos < cbulUncompSize)
            {
                if (ptr == end)
                    break;

                //If a given bit in flags is set, read the next byte.
                //Otherwise, there is a reference to some data we've seen previously;
                //apply it to the main output
                var flags = *ptr++;

                for (var i = 1; i < 0x100; i <<= 1)
                {
                    if ((flags & i) != 0)
                    {
                        if (ptr == end)
                            break;

                        var b = *ptr++;

                        window[windowPos++] = b;
                        output[outputPos++] = b;

                        windowPos &= RING_BUF_LEN - 1;
                    }
                    else
                    {
                        if (ptr == end)
                            break;

                        var b1 = *ptr++;

                        if (ptr == end)
                            break;

                        var b2 = *ptr++;

                        var matchPos = b1 | ((b2 & 0xF0) << 4);
                        var matchLen = (b2 & 0x0F) + 3;

                        matchPos &= RING_BUF_LEN - 1;

                        for (var j = 0; j < matchLen; j++)
                        {
                            var val = window[matchPos++];

                            window[windowPos++] = val;
                            output[outputPos++] = val;

                            windowPos &= RING_BUF_LEN - 1;
                            matchPos &= RING_BUF_LEN - 1;
                        }
                    }
                }
            }

            if (outputPos != cbulUncompSize)
            {
                decompressionInfo = default;
                return false;
            }

            decompressionInfo = new DecompressionInfo
            {
                Bytes = output,
                ExtensionChar = extChar
            };

            return true;
        }

        [Flags]
        enum KWAJHeaderFlags : short
        {
            HasLength = 1 << 0,
            Unknown1 = 1 << 1,
            Unknown2 = 1 << 2,
            HasFileName = 1 << 3,
            HasFileExtension = 1 << 4,
            HasText = 1 << 5
        }

        //https://www.cabextract.org.uk/libmspack/doc/szdd_kwaj_format.html
        enum KWAJCompressionMethod : short
        {
            None,
            NoneXorFF,
            QBasicSZDD,
            JeffJohnson,
            MSZIP
        }

        private static unsafe bool TryExtractKWAJ(MemoryMappedFileHolder mmf, out DecompressionInfo decompressionInfo)
        {
            var ptr = mmf.Address + COMP_SIG_LEN; //KWAJ has an 8 byte signature too;

            var compressionMethod = *(KWAJCompressionMethod*) ptr;
            ptr += 2;

            var compressedDataOffset = *(ushort*) ptr;
            ptr += 2;

            var headerFlags = *(KWAJHeaderFlags*) ptr;
            ptr += 2;

            int length;
            AnsiString fileName;
            AnsiString fileExtension;

            if ((headerFlags & KWAJHeaderFlags.HasLength) != 0)
            {
                length = *(int*) ptr;
                ptr += 4;
            }

            if ((headerFlags & KWAJHeaderFlags.Unknown1) != 0)
                ptr += 2;

            if ((headerFlags & KWAJHeaderFlags.Unknown2) != 0)
                ptr += 2;

            if ((headerFlags & KWAJHeaderFlags.HasFileName) != 0)
            {
                fileName = new AnsiString(ptr);
                ptr += fileName.Length + 1;
            }

            if ((headerFlags & KWAJHeaderFlags.HasFileExtension) != 0)
            {
                fileExtension = new AnsiString(ptr);
                ptr += fileExtension.Length + 1;
            }

            if ((headerFlags & KWAJHeaderFlags.HasText) != 0)
            {
                var size = *(ushort*) ptr;
                ptr += 2 + size;
            }

            //We should now be at the compressed data offset, but let's reset just in case
            ptr = mmf.Address + compressedDataOffset;
    }
}
