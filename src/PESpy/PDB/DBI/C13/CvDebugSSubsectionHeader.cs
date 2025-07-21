using System;
using System.Collections.Generic;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    //CV_DebugSSubsectionHeader_t
    public class CvDebugSSubsectionHeader : IValue, IViewable //May not be present
    {
        //type
        public DEBUG_S_SUBSECTION_TYPE Type => (DEBUG_S_SUBSECTION_TYPE) chunk.PeekUInt32(0);
        
        //cbLen
        public CV_off32_t Length => chunk.PeekInt32(4);

        private object? data;

        public unsafe object Data
        {
            get
            {
                if (data == null)
                {
                    var dataChunk = chunk.Slice(8);

                    switch (Type)
                    {
                        case DEBUG_S_SUBSECTION_TYPE.DEBUG_S_SYMBOLS:

                            //CV_DebugSSubsectionHeader_t is implicitly C13 data, but we still have to register ourselves in any case

                            if (dataChunk.block is PagedMemoryBlock)
                                SymbolMemoryTracker.RegisterPDBSymbolMemory(dataChunk);
                            else
                            {
                                //OBJ or LIB. We're C13, which means UTF8
                                SymbolMemoryTracker.RegisterCVSymbolMemory(CV_SIGNATURE.C13, dataChunk); //We're being called from OBJSymbolsTable.C13SubSections which only runs when the signature is C13
                            }

                            data = new SymTypeList(dataChunk.Pointer, Length);
                            break;

                        case DEBUG_S_SUBSECTION_TYPE.DEBUG_S_LINES:
                            data = new CvDebugSLinesHeader(dataChunk, Length);
                            break;

                        case DEBUG_S_SUBSECTION_TYPE.DEBUG_S_STRINGTABLE:
                            data = ParseStringTable(dataChunk);
                            break;

                        case DEBUG_S_SUBSECTION_TYPE.DEBUG_S_FILECHKSMS:
                            data = new CvFileCheckSum(dataChunk);
                            break;

                        case DEBUG_S_SUBSECTION_TYPE.DEBUG_S_FRAMEDATA:
                            data = new RvaAndFrameData(dataChunk.Slice(8), Length);
                            break;

                        case DEBUG_S_SUBSECTION_TYPE.DEBUG_S_INLINEELINES:
                            data = ParseInlineeLines(dataChunk);
                            break;

                        case DEBUG_S_SUBSECTION_TYPE.DEBUG_S_CROSSSCOPEIMPORTS: //CrossScopeReferences (see DumpModCrossScopeRefs)
                            throw new NotImplementedException();

                        case DEBUG_S_SUBSECTION_TYPE.DEBUG_S_CROSSSCOPEEXPORTS: //LocalIdAndGlobalIdPair
                            throw new NotImplementedException();

                        case DEBUG_S_SUBSECTION_TYPE.DEBUG_S_IL_LINES: //? (DumpObjFileSections calls DumpModILLines)
                            throw new NotImplementedException();

                        case DEBUG_S_SUBSECTION_TYPE.DEBUG_S_FUNC_MDTOKEN_MAP: //? (DumpObjFileSections calls DumpModFuncTokenMap)
                            throw new NotImplementedException();

                        case DEBUG_S_SUBSECTION_TYPE.DEBUG_S_TYPE_MDTOKEN_MAP: //? (DumpObjFileSections calls DumpModTypeTokenMap)
                            throw new NotImplementedException();

                        case DEBUG_S_SUBSECTION_TYPE.DEBUG_S_MERGED_ASSEMBLYINPUT: //? (see DumpModMergedAssemblyInput)
                            throw new NotImplementedException();

                        case DEBUG_S_SUBSECTION_TYPE.DEBUG_S_COFF_SYMBOL_RVA:
                            throw new NotImplementedException();

                        default:
                            throw new NotImplementedException();
                    }
                }

                return data;
            }
        }

        public int Offset => chunk.AbsoluteOffset;

        internal int StructSize =>
            sizeof(int) + //Type
            Length;

        private readonly MemoryChunk chunk;

        internal CvDebugSSubsectionHeader(in MemoryChunk chunk)
        {
            this.chunk = chunk;

            if ((Type & DEBUG_S_SUBSECTION_TYPE.DEBUG_S_IGNORE) != 0)
                throw new System.NotImplementedException(); //you're meant to ignore the contents when this bit is set, but is the data actually valid?

#if STRESS_TEST
            _ = Data;
#endif
        }

        private object ParseInlineeLines(in MemoryChunk dataChunk)
        {
            throw new NotImplementedException();
        }

        private object ParseStringTable(in MemoryChunk dataChunk)
        {
            var read = 0;

            var end = Length;

            using var results = new PooledList<RawValue<Utf8String>>();

            while (read < end)
            {
                var str = dataChunk.PeekUtf8NullTerminatedString(read);
                results.Add(new RawValue<Utf8String>(dataChunk.AbsoluteOffset + read, str));
                read += str.Length + 1;
            }

            return results.ToArray();
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct("CV_DebugSSubsectionHeader_t", this, ViewKind.CvDebugSSubsectionHeader, StructSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField("type", Type, sizeof(int));
            s.WriteField("cbLen", Length);

            switch (Type)
            {
                case DEBUG_S_SUBSECTION_TYPE.DEBUG_S_SYMBOLS:
                    if (chunk.block is PagedMemoryBlock p)
                        s.WritePagedValue(chunk.RelativeOffset + 8, p, (SymTypeList) Data);
                    else
                        s.WriteValue(Offset + 8, (SymTypeList) Data);
                    break;

                case DEBUG_S_SUBSECTION_TYPE.DEBUG_S_LINES:
                    s.WriteInline((CvDebugSLinesHeader) Data);
                    break;

                case DEBUG_S_SUBSECTION_TYPE.DEBUG_S_STRINGTABLE:
                    s.WriteInlineUtf8NullTerminated((RawValue<Utf8String>[]) Data);
                    break;

                case DEBUG_S_SUBSECTION_TYPE.DEBUG_S_FILECHKSMS:
                    s.WriteInline((CvFileCheckSum) Data);
                    break;

                case DEBUG_S_SUBSECTION_TYPE.DEBUG_S_FRAMEDATA:
                    s.WriteInline((RvaAndFrameData) Data);
                    break;

                case DEBUG_S_SUBSECTION_TYPE.DEBUG_S_INLINEELINES:
                    throw new NotImplementedException();

                case DEBUG_S_SUBSECTION_TYPE.DEBUG_S_CROSSSCOPEIMPORTS:
                    throw new NotImplementedException();

                case DEBUG_S_SUBSECTION_TYPE.DEBUG_S_CROSSSCOPEEXPORTS:
                    throw new NotImplementedException();

                case DEBUG_S_SUBSECTION_TYPE.DEBUG_S_IL_LINES:
                    throw new NotImplementedException();

                case DEBUG_S_SUBSECTION_TYPE.DEBUG_S_FUNC_MDTOKEN_MAP:
                    throw new NotImplementedException();

                case DEBUG_S_SUBSECTION_TYPE.DEBUG_S_TYPE_MDTOKEN_MAP:
                    throw new NotImplementedException();

                case DEBUG_S_SUBSECTION_TYPE.DEBUG_S_MERGED_ASSEMBLYINPUT:
                    throw new NotImplementedException();

                case DEBUG_S_SUBSECTION_TYPE.DEBUG_S_COFF_SYMBOL_RVA:
                    throw new NotImplementedException();

                default:
                    throw new NotImplementedException();
            }

            return s.ToArray();
        }

        public override string ToString()
        {
            return Type.ToString();
        }
    }
}
