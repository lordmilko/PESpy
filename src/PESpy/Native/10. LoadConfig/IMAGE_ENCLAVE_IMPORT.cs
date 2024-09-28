namespace PESpy.Native
{
    //ImageEnclaveImport
    internal unsafe struct IMAGE_ENCLAVE_IMPORT
    {
        private const int IMAGE_ENCLAVE_LONG_ID_LENGTH = 32;
        private const int IMAGE_ENCLAVE_SHORT_ID_LENGTH = 16;

        public IMAGE_ENCLAVE_IMPORT_MATCH MatchType;
        public int MinimumSecurityVersion;
        public fixed byte UniqueOrAuthorID[IMAGE_ENCLAVE_LONG_ID_LENGTH];
        public fixed byte FamilyID[IMAGE_ENCLAVE_SHORT_ID_LENGTH];
        public fixed byte ImageID[IMAGE_ENCLAVE_SHORT_ID_LENGTH];
        public int ImportName;
        public int Reserved;
    }
}
