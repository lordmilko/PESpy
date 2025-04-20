using System;
using System.IO;
using ClrDebug;
using PESpy.PDB;
using PESpy.View;

namespace PESpy.OBJ
{
    class OBJFile : IViewable, IDisposable
    {
        public static OBJFile FromFile(string path)
        {
            using var fs = File.OpenRead(path);

            return new OBJFile(fs);
        }

        private MemoryMappedFileHolder mmf;
        private GlobalMemoryBlock globalBlock;

        /* When a program is compiled with /GL for link time code generation,
         * obj file begins with ANON_OBJECT_HEADER instead of IMAGE_FILE_HEADER.
         * In this case, the file contains CxxIL records. FxCop's phx.dll in Visual Studio 2022
         * is a managed assembly that is full of information on how to parse this data */

        public ImageFileHeader FileHeader { get; private set; }

        public AnonObjectHeader? AnonObjectHeader { get; private set; }

        public ImageSectionHeader[] SectionHeaders { get; private set; }

        private IValue[]? sectionData;

        public unsafe IValue[] SectionData
        {
            get
            {
                if (sectionData == null)
                {
                    var sections = SectionHeaders;

                    var results = new IValue[sections.Length];

                    for (var i = 0; i < results.Length; i++)
                    {
                        ref var section = ref sections[i];

                        MemoryChunk sectionChunk;

                        //When we're dealing with LTCG object files, it seems that the pointer to raw data is relative to the start of the image file header. The anon header doesn't count!
                        //I spent several hours figuring this out the hard way, however it's actually hinted by microsoft-pdb in cvdump.cpp!DumpObjFileSections, with it taking
                        //an initial "offSection" parameter
                        if (AnonObjectHeader != null)
                            sectionChunk = new MemoryChunk(globalBlock, section.PointerToRawData + FileHeader.Offset);
                        else
                            sectionChunk = new MemoryChunk(globalBlock, section.PointerToRawData);

                        //Can't switch as section name is a Utf8String and we don't want to allocate
                        if (section.Name == ".drectve")
                        {
                            //I don't know if it's ANSI or UTF-8, however treating it as UTF-8 seems like the safest thing to do
                            var str = sectionChunk.PeekUtf8FixedLength(0, section.SizeOfRawData);
                            results[i] = new RawValue<FixedUtf8String>(sectionChunk.AbsoluteOffset, str);
                        }
                        else if (section.Name == ".debug$S")
                            results[i] = ReadSymbolsSection(sectionChunk, section.SizeOfRawData);
                        else if (section.Name == ".debug$T" || section.Name == ".debug$P")
                            results[i] = ReadTypesSection(sectionChunk, section.SizeOfRawData);
                        else if (section.Name == ".text$mn")
                        {
                            //It's assembly code, but we can't read it ourselves
                            results[i] = new RawValue<byte[]>(sectionChunk.AbsoluteOffset, sectionChunk.PeekSpan<byte>(0, section.SizeOfRawData).ToArray());
                        }
                        else if (section.Name == ".cil$db")
                        {
                            //debugDataFileReader -> phx!DebugDataReader
                            AssertNotImplemented();
                        }
                        else if (section.Name == ".cil$ex")
                        {
                            //expressionFileReader -> phx!ExpressionReader
                            AssertNotImplemented();
                        }
                        else if (section.Name == ".cil$fg")
                        {
                            //phx.dll flagsFileReader is not used, so I don't know what the format of this is. PEAnatomist doesn't seem to know either
                            AssertNotImplemented();
                        }
                        else if (section.Name == ".cil$gl")
                        {
                            //globalSymbolFileReader -> phx!GlobalSymbolReader.ReadHeaders

                            var symbolType = (SSR) sectionChunk.PeekByte(0);
                        else if (section.Name == ".cil$in")
                        {
                            //initializeFileReader -> phx!InitializerReader
                            AssertNotImplemented();
                        }
                        else if (section.Name == ".cil$md")
                        {
                            //metadataFileReader -> phx!MetadataReader
                            AssertNotImplemented();
                        }
                        else if (section.Name == ".cil$sy")
                        {
                            //localSymbolFileReader -> phx!LocalSymbolReader
                            AssertNotImplemented();
                        }
                        else
                        {
                            //Lookout for the .cil$ item that starts with "p" and add it above .cil$sy above

                            AssertNotImplemented();
                        }
                    }

                    sectionData = results;
                }

                return sectionData;
            }
        }

        private unsafe OBJFile(FileStream stream)
        {
            mmf = new MemoryMappedFileHolder(stream);

            globalBlock = new GlobalMemoryBlock(mmf.Address, (int) mmf.Length);

            //Read the OBJ Headers
            ReadObjHeaders();
        }

        private void ReadObjHeaders()
        {
            var chunk = new MemoryChunk(globalBlock, 0);

            var anonHeader = new AnonObjectHeader(chunk);

            int anonStructSize = 0;

            if (anonHeader.Version >= 2)
            else
                anonStructSize = AnonObjectHeader.StructSize;

            /* I tried creating a test project that uses phx.dll to see how it reads the data. CxxILObjectFileReaderInterface.ProcessFile()
             * ProcessFile() enumerates all the sections it calls a C++/CLI function ProcessSection(). This function does strcmp()
             * which checks whether the section name starts with ".cil$" (it doesn't show it in the C# but I checked in the debugger).
             * ProcessSection() does some stuff involving strings. The first cil section .cil$fg can have a string that spans into
             * .cil$gl (which is the second section) so I believe ProcessSection() is trying to find the point at which the "real"
             * content.
             *
             * It then switches on the next letter in the string. Based on known .cil$ section names (most of which are listed in (c2!LtcgCilToObj)
             * I assume the first letter maps to the given full name, and then these map to the full description given by the name of the reader
             * used in ProcessSection()
             *
             * - 100 (d -> db) -> debugDataFileReader
             * - 101 (e -> ex) -> expressionFileReader
             * - 102 (f -> fg) -> flagsFileReader
             * - 103 (g -> gl) -> globalSymbolFileReader
             * - 105 (i -> in) -> initializerFileReader
             * - 109 (m -> md) -> metadataFileReader
             * - 112 (p -> ?)  -> the section provides the precompiled header offset and size. There is no reader
             * - 115 (s -> sy) -> localSymbolFileReader
             */
            if (anonHeader.Sig1 == IMAGE_FILE_MACHINE.UNKNOWN && anonHeader.Sig2 == -1)
            {
                AnonObjectHeader = anonHeader;
                chunk = chunk.Slice(anonStructSize);
                FileHeader = new ImageFileHeader(chunk); //Machine type should be unknown kind 0xC13
            }
            else
            {
                FileHeader = new ImageFileHeader(chunk);
            }

            var sectionHeaders = new ImageSectionHeader[FileHeader.NumberOfSections];

            for (var i = 0; i < FileHeader.NumberOfSections; i++)
                sectionHeaders[i] = new ImageSectionHeader(chunk.Slice(ImageFileHeader.StructSize + (i * ImageSectionHeader.StructSize)));

            SectionHeaders = sectionHeaders;
        }

        private IValue ReadSymbolsSection(in MemoryChunk chunk, int length)
        {
            throw new NotImplementedException();
        }

        private IValue ReadTypesSection(in MemoryChunk chunk, int length)
        {
            throw new NotImplementedException();
        }
        public unsafe OBJFileView GetView()
        {
            var writer = new OBJViewWriter(this, new StreamFileReader(new MMFStream(mmf.Address, (int) mmf.Length), new object())); //todo: temp using reader+stream while we're still in transition
            ((IViewable) this).WriteView(writer);

            return (OBJFileView) writer.Finalize();
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            writer.WriteGlobal(AnonObjectHeader);
            writer.WriteGlobal(FileHeader);
            writer.WriteGlobal(SectionHeaders);
            
            foreach (var item in SectionData)
            {
                if (item == null)
                    continue;

                if (item is IViewable v)
                    writer.WriteGlobal(v);
                else if (item is RawValue<FixedUtf8String> s)
                    writer.WriteGlobal(s.Offset, s.Value, s.Value.Length + 1, ViewKind.Value);
                else
            }
        }

        public void Dispose()
        {
        }
    }
}
