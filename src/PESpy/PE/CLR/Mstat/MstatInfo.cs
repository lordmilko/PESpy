using System;
using System.Diagnostics;
using ClrDebug;
using PESpy.Ecma335;
using PESpy.IL;

namespace PESpy.Mstat
{
    //Type is made up; encapsulates the details of https://github.com/dotnet/runtime/blob/1e404e19d5246f5c9c0da4e28a9a0f2345bfee58/src/coreclr/tools/aot/ILCompiler.Compiler/Compiler/MstatObjectDumper.cs
    public class MstatInfo
    {
        /* An mstat is a special type of PEFile
         * 
         * https://github.com/dotnet/runtime/blob/1e404e19d5246f5c9c0da4e28a9a0f2345bfee58/src/coreclr/tools/aot/ILCompiler.Compiler/Compiler/MstatObjectDumper.cs
         * 
         * It contains a .names section full of ser strings, as well as a single <Module> TypeDef containing
         * the following special "methods"
         * 
         * - Methods
         * - Types
         * - Blobs
         * - RvaFields
         * - FrozenObjects
         * - ManifestResources
         * - DeduplicatedMethods
         * 
         * Within each "method", information is encoded via the use of IL
         */

        public Method[] Methods { get; private set; }
        public Type[] Types { get; private set; }
        public Blob[] Blobs { get; private set; }
        public Field[]? Fields { get; private set; }
        public FrozenObject[]? FrozenObjects { get; private set; }
        public ManifestResource[]? ManifestResources { get; private set; }
        public DeduplicatedMethod[]? DeduplicatedMethods { get; private set; }

        private MstatHeap metadata;

        public MstatHeap Metadata => metadata ??= new MstatHeap(_modelHeap, this);

        private ModelHeap _modelHeap;

        internal static unsafe bool TryCreate(PEFile peFile, out MstatInfo info)
        {
            /* mstat version history:
             * 
             * 1.0: included the mangled name directly in each item
             * 1.1: stopped including mangled names
             * 2.0: include an index into .names
             * 2.1: Add RvaFields, FrozenObjects, ManifestResources
             * 2.2: Add DeduplicatedMethods
             */

            info = default;

            if (peFile.IsLoadedImage || peFile.Name == null || !peFile.Name.EndsWith(".mstat", StringComparison.OrdinalIgnoreCase))
                return false;

            var ecmaMetadata = peFile.EcmaMetadata;

            if (ecmaMetadata == null)
                return false;

            var modelHeap = ecmaMetadata.ModelHeap;
            var userStringHeap = ecmaMetadata.UserStringHeap;

            var typeDefs = modelHeap.TypeDefTable;

            if (typeDefs == null || typeDefs.Count != 1 || userStringHeap == null)
                return false;

            var name = typeDefs[0].TypeName.GetString();

            if (name != "<Module>"u8)
                return false;

            var methodDefTable = modelHeap.MethodDefTable;

            var assemblyTable = modelHeap.AssemblyTable[0];

            var majorVersion = assemblyTable.MajorVersion;
            var minorVersion = assemblyTable.MinorVersion;

            info = new MstatInfo
            {
                _modelHeap = modelHeap
            };

            ByteReader names = default;

            if (majorVersion >= 2)
            {
                if (majorVersion > 2)
                    throw new NotImplementedException($"Don't know how to handle MSTAT version '{majorVersion}'");

                //.names section should be present
                var sectionHeaders = peFile.SectionHeaders;

                for (var i = 0; i < sectionHeaders.Length; i++)
                {
                    ref var sectionHeader = ref sectionHeaders[i];

                    if (sectionHeader.Name == ".names"u8)
                    {
                        var memoryBlock = peFile.GetSectionBlock(i, sectionHeader);

                        names = new ByteReader(memoryBlock.LocalPointer, (int) memoryBlock.Length);

                        break;
                    }
                }
            }

            //.names is only present in v2+

            foreach (var methodDef in methodDefTable)
            {
                var ilMethod = methodDef.ILMethod;

                if (ilMethod == null)
                    continue;

                var ilDecoder = ilMethod.Value.IL.Decoder;

                var methodName = methodDef.Name.GetString();
                var span = methodName.AsSpan();

                if (span.SequenceEqual("Methods"u8))
                    info.Methods = ProcessMethods(names, ilDecoder, majorVersion, minorVersion);
                else if (span.SequenceEqual("Types"u8))
                    info.Types = ProcessTypes(names, ilDecoder, majorVersion, minorVersion);
                else if (span.SequenceEqual("Blobs"u8))
                    info.Blobs = ProcessBlobs(ilDecoder, userStringHeap);
                else if (span.SequenceEqual("RvaFields"u8))
                    info.Fields = ProcessRvaFields(names, ilDecoder);
                else if (span.SequenceEqual("FrozenObjects"u8))
                    info.FrozenObjects = ProcessFrozenObjects(names, ilDecoder);
                else if (span.SequenceEqual("ManifestResources"u8))
                    info.ManifestResources = ProcessManifestResources(ilDecoder, userStringHeap);
                else if (span.SequenceEqual("DeduplicatedMethods"u8))
                    info.DeduplicatedMethods = ProcessDeduplicatedMethods(names, ilDecoder);
                else
                {
                    Debug.Assert(false);
                }
            }

            return true;
        }

