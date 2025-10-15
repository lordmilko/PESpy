using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using ClrDebug;
using PESpy.OBJ;
using PESpy.View;
using PESpy.View.Builder;

namespace PESpy
{
    /// <summary>
    /// Represents a file in the Common Object File Format that is not better described
    /// by a more specific type (such as <see cref="PEFile"/>).<para/>
    /// File types commonly used with this type include *.exp and non-OMF *.obj files.
    /// </summary>
    public class OBJFile : IFile, IViewable, IDisposable
    {
        public static OBJFile FromFile(string path)
        {
            using var fs = File.OpenRead(path);

            var mmf = new MemoryMappedFileHolder(fs);

            try
            {
                return new OBJFile(fs.Name, mmf);
            }
            catch
            {
                mmf.Dispose();

                throw;
            }
        }

        /// <inheritdoc/>
        public string? Name { get; private set; }

        /// <inheritdoc/>
        public string? FileName { get; private set; }

        /// <inheritdoc/>
        public FileKind Kind => FileKind.OBJ;

        public int Length => globalBlock.Length;

        /* When a program is compiled with /GL for link time code generation,
         * obj file begins with ANON_OBJECT_HEADER instead of IMAGE_FILE_HEADER.
         * In this case, the file contains CxxIL records. FxCop's phx.dll in Visual Studio 2022
         * is a managed assembly that is full of information on how to parse this data */

        public ImageFileHeader FileHeader { get; private set; }

        public AnonObjectHeader? AnonObjectHeader { get; private set; }

        public ImageSectionHeader[] SectionHeaders { get; private set; }

        private IValue?[]? sectionData;

        public IValue?[] SectionData
        {
            get
            {
                if (sectionData == null)
                {
                    var sections = SectionHeaders;

                    var results = new IValue?[sections.Length];

                    /* https://web.archive.org/web/20160909082838/http://pierrelib.pagesperso-orange.fr/exec_formats/MS_Symbol_Type_v1.0.pdf
                     *
                     * .debug$S is also known as $$SYMBOLS, while .debug$T is also known as $$TYPES
                     *
                     * This document appears to be from the CV_SIGNATURE_C7 era because it says that a 1 goes before the symbols.
                     * There may be multiple .debug$S sections. .debug$T does not say whether it permits having multiple sections */

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

                        results[i] = GetDataForSection(sectionChunk, section.Name, section.SizeOfRawData);
                    }

                    sectionData = results;
                }

                return sectionData;
            }
        }

        public IEnumerable<T> GetSectionData<T>(string name) where T : class
        {
            for (var i = 0; i < SectionHeaders.Length; i++)
            {
                if (SectionHeaders[i].Name == name)
                    yield return Unsafe.As<T>(SectionData[i]);
            }
        }

        internal static unsafe IValue? GetDataForSection(in MemoryChunk sectionChunk, FixedUtf8String sectionName, int sizeOfRawData)
        {
            //Can't switch as section name is a Utf8String and we don't want to allocate
            if (sectionName == ".drectve")
            {
                //I don't know if it's ANSI or UTF-8, however treating it as UTF-8 seems like the safest thing to do
                var str = sectionChunk.PeekUtf8FixedLength(0, sizeOfRawData);
                return new RawValue<FixedUtf8String>(sectionChunk.AbsoluteOffset, str);
            }
            else if (sectionName == ".debug$S")
                return new OBJSymbolsTable(sectionChunk, sizeOfRawData);
            else if (sectionName == ".debug$T" || sectionName == ".debug$P")
                return new OBJTypesTable(sectionChunk, sizeOfRawData);
            else if (sectionName == ".text$mn")
            {
                //It's assembly code, but we can't read it ourselves
                return new RawValue<NativeSpan<byte>>(sectionChunk.AbsoluteOffset, sectionChunk.PeekNativeSpan<byte>(0, sizeOfRawData));
            }
            else if (sectionName == ".edata")
            {
                //Found in *.exp files. I think the format consists of a meaningless ImageExportDirectory
                //header (it doesn't actually seem to point to any symbols, followed by a numberof symbols),
                //whose locations are pointed to by COFF symbols
                //whose section is .edata
                return AssertNotImplemented();
            }
            #region CxxIL
            else if (sectionName == ".cil$db")
            {
                //debugDataFileReader -> phx!DebugDataReader
                return AssertNotImplemented();
            }
            else if (sectionName == ".cil$ex")
            {
                //expressionFileReader -> phx!ExpressionReader
                return AssertNotImplemented();
            }
            else if (sectionName == ".cil$fg")
            {
                //phx.dll flagsFileReader is not used, so I don't know what the format of this is. PEAnatomist doesn't seem to know either
                return AssertNotImplemented();
            }
            else if (sectionName == ".cil$gl")
            {
                //globalSymbolFileReader -> phx!GlobalSymbolReader.ReadHeaders

                return AssertNotImplemented();
            }
            else if (sectionName == ".cil$in")
            {
                //initializeFileReader -> phx!InitializerReader
                return AssertNotImplemented();
            }
            else if (sectionName == ".cil$md")
            {
                //metadataFileReader -> phx!MetadataReader
                return AssertNotImplemented();
            }
            else if (sectionName == ".cil$sy")
            {
                //localSymbolFileReader -> phx!LocalSymbolReader
                return AssertNotImplemented();
            }
            #endregion
            else
            {
                //Lookout for the .cil$ item that starts with "p" and add it above .cil$sy above

                return AssertNotImplemented();
            }
        }

