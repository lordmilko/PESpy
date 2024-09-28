namespace PESpy.Native
{
    //ImageEnclaveConfig
    internal unsafe struct IMAGE_ENCLAVE_CONFIG64
    {
        private const int IMAGE_ENCLAVE_SHORT_ID_LENGTH = 16;

        public int Size;
        public int MinimumRequiredConfigSize;
        public IMAGE_ENCLAVE_POLICY PolicyFlags;
        public int NumberOfImports;
        public int ImportList;
        public int ImportEntrySize;
        public fixed byte FamilyID[IMAGE_ENCLAVE_SHORT_ID_LENGTH];
        public fixed byte ImageID[IMAGE_ENCLAVE_SHORT_ID_LENGTH];
        public int ImageVersion;
        public int SecurityVersion;
        public long EnclaveSize;
        public int NumberOfThreads;
        public IMAGE_ENCLAVE_FLAG EnclaveFlags;
    }
}
