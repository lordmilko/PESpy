using System;
using ClrDebug;

namespace PESpy
{
    //CORCOMPILE_VERSION_INFO
    [Source(SourceKind.corcompile_h)]
    public class CorCompileVersionInfo : IValue //May not be present
    {
        private const int wOSPlatformIDOffset = 0;
        private const int wOSMajorVersionOffset = 2;
        private const int wVersionMajorOffset = 4;
        private const int wVersionMinorOffset = 6;
        private const int wVersionBuildNumberOffset = 8;
        private const int wVersionPrivateBuildNumberOffset = 10;
        private const int wCodegenFlagsOffset = 12;
        private const int wConfigFlagsOffset = 14;
        private const int wBuildOffset = 16;
        private const int wMachineOffset = 18;
        private const int cpuInfoOffset = 20;
        private const int sourceAssemblyOffset = 20 + CorInfoCPU.StructSize;
        private const int signatureOffset = 20 + CorInfoCPU.StructSize + CorCompileAssemblySignature.StructSize;
        private const int runtimeDllInfoOffset = 36 + CorInfoCPU.StructSize + CorCompileAssemblySignature.StructSize;

        //OS

        public short wOSPlatformID => chunk.PeekInt16(wOSPlatformIDOffset);

        public short wOSMajorVersion => chunk.PeekInt16(wOSMajorVersionOffset);

        // EE Version
        // For backward compatibility reasons, the following four fields must start at offset 4,
        // be consequtive, and be 2 bytes each.  See code:PEDecoder::GetMetaDataHelper.

        public short wVersionMajor => chunk.PeekInt16(wVersionMajorOffset);

        public short wVersionMinor => chunk.PeekInt16(wVersionMinorOffset);

        public short wVersionBuildNumber => chunk.PeekInt16(wVersionBuildNumberOffset);

        public short wVersionPrivateBuildNumber => chunk.PeekInt16(wVersionPrivateBuildNumberOffset);

        // Codegen flags

        public short wCodegenFlags => chunk.PeekInt16(wCodegenFlagsOffset);

        public short wConfigFlags => chunk.PeekInt16(wConfigFlagsOffset);

        public short wBuild => chunk.PeekInt16(wBuildOffset);

        // Processor

        public IMAGE_FILE_MACHINE wMachine => (IMAGE_FILE_MACHINE) chunk.PeekUInt16(wMachineOffset);

        public CorInfoCPU cpuInfo => new CorInfoCPU(chunk.Slice(cpuInfoOffset));

        /// <summary>
        /// Signature of source assembly
        /// </summary>
        public CorCompileAssemblySignature sourceAssembly => new CorCompileAssemblySignature(chunk.Slice(sourceAssemblyOffset));

        /// <summary>
        /// Signature which identifies this ngen image
        /// </summary>
        public Guid signature => chunk.PeekGuid(signatureOffset);

        /// <summary>
        /// Timestamp info for runtime dlls
        /// </summary>
        public CorCompileRuntimeDllInfo[] runtimeDllInfo
        {
            get
            {
                var results = new CorCompileRuntimeDllInfo[(int) CorCompileRuntimeDlls.NUM_RUNTIME_DLLS];

                for (var i = 0; i < results.Length; i++)
                    results[i] = new CorCompileRuntimeDllInfo(chunk.Slice(runtimeDllInfoOffset + (i * CorCompileRuntimeDllInfo.StructSize)));

                return results;
            }
        }
        public int Offset => chunk.AbsoluteOffset;

        private readonly MemoryChunk chunk;

        internal CorCompileVersionInfo(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }
    }
}
