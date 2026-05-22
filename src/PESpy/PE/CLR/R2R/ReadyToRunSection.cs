using System;
using ClrDebug;
using PESpy.View;

//This is in a namespace to distinguish this type from the NativeAOT types which share similar names
namespace PESpy.R2R
{
    public struct ReadyToRunSection : IViewableValue
    {
        private const int TypeOffset = 0;
        private const int SectionOffset = 4;
        private const int DataOffset = 0;

        public ReadyToRunSectionType Type => (ReadyToRunSectionType) chunk.PeekUInt32(TypeOffset);

        public ImageDataDirectory Section => new ImageDataDirectory(chunk.Slice(SectionOffset));

        private object? data;

        public object? Data
        {
            get
            {
                //todo: add a github issue for all of the missing types here, and then dont throw on unknown, just assert

                if (data == null)
                {
                    if (chunk.PEFile().TryGetDirectoryChunk(Section, out var valueChunk))
                    {
                        switch (Type)
                        {
                            case ReadyToRunSectionType.CompilerIdentifier:
                                //It says it's 0 terminated, but it's not always
                                //https://github.com/dotnet/runtime/blob/a38ab4c0bc3780754259be600db1501cc2907a84/docs/design/coreclr/botr/readytorun-format.md#readytorunsectiontypecompileridentifier
                                data = new RawValue<FixedAnsiString>(valueChunk.AbsoluteOffset, valueChunk.PeekAnsiFixedLength(DataOffset, Section.Size));
                                break;

                            case ReadyToRunSectionType.ImportSections:
                                {
                                    var entries = new ReadyToRunImportSection[Section.Size / ReadyToRunImportSection.StructSize];

                                    for (var i = 0; i < entries.Length; i++)
                                        entries[i] = new ReadyToRunImportSection(valueChunk.Slice(i * ReadyToRunImportSection.StructSize));

                                    data = entries;
                                    break;
                                }

                            case ReadyToRunSectionType.RuntimeFunctions:
                                break; //todo

                            case ReadyToRunSectionType.MethodDefEntryPoints:
                                break; //todo

                            case ReadyToRunSectionType.ExceptionInfo:
                                goto default;

                            case ReadyToRunSectionType.DebugInfo:
                                break; //todo

                            case ReadyToRunSectionType.DelayLoadMethodCallThunks:
                                break; //todo

                            case ReadyToRunSectionType.AvailableTypes:
                                break; //todo

                            case ReadyToRunSectionType.InstanceMethodEntryPoints:
                                break;

                            case ReadyToRunSectionType.InliningInfo:
                            case ReadyToRunSectionType.ProfileDataInfo:
                            case ReadyToRunSectionType.ManifestMetadata:
                            case ReadyToRunSectionType.AttributePresence:
                            case ReadyToRunSectionType.InliningInfo2:
                            case ReadyToRunSectionType.ComponentAssemblies:
                            case ReadyToRunSectionType.OwnerCompositeExecutable:
                            case ReadyToRunSectionType.PgoInstrumentationData:
                            case ReadyToRunSectionType.ManifestAssemblyMvids:
                            case ReadyToRunSectionType.CrossModuleInlineInfo:
                            case ReadyToRunSectionType.HotColdMap:
                            case ReadyToRunSectionType.MethodIsGenericMap:
                            case ReadyToRunSectionType.EnclosingTypeMap:
                            case ReadyToRunSectionType.TypeGenericInfoMap:
                            default:
                                throw new NotImplementedException();
                        }
                    }
                }

                return data;
            }
        }

        public long Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(int) + //Type
            sizeof(long); //Section

        private readonly MemoryChunk chunk;

        internal ReadyToRunSection(in MemoryChunk chunk)
        {
            this.chunk = chunk;
            data = default;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            var data = Data;

            if (data is IViewable v)
                writer.WriteGlobal(v);
            else if (data is RawValue<FixedAnsiString> s)
            {
                var kind = Type switch
                {
                    ReadyToRunSectionType.CompilerIdentifier => ViewKind.ReadyToRunSection_CompilerIdentifier
                };

                writer.WriteGlobal(s.Offset, s.Value, s.Value.Length, kind);
            }
            else
                throw new NotImplementedException();
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.ReadyToRunSection, StructSize);

        int IViewable.NumChildren() => 2;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(Type), TypeOffset, Type, sizeof(int));
                    break;

                case 1:
                    structWriter.WriteStructField(nameof(Section), Section);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
