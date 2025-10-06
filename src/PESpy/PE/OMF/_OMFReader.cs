using ClrDebug.OMF;
using System.Collections.Generic;
using System;
using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy
{
    /* https://web.archive.org/web/20160909082838/http://pierrelib.pagesperso-orange.fr/exec_formats/MS_Symbol_Type_v1.0.pdf pdf page 71
     * 
     * There are two kinds of OMF data
     * - Data embedded in PE Files
     * - Data embedded in pre-PE Files
     * 
     * Regardless of whether a file is a PE File or not, OMF-embedded data seems to have the property of having an NBxx signature
     * at the end of the file, followed by lfoBase which tells us how many bytes to rewind from the end of the file to find another NBxx
     * signature, that is then immediately followed by lfoDir, the subsection tables, and the Subsection Directory header that lfoDir
     * points to, describing the types of subsection tables that precede it.
     * 
     * There are two OMF formats
     * - NB02, which covers NB00, NB01 and NB02. Microsoft considers NB00 and NB01 obsolete, so refers to this old format as the NB02 format,
     *   even though it technically precedes NB02
     * - NB05, which is the new format that covers all versions NB05 and newer. These use the OMF* structs that are normally associated with OMF
     */

    public static unsafe class OMFReader
    {
        //startAddress should be the start address of the file
        //length should be the total length of the file
        //globalBlock should be a block that is capable of accessing the entire file
        internal static bool TryReadTrailingOMF(byte* startAddress, int length, MemoryBlock globalBlock, out ICodeView? omfData)
        {
            var endOfFile = startAddress + length;

            var endSig = (OMFSignature*) (endOfFile - 8);

            var oldStyle = false;

            omfData = default;

            switch ((CodeViewSig) endSig->Signature)
            {
                case CodeViewSig.DNRB:
                    /* DNRB (also called CV_OLD_SIG) works as follows:
                     * 1. At the end of the file is a CVINFO structure
                     *
                     *     struct CVINFO
                     *     {
                     *         char signature[4];
                     *         long secTblOffset;
                     *     }
                     *
                     * This structure has the same shape as OMFSignature. However, unlike in OMF where the offset after
                     * the signature is an lfoBase, describing the number of bytes to rewind from the end of the file,
                     * secTblOffset describes the _absolute position_ within the file where the section table can be found.
                     *
                     * The section table has the following format
                     *
                     *     struct CVSECTBL
                     *     {
                     *         long secOffset[5];
                     *         unsigned int version;
                     *     }
                     *
                     * There are exactly 5 sections pointed to by the section table. Each value in secOffset describes
                     * the absolute position within the file that that section begins at. The meanings of each section
                     * is currently unknown
                     */

                    //We expect that this value should be the same value as the start of the overlay
                    var dataStart = endSig->filepos;
                    var dataLength = length - dataStart;

                    ReadDNRB(new MemoryChunk(globalBlock, dataStart), length - dataStart);

                    throw new NotImplementedException("DNRB not yet implemented");

                case CodeViewSig.NB00:
                case CodeViewSig.NB01:
                case CodeViewSig.NB02:
                    //case CodeViewSig.NB03: //Unknown IBM(?) format. Not currently supported
                    //case CodeViewSig.NB04: //Unknown IBM(?) format. Not currently supported
                    oldStyle = true;
                    break;

                case CodeViewSig.NB05:
                case CodeViewSig.NB06:
                case CodeViewSig.NB07:
                case CodeViewSig.NB08:
                case CodeViewSig.NB09:
                //case CodeViewSig.NB10: //NB10 is used for PDBs, and should not be found in an OMFSignature
                case CodeViewSig.NB11:
                    break;

                default:
                    return false;
            }

            var lfoBase = endSig->filepos;

            var startPos = (endOfFile - lfoBase);

            var startSig = (OMFSignature*) startPos;

            //We would expect to have the same signature
            if (startSig->Signature != endSig->Signature)
                return false;

            //It's a match!
            var startOffset = length - lfoBase;
            var omfLength = lfoBase - 8; //lfoBase describes how many bytes to rewind from the end of the file to find the data, which means it also describes the length of the data to the end of the file

            var chunk = new MemoryChunk(globalBlock, startOffset);

            if (oldStyle)
                ReadNB02(chunk, omfLength);
            else
                omfData = ReadNB05(chunk, (CodeViewSig) startSig->Signature, lfoBase, omfLength);

            return true;
        }

        private static void ReadDNRBModules(in MemoryChunk chunk, NativeSpan<int> secOffset)
        {
            //The first section appears to contain names of lib files and symbols that are associated with them.
            //Each symbol contains 30 unknown bytes prior to it. Seems similar to sstModules?

            var read = 0;
            var toRead = secOffset[1] - secOffset[0];
            var moduleReader = chunk.Slice(secOffset[0] - chunk.AbsoluteOffset);

            var modules = new List<(NativeSpan<byte> bytes, FixedAnsiString name)>();

            while (read < toRead)
            {
                var leadingBytes = moduleReader.PeekNativeSpan<byte>(read, 30);
                read += 30;
                var length = moduleReader.PeekByte(read);
                var str = moduleReader.PeekAnsiFixedLength(read + 1, length);
                read += length + 1;

                modules.Add((leadingBytes, str));
            }
        }

        private static void ReadDNRBPublics(in MemoryChunk chunk, NativeSpan<int> secOffset)
        {
            //Next is a list of function names. Seems similar to sstPublics?
            //Each item consists of 6 bytes, followed by a length prefixed string.
            //This is literally the same as sstPublics

            var read = 0;
            var toRead = secOffset[2] - secOffset[1];
            var publicsReader = chunk.Slice(secOffset[1] - chunk.AbsoluteOffset);

            //Offset is definitely offset, and I've also observed that segment seems to match what we see in the relocations
            var publics = new List<(ushort moffset, ushort segment, ushort maybeTypeIndex, FixedAnsiString name)>();

            //The entry point is called "_astart" and is listed in publics
            while (read < toRead)
            {
                var offset = publicsReader.PeekUInt16(read);
                var segment = publicsReader.PeekUInt16(read + 2);
                var maybeTypeIndex = publicsReader.PeekUInt16(read + 4);

                read += 6;
                var length = publicsReader.PeekByte(read);
                var str = publicsReader.PeekAnsiFixedLength(read + 1, length);
                read += length + 1;

                publics.Add((offset, segment, maybeTypeIndex, str));
            }
        }
        #region NB00 -> NB02

        internal static void ReadNB02(in MemoryChunk chunk, int sizeOfData)
        {
            /* The Microsoft C 6.0 Developer's Toolkit Reference contains a lot of useful
             * information on how to parse all the types of records in NB02, and even contains some typedefs
             * https://www.pcjs.org/documents/books/mspl13/c/ctoolkit/
             *
             * (see section 3.7)
             *
             * The following lists the types mentioned in section 3.7 and compares them with the types
             * listed in cvexefmt.h
             *
             * cvexefmt.h
             * ----------
             * DirEntry
             * oldnsg (describes each segment in a module)
             * oldsmd (subsection module info)
             * oldnsg32
             * oldsmd32
             *
             * Section 3.7
             * -----------
             * DNT (same as DirEntry)
             *
             * SSTMODULES
             * ----------
             * nsg (same as oldnsg, except oldnsg explicitly says short, whereas nsg just says "unsigned")
             * smd (same as oldsmd. Same issues with "unsigned")
             *
             * SSTPUBLICS
             * ----------
             * pbi
             *
             * SSTTYPES
             * SSTCOMPACTED
             * SSTSYMBOLS
             * SSTSRCLINES
             * -----------
             * loe
             *
             * SSTSRCLNSEG
             * -----------
             * a different kind of loe(?)
             *
             * SSTLIBRARIES
             * ------------
             * lib[]
             */

            //cvdump doesn't seem to like NB02 files from Windows 3.1 for some reason. I found a GitHub repo that says that the NB02
            //dumper contains a fundamental bug, and includes a patch

            var lfoBase = chunk.Pointer;

            var lfoDir = chunk.PeekInt32(4);

            //In NB02 there's no OMFDirHeader. It's just a 16-bit count of directories
            var cDir = chunk.PeekUInt16(lfoDir);

            //Instead of OMFDirEntry, we have DirEntry items. They are basically the same as OMFDirEntry except the size is 16-bit
            var entries = new dnt[cDir];
            var tableData = new object[cDir];

            //This is the table in section 7.3
            var offset = lfoDir + sizeof(ushort);

            //NB02 data is a bit tricky in that things can sometimes be 16-bit, other times 32-bit. I have seen indications that 32-bit
            //applies when we have an LE File.

            for (var i = 0; i < cDir; i++)
            {
                //cvdump.cpp collects info about sections and then calls various dump methods for each type.
                //e.g. DumpPub retrieves the individual fields of a public

                entries[i] = new dnt(chunk.Slice(offset), chunk);

                offset += dnt.StructSize;
            }
        }

        internal static OldSymType[] ReadNB02Symbols(in MemoryChunk symbolsChunk, int size)
        {
            var read = 0;

            using var list = new PooledList<OldSymType>();

            while (read < size)
            {
                var ptr = new OldSymType(symbolsChunk.Pointer + read);

                list.Add(ptr);

                read += ptr.reclen + 1;
            }

            return list.ToArray();
        }

        #endregion
        #region NB05+

        internal static NB05Data ReadNB05(in MemoryChunk chunk, CodeViewSig sig, int lfoBaseOff, int sizeOfData)
        {
            /* NB05-NB11 have the same format. The individual versions seem to just indicate which linker was used and whether the file was packed or not.
             * The only substantive difference seems to be when dumping globals, if it's NB09 or NB11 there's no OMFSymHash offset to consider (see dympsym7.cpp!DumpGlobal)
             * 
             * For OMF executables (not PE Files) the last 8 bytes of the file contain a signature (NBxx) and a Long File Offset
             * from the end of the file (lfoBase). lfaBase = length of file - lfoBase
             * 
             * Data pointed to by PointerToRawData:
             * - NB11
             * - 0x0001f444 (lfoDir) - corresponds to lfoDirectory (Offset of directory from base address)
             * - Subsection Tables
             * - Subsection Directory (at lfaBase + lfoDir)
             */

            var lfoBase = chunk.Pointer;

            //Step 1: read lfoDir
            var lfoDir = chunk.PeekInt32(4);

            /* Step 2: skip over the subsection tables and read the subsection directory at lfaBase + lfoDir.
             * lfaBase is the PointerToRawData of the debug directory data, and lfoDir is the offset from this base address that the subsection directory is located
             * The subsection directory, which consists of a header followed by a bunch of entries */
            var dirHeader = new OMFDirHeader(chunk.Slice(lfoDir));

            var entries = new OMFDirEntry[dirHeader.cDir];

            NB05SymbolAccessor symbolAccessor = null;
            var file = chunk.File();

            switch (sig)
            {
                case CodeViewSig.NB05:
                {
                    if (file is DOSFile d)
                        symbolAccessor = new DOSNB05SymbolAccessor(d);
                    else
                        symbolAccessor = new NB05SymbolAccessor(file);
                }
                break;

                case CodeViewSig.NB06:
                case CodeViewSig.NB07:
                case CodeViewSig.NB08:
                case CodeViewSig.NB09:
                //case CodeViewSig.NB10: //NB10 is used for PDBs, and should not be found in an OMFSignature
                case CodeViewSig.NB11:
                {
                    if (file is DOSFile d)
                        symbolAccessor = new DOSNB09SymbolAccessor(d);
                    else
                        symbolAccessor = new NB09SymbolAccessor(file);
                }
                break;
            }

            //The subsection directory entries are immediately after the header. This is the table in section 7.3.
            //It is _not_ the table in section 7.2
            var offset = lfoDir + OMFDirHeader.StructSize;

            CV_SIGNATURE lastSignature = default;

            for (var i = 0; i < dirHeader.cDir; i++)
            {
                entries[i] = new OMFDirEntry(chunk.Slice(offset), chunk, symbolAccessor, ref lastSignature);                

                offset += OMFDirEntry.StructSize;
            }

            var data = new NB05Data(
                chunk,
                sig,
                lfoBaseOff,
                lfoDir,
                dirHeader,
                entries
            );

            symbolAccessor.data = data;

            Debug.Assert(lastSignature != default);
            symbolAccessor.CvSignature = lastSignature;
            //C13 uses UTF8; C7 and C11 use length prefixed. Not sure about C6
            symbolAccessor.HasLengthPrefixedStrings = lastSignature != CV_SIGNATURE.C13;

            return data;
        }

        #endregion
    }
}