        private static Method[] ProcessMethods(
            ByteReader names,
            ILDecoder decoder,
            int majorVersion,
            int minorVersion)
        {
            /* Each method consists of the following pattern
             * 
             * - ldtoken <methodToken>
             * - ldc Size
             * - ldc GcInfoSize
             * - ldc MethodEHInfo
             * - ldc mangled name index
             * 
             * As of writing, the current MSTAT version is 2.2, which you can read from the AssemblyTable
             */

            //todo: assemblyname is crashing for the mstat file

            using var list = new ValueList<Method>();

            while (decoder.RemainingBytes > 0)
            {
                var token = decoder.ReadToken();

                if (majorVersion == 1 && minorVersion == 0)
                    throw new NotImplementedException("Handling MSTAT version 1.0 is not implemented"); //There's a UserString here containing the mangled name

                var size = decoder.ReadI4();
                var gcInfoSize = decoder.ReadI4();
                var methodEhInfoSize = decoder.ReadI4();

                FixedUtf8String mangledName = default;

                if (majorVersion >= 2)
                {
                    //Version 2 (added in https://github.com/dotnet/runtime/pull/83578) adds a mangled name index

                    var mangledNameIndex = decoder.ReadI4();

                    mangledName = names.ReadSerString(mangledNameIndex);
                }

                //The data in a given method entry comes from several different sources, so there isn't really a single
                //clear type to "reconstruct" this data as

                list.Add(new Method(token, size, gcInfoSize, methodEhInfoSize, mangledName));
            }

            return list.ToArray();
        }

        private static Type[] ProcessTypes(
            ByteReader names,
            ILDecoder decoder,
            int majorVersion,
            int minorVersion)
        {
            /* This information comes from an EETypeNode
             * 
             * - ldtoken <token>
             * - ldc Length
             * - ldc mangled name index
             */

            using var list = new ValueList<Type>();

            while (decoder.RemainingBytes > 0)
            {
                var token = decoder.ReadToken();

                if (majorVersion == 1 && minorVersion == 0)
                    throw new NotImplementedException("Handling MSTAT version 1.0 is not implemented"); //There's a UserString here containing the mangled name

                var size = decoder.ReadI4();

                FixedUtf8String mangledName = default;

                if (majorVersion >= 2)
                {
                    //This ID has nothing to do with the node ID in the dgml file; to map to that
                    //you have to look the dgml node up by its name
                    var mangledNameIndex = decoder.ReadI4();
                    mangledName = names.ReadSerString(mangledNameIndex);
                }

                list.Add(new Type(token, size, mangledName));
            }

            return list.ToArray();
        }

        private static Blob[] ProcessBlobs(ILDecoder decoder, UserStringHeap userStringHeap)
        {
            //Certain sections were added in MSTAT 2.1; anyone looking at 2.0
            //will instead see these in the blobs section. A blob consists of
            //a user string followed by the size of the blob

            using var list = new ValueList<Blob>();

            while (decoder.RemainingBytes > 0)
            {
                var token = decoder.ReadToken();

                var str = userStringHeap.GetString((mdString) token);
                var size = decoder.ReadI4();

                list.Add(new Blob(str, size));
            }

            return list.ToArray();
        }

        private static Field[] ProcessRvaFields(
            ByteReader names,
            ILDecoder decoder)
        {
            /* - ldtoken <token>
             * - ldc size
             * - ldc mangled name index
             * 
             * Because RvaFields was introduced in 2.1, we don't need to check the assembly version
             */

            using var list = new ValueList<Field>();

            while (decoder.RemainingBytes > 0)
            {
                var token = decoder.ReadToken();
                var size = decoder.ReadI4();
                var mangledNameIndex = decoder.ReadI4();

                var mangledName = names.ReadSerString(mangledNameIndex);

                list.Add(new Field(token, size, mangledName));
            }

            return list.ToArray();
        }

        private static FrozenObject[] ProcessFrozenObjects(
            ByteReader names,
            ILDecoder decoder)
        {
            /* FrozenObjectNode entities are serialized as
             * 
             * - ldtoken <token>
             * - ldc size
             * - ldc mangled name index
             * 
             * If the node is specifically a SerializedFrozenObjectNode,
             * you also get another ldtoken for that type's owning type; else you get an i4 of 0
             * 
             * Because Frozen Objects were introduced in 2.1, we don't need to check the assembly version
             */

            using var list = new ValueList<FrozenObject>();

            while (decoder.RemainingBytes > 0)
            {
                var instanceTypeToken = decoder.ReadToken();
                var size = decoder.ReadI4();
                var mangledNameIndex = decoder.ReadI4();

                var mangledName = names.ReadSerString(mangledNameIndex);

                var instr = decoder.Decode();

                mdToken owningType = default;   

                if (instr.Opcode == ILOpcode.ldtoken)
                {
                    owningType = instr.Token;
                }
                else
                {
                    //It's a ldc of 0
                }

                list.Add(new FrozenObject(instanceTypeToken, size, mangledName, owningType));
            }

            return list.ToArray();
        }

