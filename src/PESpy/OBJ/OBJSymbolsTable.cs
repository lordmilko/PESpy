using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.LIB;
using PESpy.PDB;
using PESpy.View;

namespace PESpy.OBJ
{
    //Name is made up
    [DebuggerDisplay("{DebuggerDisplay(),nq}")]
    public class OBJSymbolsTable : IValue, IViewable, ICodeViewModuleAccessor
    {
        private string DebuggerDisplay()
        {
            switch (Signature)
            {
                case CV_SIGNATURE.C7:
                case CV_SIGNATURE.C11:
                case CV_SIGNATURE.C13:
                    return $"{Signature} Symbols";

                default:
                    //Garbage; must be C6
                    return "C6 Symbols";
            }
        }

        private const int SignatureOffset = 0;

        public CV_SIGNATURE Signature => (CV_SIGNATURE) chunk.PeekUInt32(SignatureOffset);

        private unsafe SymTypeList? c6Symbols;

        //Even in modern OBJ files you can have C6 symbols. There can be two .debug$S sections, and only
        //one of which starts with a valid Signature
        public unsafe SymTypeList? C6Symbols
        {
            get
            {
                if (c6Symbols == null)
                {
                    switch (Signature)
                    {
                        case CV_SIGNATURE.C7:
                        case CV_SIGNATURE.C11:
                        case CV_SIGNATURE.C13:
                            return null;
                    }

                    //Garbage; must be C6

                    var block = chunk.block;

                    //C7 and C11 use ST strings
                    ICodeViewAccessor codeViewAccessor = null;

                    if (block is GlobalMemoryBlock b)
                    {
                        codeViewAccessor = new OBJFileCodeViewAccessor((OBJFile) b.File, true);
                    }
                    else
                    {
                        var s = (GlobalSubMemoryBlock) block;
                        codeViewAccessor = new LongImportLibraryMemberSymbolAccessor((LongImportLibraryMember) s.Owner, true);
                    }

                    SymbolMemoryTracker.RegisterCVSymbolMemory(chunk, codeViewAccessor, this);
                    c6Symbols = new SymTypeList(chunk.Pointer, 0, Length, codeViewAccessor);
                }

                return c6Symbols;
            }
        }

        public SymTypeList? c7Symbols;

        //C7 or C11
        public unsafe SymTypeList? C7Symbols
        {
            get
            {
                var sig = Signature;

                if (c7Symbols == null && sig is CV_SIGNATURE.C7 or CV_SIGNATURE.C11)
                {
                    var block = chunk.block;

                    //C7 and C11 use ST strings
                    ICodeViewAccessor codeViewAccessor = null;

                    if (block is GlobalMemoryBlock b)
                    {
                        codeViewAccessor = new OBJFileCodeViewAccessor((OBJFile) b.File, true);
                    }
                    else
                    {
                        var s = (GlobalSubMemoryBlock) block;
                        codeViewAccessor = new LongImportLibraryMemberSymbolAccessor((LongImportLibraryMember) s.Owner, true);
                    }

                    SymbolMemoryTracker.RegisterCVSymbolMemory(chunk, codeViewAccessor, this);
                    c7Symbols = new SymTypeList(chunk.Pointer, sizeof(int), Length, codeViewAccessor);
                }

                return c7Symbols;
            }
        }

        private CvDebugSSubsectionHeader[]? c13SubSections;

        public CvDebugSSubsectionHeader[]? C13SubSections
        {
            get
            {
                if (c13SubSections == null && Signature == CV_SIGNATURE.C13)
                {
                    var totalOffset = 4;

                    using var results = new PooledList<CvDebugSSubsectionHeader>();

                    while (totalOffset < Length)
                    {
                        //The start of each record must be 32-bit aligned relative to the start
                        //of the section. i.e. if the section starts at 1, address 5 is aligned
                        if ((totalOffset & 3) != 0)
                        {
                            var diff = 4 - (totalOffset & 3);
                            totalOffset += diff;
                        }

                        //C13 uses UTF8 strings
                        var header = new CvDebugSSubsectionHeader(chunk.Slice(totalOffset));

                        results.Add(header);

                        totalOffset += header.Length + 8; //sizeof(type) = sizeof(cbLen)
                    }

                    c13SubSections = results.ToArray();
                }

                return c13SubSections;
            }
        }

        SymTypeList ICodeViewModuleAccessor.Symbols
        {
            get
            {
                switch (Signature)
                {
                    case CV_SIGNATURE.C7:
                    case CV_SIGNATURE.C11:
                        return C7Symbols;

                    case CV_SIGNATURE.C13:
                        throw new InvalidOperationException("This object should not have been used to register symbol memory if it has C13 symbols");

                    default:
                        return C6Symbols;
                }
            }
        }

        bool ICodeViewModuleAccessor.TryGetFunctionSymbol(int off, ISECT seg, out SymType symType)
        {
            throw new NotImplementedException();
        }

        public int Offset => chunk.AbsoluteOffset;

        private readonly MemoryChunk chunk;

        /// <summary>
        /// Gets the number of bytes contained in the table.
        /// </summary>
        public int Length { get; }

        internal OBJSymbolsTable(in MemoryChunk chunk, int length)
        {
            this.chunk = chunk;
            Length = length;

            _ = C7Symbols;
#if STRESS_TEST
            _ = C13SubSections;
#endif
        }

        public unsafe void CopyTo(Span<byte> destination)
        {
            new Span<byte>(chunk.Pointer, Length).CopyTo(destination);
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            switch (Signature)
            {
                case CV_SIGNATURE.C7:
                case CV_SIGNATURE.C11:
                    writer.WriteGlobal(Offset, Signature, sizeof(int), ViewKind.CvSignature);
                    writer.WriteGlobal(Offset + 4, C7Symbols);
                    break;

                case CV_SIGNATURE.C13:
                    writer.WriteGlobal(Offset, Signature, sizeof(int), ViewKind.CvSignature);
                    writer.WriteGlobal(C13SubSections);
                    break;

                default:
                    //Garbage; must be C6
                    writer.WriteGlobal(Offset, C6Symbols);
                    break;
            }
        }

        IView? IViewable.WriteStruct(ViewWriter writer) => null;

        int IViewable.NumChildren() => throw new NotSupportedException();

        void IViewable.WriteChild(int index, ref StructWriter structWriter) => throw new NotSupportedException();
    }
}
