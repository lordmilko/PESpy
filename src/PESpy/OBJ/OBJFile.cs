using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using PESpy.OBJ;
using PESpy.View;
using PESpy.View.Builder;
using static ClrDebug.IMAGE_FILE_MACHINE;

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

                        if (section.PointerToRawData == 0)
                            continue;

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
            if (sizeOfRawData == 0)
                return null;

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
            else if (sectionName == ".debug$F")
            {
                //FPO
                Debug.Assert((sizeOfRawData % FpoData.StructSize) == 0);

                var results = new FpoData[sizeOfRawData / FpoData.StructSize];

                for (var i = 0; i < results.Length; i++)
                    results[i] = new FpoData(sectionChunk.Slice(i * FpoData.StructSize));

                return new RawValue<FpoData[]>(sectionChunk.AbsoluteOffset, results);
            }
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
                return AssertNotImplemented(sectionChunk, sectionName, sizeOfRawData);
            }
            #region CxxIL
            else if (sectionName == ".cil$db")
            {
                //debugDataFileReader -> phx!DebugDataReader
                return AssertNotImplemented(sectionChunk, sectionName, sizeOfRawData);
            }
            else if (sectionName == ".cil$ex")
            {
                //expressionFileReader -> phx!ExpressionReader
                return AssertNotImplemented(sectionChunk, sectionName, sizeOfRawData);
            }
            else if (sectionName == ".cil$fg")
            {
                //phx.dll flagsFileReader is not used, so I don't know what the format of this is. PEAnatomist doesn't seem to know either
                return AssertNotImplemented(sectionChunk, sectionName, sizeOfRawData);
            }
            else if (sectionName == ".cil$gl")
            {
                //globalSymbolFileReader -> phx!GlobalSymbolReader.ReadHeaders

                return AssertNotImplemented(sectionChunk, sectionName, sizeOfRawData);
            }
            else if (sectionName == ".cil$in")
            {
                //initializeFileReader -> phx!InitializerReader
                return AssertNotImplemented(sectionChunk, sectionName, sizeOfRawData);
            }
            else if (sectionName == ".cil$md")
            {
                //metadataFileReader -> phx!MetadataReader
                return AssertNotImplemented(sectionChunk, sectionName, sizeOfRawData);
            }
            else if (sectionName == ".cil$sy")
            {
                //localSymbolFileReader -> phx!LocalSymbolReader
                return AssertNotImplemented(sectionChunk, sectionName, sizeOfRawData);
            }
            #endregion
            else
            {
                //Lookout for the .cil$ item that starts with "p" and add it above .cil$sy above

                return AssertNotImplemented(sectionChunk, sectionName, sizeOfRawData);
            }
        }

        private MemoryMappedFileHolder mmf;
        private readonly GlobalMemoryBlock globalBlock;
        private ISymbolAccessor symbolAccessor;

        private readonly object c13SymbolMemoryLock = new object();
        private readonly HashSet<int> c13RegisteredSymbolMemory = new HashSet<int>();

        private bool disposed;

        internal unsafe OBJFile(string fileName, in MemoryMappedFileHolder mmf)
        {
            this.mmf = mmf;

            FileName = fileName;
            Name = Path.GetFileName(fileName);

            FileHeader = default;
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
            if (anonHeader.Sig1 == IMAGE_FILE_MACHINE_UNKNOWN && anonHeader.Sig2 == -1)
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

        private static IValue? AssertNotImplemented(in MemoryChunk sectionChunk, FixedUtf8String sectionName, int sizeOfRawData)
        {
            return new RawValue<NativeSpan<byte>>(sectionChunk.AbsoluteOffset, sectionChunk.PeekNativeSpan<byte>(0, sizeOfRawData));
        }

        public FileView GetView(
            LocatorHttpPolicy httpPolicy = LocatorHttpPolicy.None,
            bool trackXRefs = false,
            CancellationToken cancellationToken = default)
        {
            var writer = new OBJViewWriter(this);
            ((IViewable) this).WriteGlobals(writer);

            return (FileView) writer.Finalize();
        }

        //There isn't really "one" symbol accessor; each section may have its own accessor with its own rules
        public ISymbolAccessor GetSymbolAccessor(
            LocatorHttpPolicy httpPolicy = LocatorHttpPolicy.All,
            ILocatorProgress? progress = null,
            CancellationToken cancellationToken = default) => symbolAccessor ??= new OBJFileSymbolAccessor(this);

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

            var sectionHeaders = SectionHeaders;

            writer.WriteGlobal(sectionHeaders);

            WriteGlobals(writer, sectionHeaders, SectionData);
        }

        internal static void WriteGlobals(
            ViewWriter writer,
            ImageSectionHeader[] sectionHeaders,
            object[] sectionData)
        {
            for (var i = 0; i < sectionHeaders.Length; i++)
            {
                var data = sectionData[i];

                if (data == null)
                    continue;

                if (data is IViewable v)
                    writer.WriteGlobal(v);
                else if (data is RawValue<FixedUtf8String> s)
                {
                    ref var header = ref sectionHeaders[i];
                    var sectionName = header.Name;

                    ViewKind kind;

                    if (sectionName == ".drectve")
                        kind = ViewKind.drectve;
                    else
                        throw new NotImplementedException();

                    writer.WriteGlobal(s.Offset, s.Value, s.Value.Length + 1, kind);
                }
                else if (data is RawValue<FpoData[]> f)
                {
                    foreach (var item in f.Value)
                        writer.WriteGlobal(item);
                }
                else if (data is RawValue<NativeSpan<byte>> b)
                {
                    ref var header = ref sectionHeaders[i];
                    var sectionName = header.Name;

                    ViewKind kind;

                    if (sectionName == ".text")
                        kind = ViewKind.text;
                    else if (sectionName == ".text$mn")
                        kind = ViewKind.text_mn;
                    else if (sectionName == ".data")
                        kind = ViewKind.data;
                    else if (sectionName.StartsWith(".idata"))
                        kind = ViewKind.idata;
                    else if (sectionName == ".edata")
                        kind = ViewKind.edata;
                    else if (sectionName == ".rdata")
                        kind = ViewKind.rdata;
                    else if (sectionName == ".debug$f") //FPO
                        kind = ViewKind.debug_f;
                    else if (sectionName == ".bss")
                        kind = ViewKind.bss; //Don't know what the actual data format is
                    else if (sectionName.StartsWith(".rsrc"))
                        kind = ViewKind.rsrc;
                    else if (sectionName == ".sxdata")
                        kind = ViewKind.sxdata;
                    else
                    {
#if DEBUG
                        if (sectionName.StartsWith("."))
                            throw new NotImplementedException();
#endif

                        //You can have weird garbage in a lib file in the name
                        kind = ViewKind.UnknownSection;
                    }

                    writer.WriteGlobal(b.Offset, b.Value, b.Value.Length, kind);
                }
                else
                    throw new NotImplementedException($"Don't know how to write a value of type {data.GetType().Name}");
            }
        }

        IView? IViewable.WriteStruct(ViewWriter writer) => null;

        int IViewable.NumChildren() => throw new NotSupportedException();

        void IViewable.WriteChild(int index, ref StructWriter structWriter) => throw new NotSupportedException();

        internal ICodeViewAccessor RegisterC13SymbolMemory(MemoryChunk dataChunk, ICodeViewModuleAccessor codeViewModuleAccessor)
        {
            lock (c13SymbolMemoryLock)
            {
                if (c13RegisteredSymbolMemory.Add(dataChunk.AbsoluteOffset))
                {
                    var codeViewAccessor = new OBJFileCodeViewAccessor(this, false);

                    //We're being called from OBJSymbolsTable.C13SubSections which only runs when the signature is C13
                    SymbolMemoryTracker.RegisterCVSymbolMemory(dataChunk, codeViewAccessor, codeViewModuleAccessor);

                    return codeViewAccessor;
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