        private static ManifestResource[] ProcessManifestResources(ILDecoder decoder, UserStringHeap userStringHeap)
        {
            /* - ldtoken <token>
             * - ldstr <resourceName>
             * - ldc <size>
             */

            using var list = new ValueList<ManifestResource>();

            while (decoder.RemainingBytes > 0)
            {
                //This is a token, but it's written as a ldc
                var token = (mdToken) decoder.ReadI4();
                var strToken = decoder.ReadToken();

                var str = userStringHeap.GetString((mdString) strToken);

                var size = decoder.ReadI4();

                list.Add(new ManifestResource(token, str, size));
            }

            return list.ToArray();
        }

        private static DeduplicatedMethod[] ProcessDeduplicatedMethods(ByteReader names, ILDecoder decoder)
        {
            //Only present in 2.2, which also means we're guaranteed to have
            //a mangledNameIndex for each item

            using var list = new ValueList<DeduplicatedMethod>();

            while (decoder.RemainingBytes > 0)
            {
                var originalBodyToken = decoder.ReadToken();
                var count = decoder.ReadI4();

                var targets = new (mdToken token, FixedUtf8String mangledName)[count];

                for (var i = 0; i < count; i++)
                {
                    var targetBodyToken = decoder.ReadToken();
                    var targetBodyMangledNameIndex = decoder.ReadI4();

                    var targetBodyMangledName = names.ReadSerString(targetBodyMangledNameIndex);

                    targets[i] = (targetBodyToken, targetBodyMangledName);
                }

                list.Add(new DeduplicatedMethod(originalBodyToken, targets));
            }

            return list.ToArray();
        }

        public struct Method
        {
            public mdToken Token { get; }

            public int Size { get; }

            public int GcInfoSize { get; }

            public int MethodEhInfoSize { get; }

            public FixedUtf8String MangledName { get; }

            internal Method(
                mdToken token,
                int size,
                int gcInfoSize,
                int methodEhInfoSize,
                FixedUtf8String mangledName)
            {
                Token = token;
                Size = size;
                GcInfoSize = gcInfoSize;
                MethodEhInfoSize = methodEhInfoSize;
                MangledName = mangledName;
            }

            public override string ToString()
            {
                return MangledName.ToString();
            }
        }

        public struct Type
        {
            public mdToken Token { get; }

            public int Size { get; }

            public FixedUtf8String MangledName { get; }

            internal Type(mdToken token, int size, FixedUtf8String mangledName)
            {
                Token = token;
                Size = size;
                MangledName = mangledName;
            }

            public override string ToString()
            {
                return MangledName.ToString();
            }
        }

        public struct Blob
        {
            public UserString Name { get; }

            public int Size { get; }

            internal Blob(UserString name, int size)
            {
                Name = name;
                Size = size;
            }

            public override string ToString()
            {
                return Name.ToString();
            }
        }

        public struct Field
        {
            public mdToken Token { get; }

            public int Size { get; }

            public FixedUtf8String MangledName { get; }

            internal Field(mdToken token, int size, FixedUtf8String mangledName)
            {
                Token = token;
                Size = size;
                MangledName = mangledName;
            }

            public override string ToString()
            {
                return MangledName.ToString();
            }
        }

        public struct FrozenObject
        {
            public mdToken InstanceTypeToken { get; }

            public int Size { get; }

            public FixedUtf8String MangledName { get; }

            public mdToken OwningTypeToken { get; }

            internal FrozenObject(mdToken instanceTypeToken, int size, FixedUtf8String mangledName, mdToken owningTypeToken)
            {
                InstanceTypeToken = instanceTypeToken;
                Size = size;
                MangledName = mangledName;
                OwningTypeToken = owningTypeToken;
            }

            public override string ToString()
            {
                return MangledName.ToString();
            }
        }

        public struct ManifestResource
        {
            public mdToken Token { get; }

            public UserString Name { get; }

            public int Size { get; }

            internal ManifestResource(mdToken token, UserString name, int size)
            {
                Token = token;
                Name = name;
                Size = size;
            }

            public override string ToString()
            {
                return Name.ToString();
            }
        }

        [DebuggerDisplay("{Token}")]
        public struct DeduplicatedMethod
        {
            public mdToken OriginalMethodToken { get; }

            public (mdToken token, FixedUtf8String mangledName)[] Targets { get; }

            internal DeduplicatedMethod(mdToken token, (mdToken token, FixedUtf8String mangledName)[] targets)
            {
                OriginalMethodToken = token;
                Targets = targets;
            }
        }
    }
}
