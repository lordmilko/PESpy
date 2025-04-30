namespace PESpy
{
    public enum IMPORT_OBJECT_NAME_TYPE
    {
        IMPORT_OBJECT_ORDINAL = 0,          // Import by ordinal
        IMPORT_OBJECT_NAME = 1,             // Import name == public symbol name.
        IMPORT_OBJECT_NAME_NO_PREFIX = 2,   // Import name == public symbol name skipping leading ?, @, or optionally _.
        IMPORT_OBJECT_NAME_UNDECORATE = 3,  // Import name == public symbol name skipping leading ?, @, or optionally _ and truncating at first @.
        IMPORT_OBJECT_NAME_EXPORTAS = 4,    // Import name == a name is explicitly provided after the DLL name.
    }
}