        private MemoryMappedFileHolder mmf;
        private readonly GlobalMemoryBlock globalBlock;

        private readonly object c13SymbolMemoryLock = new object();
        private readonly HashSet<int> c13RegisteredSymbolMemory = new HashSet<int>();

        private bool disposed;

        internal unsafe OBJFile(string fileName, in MemoryMappedFileHolder mmf)
        {
            this.mmf = mmf;

            FileName = fileName;
            Name = Path.GetFileName(fileName);

            FileHeader = null!;
            SectionHeaders = null!;

            globalBlock = new GlobalMemoryBlock(mmf.Address, (int) mmf.Length, this);

            try
            {
                //Read the OBJ Headers
                ReadObjHeaders();
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        ~OBJFile()
        {
            Dispose(false);
        }

        private void ReadObjHeaders()
        {
            var chunk = new MemoryChunk(globalBlock, 0);

            var anonHeader = new AnonObjectHeader(chunk);

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
                int anonStructSize = 0;

                if (anonHeader.Version >= 2)
                {
                    throw new NotImplementedException("Parsing Anon Header V2 is not implemented");
                }
                else
                    anonStructSize = AnonObjectHeader.StructSize;

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

        private static IValue? AssertNotImplemented()
        {
            //todo
            return null;
        }

        public unsafe FileView GetView()
        {
            var writer = new OBJViewWriter(this);
            ((IViewable) this).WriteGlobals(writer);

            return (FileView) writer.Finalize();
        }

        internal unsafe ByteViewProvider CreateByteViewProvider() => new LocalByteViewProvider(mmf.Address, (int) mmf.Length);

        [EditorBrowsable(EditorBrowsableState.Never)]
        public unsafe void GetRawPointer(out byte* pointer, out int length)
        {
            pointer = mmf.Address;
            length = (int) mmf.Length;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
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
                else if (item is RawValue<NativeSpan<byte>> b)
                    continue;
                else
                    throw new NotImplementedException($"Don't know how to write a value of type {item.GetType().Name}");
            }
        }

        IView? IViewable.WriteStruct(ViewWriter writer) => null;

        int IViewable.NumChildren => throw new NotSupportedException();

        void IViewable.WriteChild(int index, ref StructWriter structWriter) => throw new NotSupportedException();

        internal ISymbolAccessor RegisterC13SymbolMemory(MemoryChunk dataChunk)
        {
            lock (c13SymbolMemoryLock)
            {
                if (c13RegisteredSymbolMemory.Add(dataChunk.AbsoluteOffset))
                {
                    var symbolAccessor = new OBJSymbolAccessor(this, false);

                    //We're being called from OBJSymbolsTable.C13SubSections which only runs when the signature is C13
                    SymbolMemoryTracker.RegisterCVSymbolMemory(dataChunk, symbolAccessor);

                    return symbolAccessor;
                }

                return null;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
                GC.SuppressFinalize(this);

            globalBlock.Dispose();
            mmf.Dispose();

            disposed = true;
        }

        public override string ToString()
        {
            if (Name != null)
                return Name.ToString();

            return base.ToString();
        }
    }
}
