namespace PESpy
{
    // Tags for fixup blobs
    public enum CORCOMPILE_FIXUP_BLOB_KIND
    {
        ENCODE_NONE = 0,

        ENCODE_MODULE_OVERRIDE = 0x80,     /* When the high bit is set, override of the module immediately follows */

        ENCODE_DICTIONARY_LOOKUP_THISOBJ = 0x07,
        ENCODE_DICTIONARY_LOOKUP_TYPE = 0x08,
        ENCODE_DICTIONARY_LOOKUP_METHOD = 0x09,

        ENCODE_TYPE_HANDLE = 0x10,     /* Type handle */
        ENCODE_METHOD_HANDLE,                           /* Method handle */
        ENCODE_FIELD_HANDLE,                            /* Field handle */

        ENCODE_METHOD_ENTRY,                            /* For calling a method entry point */
        ENCODE_METHOD_ENTRY_DEF_TOKEN,                  /* Smaller version of ENCODE_METHOD_ENTRY - method is def token */
        ENCODE_METHOD_ENTRY_REF_TOKEN,                  /* Smaller version of ENCODE_METHOD_ENTRY - method is ref token */

        ENCODE_VIRTUAL_ENTRY,                           /* For invoking a virtual method */
        ENCODE_VIRTUAL_ENTRY_DEF_TOKEN,                 /* Smaller version of ENCODE_VIRTUAL_ENTRY - method is def token */
        ENCODE_VIRTUAL_ENTRY_REF_TOKEN,                 /* Smaller version of ENCODE_VIRTUAL_ENTRY - method is ref token */
        ENCODE_VIRTUAL_ENTRY_SLOT,                      /* Smaller version of ENCODE_VIRTUAL_ENTRY - type & slot */

        ENCODE_READYTORUN_HELPER,                       /* ReadyToRun helper */
        ENCODE_STRING_HANDLE,                           /* String token */

        ENCODE_NEW_HELPER,                              /* Dynamically created new helpers */
        ENCODE_NEW_ARRAY_HELPER,

        ENCODE_ISINSTANCEOF_HELPER,                     /* Dynamically created casting helper */
        ENCODE_CHKCAST_HELPER,

        ENCODE_FIELD_ADDRESS,                           /* For accessing a cross-module static fields */
        ENCODE_CCTOR_TRIGGER,                           /* Static constructor trigger */

        ENCODE_STATIC_BASE_NONGC_HELPER,                /* Dynamically created static base helpers */
        ENCODE_STATIC_BASE_GC_HELPER,
        ENCODE_THREAD_STATIC_BASE_NONGC_HELPER,
        ENCODE_THREAD_STATIC_BASE_GC_HELPER,

        ENCODE_FIELD_BASE_OFFSET,                       /* Field base */
        ENCODE_FIELD_OFFSET,

        ENCODE_TYPE_DICTIONARY,
        ENCODE_METHOD_DICTIONARY,

        ENCODE_CHECK_TYPE_LAYOUT,
        ENCODE_CHECK_FIELD_OFFSET,

        ENCODE_DELEGATE_CTOR,

        ENCODE_DECLARINGTYPE_HANDLE,

        ENCODE_MODULE_HANDLE = 0x50,     /* Module token */
        ENCODE_STATIC_FIELD_ADDRESS,                    /* For accessing a static field */
        ENCODE_MODULE_ID_FOR_STATICS,                   /* For accessing static fields */
        ENCODE_MODULE_ID_FOR_GENERIC_STATICS,           /* For accessing static fields */
        ENCODE_CLASS_ID_FOR_STATICS,                    /* For accessing static fields */
        ENCODE_SYNC_LOCK,                               /* For synchronizing access to a type */
        ENCODE_INDIRECT_PINVOKE_TARGET,                 /* For calling a pinvoke method ptr  */
        ENCODE_PROFILING_HANDLE,                        /* For the method's profiling counter */
        ENCODE_VARARGS_METHODDEF,                       /* For calling a varargs method */
        ENCODE_VARARGS_METHODREF,
        ENCODE_VARARGS_SIG,
        ENCODE_ACTIVE_DEPENDENCY,                       /* Conditional active dependency */
        ENCODE_METHOD_NATIVE_ENTRY,                     /* NativeCallable method token */
    }
}
