using System;
using System.Buffers;
using System.Collections.Generic;

namespace PESpy.PDB
{
    public static partial class MsfStream
    {
        //Provides access to the /src/headerblock stream
        public class SrcHeaders
        {
            private static readonly Guid CorSym_DocumentType_Text = new Guid("5a869d0b-6611-11d3-bd2a-0000f80849bd");
            private static readonly Guid CorSym_DocumentType_MC = new Guid("eb40cb65-3c1f-4352-9d7b-ba0fc47a9d77");

            public SrcHeaderBlock SrcHeaderBlock => new SrcHeaderBlock(chunk);

            //typedef Map<NI, SHO, HcNi> MapNiSrcHdr;
            public Map<NI, SrcHeaderOut, HcNi> FileHeaderMap { get; }

            //Null if there's no /names stream
            public PDBSourceFile[]? SourceFiles { get; }

            private readonly MemoryChunk chunk;
            private readonly NMT? nameMap;

            internal SrcHeaders(in MemoryChunk chunk, PDBFile pdbFile)
            {
                this.chunk = chunk;

                FileHeaderMap = new Map<NI, SrcHeaderOut, HcNi>(
                    chunk.Slice(SrcHeaderBlock.StructSize),
                    c => new SrcHeaderOut(c),
                    SrcHeaderOut.StructSize,
                    HcNi.Instance
                );

                var nameMap = pdbFile.NameMap;
                this.nameMap = nameMap; //The name map is inextricably linked with the /src/headerblock, so I feel like we should cache it for use in lookups

                if (nameMap != null)
                {
                    var sourceFiles = new List<PDBSourceFile>();

                    foreach (var entry in FileHeaderMap.Entries)
                    {
                        //The key of each entry is an NI which is the key of an entry in the name map.
                        //i.e. /names, NOT NMTNI - NMTNI, despite its name, has nothing to do with NI's,
                        //it maps stream names to _SI's_

                        var str = nameMap.GetStringFromNI(entry.Key);

                        var streamName = "/src/files/" + str;

                        if (pdbFile.TryGetStreamChunk(streamName, out var fileChunk))
                        {
                            /* LLVM seem to think that if the srccompress is 101, this means .NET. This is not an unreasonable assumption:
                             * - The GUIDs that you read match the format found in PDBs
                             * - DiaSymReader only parses embedded source files when the type is 101
                             * - DiaSymReader sets the compression type to 101 in SymDocumentWriter::WriteCurrentData
                             *
                             * However, it's also true that microsoft-pdb explicitly defines the "SrcFormat" structure, and yet did not define an enum
                             * value in SrcCompress for .NET specifically. There is no enum value listed for IDiaInjectedSource::get_sourceCompression,
                             * but I suspect that it's likely the SrcCompress enum type too */

                            var header = entry.Value;

                            if (header.cbSource < SrcFormat.FixedStructSize)
                            {
                                //Definitely raw text
                                var val = fileChunk.PeekAnsiFixedLength(0, header.cbSource);

                                sourceFiles.Add(new PDBSourceFile(str, new RawValue<FixedAnsiString>(fileChunk.AbsoluteOffset, val)));
                            }
                            else
                            {
                                //pdbdump matches on the algorithmId, however this could be null if there's no checksum. I think matching on documentType
                                //is best (see the notes in ClrDebug.SrcFormat for more info)
                                var documentType = fileChunk.PeekGuid(32);

                                if (documentType == CorSym_DocumentType_Text || documentType == CorSym_DocumentType_MC)
                                {
                                    var val = new SrcFormat(fileChunk);

                                    sourceFiles.Add(new PDBSourceFile(str, val));
                                }
                                else
                                {
                                    //Treat as text
                                    var val = fileChunk.PeekAnsiFixedLength(0, header.cbSource);

                                    sourceFiles.Add(new PDBSourceFile(str, new RawValue<FixedAnsiString>(fileChunk.AbsoluteOffset, val)));
                                }
                            }
                        }
                    }

                    SourceFiles = sourceFiles.ToArray();
                }
            }

            public unsafe bool TryGetFileHeaders(FixedUtf8String fileName, out SrcHeaderOut srcHeaderOut, out int physicalEntryIndex)
            {
                srcHeaderOut = default;
                physicalEntryIndex = default;

                if (nameMap == null)
                    return false;

                /* In order to lookup a file name in the header block you need to convert it to lowercase and change forward slashes to back slashes.
                 * SrcImpl::QueryByNameW creates a copy of the input file, and then calls CCanonFile::SzCanonFilename on it, which calls LCMapStringW on
                 * it with LOCALE_INVARIANT and LCMAP_LOWERCASE and then replaces all \ characters with /
                 * 
                 * There is a MemoryExtensions method that converts a ReadOnlySpan<byte> ToLower, but our hasher requires that we pass in a UTF-8 string,
                 * so we would need to convert from UTF-8 -> UTF-16 -> UTF-8 if we were to use this, which is no good. Furthermore, this API just seems to call 
                 * char.ToLower() individually anyway; it doesn't blast all the characters to LCMapStringW. So we may as well just normalize the string ourselves 
                 */

                var buffer = ArrayPool<byte>.Shared.Rent(fileName.Length);

                NI ni;

                try
                {
                    fileName.CopyTo(buffer);

                    for (var i = 0; i < fileName.Length; i++)
                    {
                        if (buffer[i] == '/')
                            buffer[i] = (byte) '\\';
                        else
                            buffer[i] = (byte) char.ToLower((char) buffer[i]);
                    }

                    fixed (byte* b = buffer)
                        ni = nameMap.Hash(new FixedUtf8String(b, fileName.Length));
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }

                if (FileHeaderMap.TryFind(ni, out srcHeaderOut, out physicalEntryIndex))
                    return true;

                return false;
            }

            public bool TryGetFileHeaders(string fileName, out SrcHeaderOut srcHeaderOut, out int physicalEntryIndex)
            {
                srcHeaderOut = default;
                physicalEntryIndex = default;

                if (nameMap == null)
                    return false;

                /* SrcImpl::QueryByNameW creates a copy
                 * of the input file, and then calls CCanonFile::SzCanonFilename on it, which calls LCMapStringW on it with LOCALE_INVARIANT and LCMAP_LOWERCASE
                 * and then replaces all \ characters with / */

                NI ni;
            }

            public SrcHeaderOut GetFileHeaders(string fileName)
            {
                if (!TryGetFileHeaders(fileName, out var srcHeaderOut, out _))
                    throw new NotImplementedException();

                return srcHeaderOut;
            }
        }
    }
}
