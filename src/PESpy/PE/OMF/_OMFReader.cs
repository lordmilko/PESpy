using System;
using System.Collections.Generic;
using System.Diagnostics;
using ClrDebug.OMF;
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

            var sig = (CodeViewSig) endSig->Signature;

            switch (sig)
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

                    omfData = ReadDNRB(new MemoryChunk(globalBlock, dataStart), sig, length - dataStart);
                    return true;

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
                omfData = ReadNB02(chunk, (CodeViewSig) startSig->Signature, lfoBase, omfLength);
            else
                omfData = ReadNB05(chunk, (CodeViewSig) startSig->Signature, lfoBase, omfLength);

            return true;
        }

        #region DNRB

        internal static DNRBData ReadDNRB(in MemoryChunk chunk, CodeViewSig sig, int sizeOfData)
        {
            /* See the comments in TryReadTrailingOMF() for info about the structure of DNRB
             *
             * For the most part, DNRB seems to be very similar to NB00 (https://www.pcjs.org/documents/books/mspl13/c/ctoolkit).
             * Part of this information has also been confirmed by cross referencing with the output of CV.EXE */

            //Read the CVSECTBL for CV_OLD_SIG
            var secOffset = chunk.PeekNativeSpan<int>(0, 5);
            var version = chunk.PeekUInt16(5 * sizeof(int)); //The type is listed as "unsigned", but based on the distance between this and the modules that come after it I think it's a ushort

            //Each offset is an absolute position in the file

            /* Since DNRB has a fixed number of sections, the length of each section can be deduced as being the distance between the current
             * section and the section after it. The length of the last section is the length from the start of the section up to 8 bytes
             * prior to the end of the file (which holds a CVINFO) */

            //Section 0: Modules. Does not match NB00
            var modules = ReadDNRBModules(chunk, secOffset);

            //Section 1: Publics. Matches NB00
            //Some of these offsets and segments seem a bit crazy, but offset is definitely offset, and I've also observed that segment seems to match what we see in the relocations
            var publics = ReadNB02Publics(chunk.Slice(secOffset[1] - chunk.AbsoluteOffset), secOffset[2] - secOffset[1]);

            //Section 2: Types. Matches NB00
            var types = ReadNB02Types(chunk.Slice(secOffset[2] - chunk.AbsoluteOffset), secOffset[3] - secOffset[2]);

            //Section 3: Symbols. Matches NB00
            var symbols = ReadNB02Symbols(chunk.Slice(secOffset[3] - chunk.AbsoluteOffset), secOffset[4] - secOffset[3]);

            //Section 4: Source Lines. Matches NB00
            var sourceLinesChunk = chunk.Slice(secOffset[4] - chunk.AbsoluteOffset);
            var sourceLines = ReadNB02SourceLines(chunk.Slice(secOffset[4] - chunk.AbsoluteOffset), sourceLinesChunk.Remaining - 8, false); //Read up to the CVINFO at the end of the file

            return new DNRBData(
                chunk.AbsoluteOffset,
                sig,
                sizeOfData,
                version,
                secOffset,
                modules,
                publics,
                types,
                symbols,
                sourceLines
            );
        }

        private static DNRBModule[] ReadDNRBModules(in MemoryChunk chunk, NativeSpan<int> secOffset)
        {
            //The first section appears to contain names of lib files and symbols that are associated with them.
            //Each symbol contains 30 unknown bytes prior to it. Seems similar to sstModules?

            var read = 0;
            var toRead = secOffset[1] - secOffset[0];
            var moduleReader = chunk.Slice(secOffset[0] - chunk.AbsoluteOffset);

            var modules = new List<DNRBModule>();

            //Doesn't seem to be the same as smd

            while (read < toRead)
            {
                var module = new DNRBModule(moduleReader.Slice(read));

                read += module.StructSize;

                modules.Add(module);
            }

            return modules.ToArray();
        }

        #endregion
        #region NB00 -> NB02

        internal static NB02Data ReadNB02(in MemoryChunk chunk, CodeViewSig sig, int lfoBaseOff, int sizeOfData)
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
             * The following types are listed above type "loe", but this is extremely dubious because loe is only used
             * for source line information
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
             * The same format as loe except there's also a segment field
             *
             * SSTLIBRARIES
             * ------------
             * lib[]
             */

            /* The version of cvdump.exe that comes with microsoft-pdb has a bug in it: cvexefmt.h is imported without changing the packing to 1,
             * which causes cvdump to fail to parse the directory entries properly, and you get an invalid executable error. If you compile cvdump yourself,
             * you can fix this by changing the import of cvexefmt.h to the following
             *
             * #pragma pack(1)
             * #include "cvexefmt.h"
             * #pragma pack()
             */

            var lfoBase = chunk.Pointer;

            var lfoDir = chunk.PeekInt32(4);

            //In NB02 there's no OMFDirHeader. It's just a 16-bit count of directories
            var cDir = chunk.PeekUInt16(lfoDir);

            //Instead of OMFDirEntry, we have DirEntry items. They are basically the same as OMFDirEntry except the size is 16-bit
            var entries = new dnt[cDir];

            //This is the table in section 7.3 of https://web.archive.org/web/20160909082838/http://pierrelib.pagesperso-orange.fr/exec_formats/MS_Symbol_Type_v1.0.pdf
            var offset = lfoDir + sizeof(ushort); //Skip over cDir

            //NB02 data is a bit tricky in that things can sometimes be 16-bit, other times 32-bit. I have seen indications that 32-bit
            //applies when we have an LE File.

            for (var i = 0; i < cDir; i++)
            {
                //cvdump.cpp collects info about sections and then calls various dump methods for each type.
                //e.g. DumpPub retrieves the individual fields of a public

                entries[i] = new dnt(chunk.Slice(offset), chunk);

                offset += dnt.StructSize;
            }

            var data = new NB02Data(
                chunk.AbsoluteOffset,
                sig,
                lfoBaseOff,
                lfoDir,
                cDir,
                entries
            );

            return data;
        }

        internal static RawValue<pbi[]> ReadNB02Publics(in MemoryChunk valueChunk, int size)
        {
            using var publics = new PooledList<pbi>();

            var read = 0;

            while (read < size)
            {
                var item = new pbi(valueChunk.Slice(read));
                publics.Add(item);
                read += item.StructSize;
            }

            return new RawValue<pbi[]>(valueChunk.AbsoluteOffset, publics.ToArray());
        }

        internal static RawValue<OldSymType[]> ReadNB02Symbols(in MemoryChunk valueChunk, int size)
        {
            var read = 0;

            using var list = new PooledList<OldSymType>();

            while (read < size)
            {
                var ptr = new OldSymType(valueChunk.Pointer + read);

                list.Add(ptr);

                read += ptr.reclen + 1;
            }

            return new RawValue<OldSymType[]>(valueChunk.AbsoluteOffset, list.ToArray());
        }

        internal static RawValue<OldTypType[]> ReadNB02Types(in MemoryChunk valueChunk, int size)
        {
            /* The Type format is described in section 1.4 of https://www.pcjs.org/documents/books/mspl13/c/ctoolkit/
             *
             * The maximum length of a type (including the 3 header bytes) is 65535 (MAXTYPE). The maximum size
             * of the data that follows the 3 header bytes is MAXTYPE - 3
             *
             * There are 511 primitive types, so the index of the first type starts at 512
             *
             * Based on the type of the leaf, different bytes may follow
             *
             * Note that while section 3.7 lists the type as being "loe", this is erroneous
             */

            var read = 0;

            using var list = new PooledList<OldTypType>();

            while (read < size)
            {
                var ptr = new OldTypType(valueChunk.Pointer + read);

                list.Add(ptr);

                read += ptr.len + 3;
            }

            return new RawValue<OldTypType[]>(valueChunk.AbsoluteOffset, list.ToArray());
        }

        internal static RawValue<loe[]> ReadNB02SourceLines(in MemoryChunk valueChunk, int size, bool hasSeg)
        {
            //Based on cvdump.cpp!DumpSrcLn, there can be multiple entries

            //The spec lists a type "loe" for both SSTSRCLINES and SSTSRCLNSEG. You can't have two types
            //with the same name, so we'll instead use a single type "loe" and if there's no segment, Seg will return null

            var read = 0;

            using var results = new PooledList<loe>();

            while (read < size)
            {
                var loe = new loe(valueChunk, hasSeg);

                read += loe.StructSize;

                results.Add(loe);
            }

            return new RawValue<loe[]>(valueChunk.AbsoluteOffset, results.ToArray());
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

                default:
                    throw new NotImplementedException($"Don't know how to parse NB05 symbols for a CodeViewSig of type '{sig}'");
            }

            //The subsection directory entries are immediately after the header. This is the table in section 7.3.
            //It is _not_ the table in section 7.2
            var offset = lfoDir + OMFDirHeader.StructSize;

            CV_SIGNATURE lastSignature = default;

            var data = new NB05Data(
                chunk,
                sig,
                lfoBaseOff,
                lfoDir,
                dirHeader,
                entries
            );

            //We need to set this prior to writing the entries, as OMFGlobalTypes needs to know what CodeView version we are
            symbolAccessor.data = data;

            for (var i = 0; i < dirHeader.cDir; i++)
            {
                entries[i] = new OMFDirEntry(chunk.Slice(offset), chunk, symbolAccessor, ref lastSignature);

                offset += OMFDirEntry.StructSize;
            }

            Debug.Assert(lastSignature != default);
            symbolAccessor.CvSignature = lastSignature;

            //C13 uses UTF8; C7 and C11 use length prefixed. Not sure about C6
            symbolAccessor.HasLengthPrefixedStrings = lastSignature != CV_SIGNATURE.C13;

            return data;
        }

        #endregion
    }
}
