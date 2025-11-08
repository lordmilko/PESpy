namespace PESpy
{
    public enum BlobType
    {
        /* IMPORTANT: Keep the first four enums together in the same order and at
           the very begining of this enum. See MetaModelPub.h for the order */
        MetadataStringPool = 0,
        MetadataGuidPool = 1,
        MetadataBlobPool = 2,
        MetadataUserStringPool = 3,

        FirstMetadataPool = 0,
        LastMetadataPool = 3,

        // SectionFormat only supports tokens, which have to already exist in the module.
        // For instantiated paramterized types, there may be no corresponding token
        // in the module, if a dependent module caused the type to be instantiated.
        // For such instantiated types, we save a blob/signature to identify the type.
        // 
        ParamTypeSpec = 4,    // Instantiated Type Signature
        ParamMethodSpec = 5,    // Instantiated Method Signature
        ExternalNamespaceDef = 6,    // External Namespace Token Definition 
        ExternalTypeDef = 7,    // External Type Token Definition
        ExternalSignatureDef = 8,    // External Signature Definition
        ExternalMethodDef = 9,    // External Method Token Definition

        IllegalBlob = 10,   // Failed to allocate the blob

        EndOfBlobStream = -1
    }
}
