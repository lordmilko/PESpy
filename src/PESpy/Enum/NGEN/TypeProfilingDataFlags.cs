namespace PESpy
{
    public enum TypeProfilingDataFlags
    {
        // Important: update toolbox\ibcmerge\ibcmerge.cs if you change these
        ReadMethodTable = 0,  // 0x00001
        ReadEEClass = 1,  // 0x00002
        WriteEEClass = 2,  // 0x00004
                           //  ReadStoredEnumData            = 3,  // 0x00008  // obsolete
        ReadFieldDescs = 4,  // 0x00010
        ReadCCtorInfo = 5,  // 0x00020
        ReadClassHashTable = 6,  // 0x00040
        ReadDispatchMap = 7,  // 0x00080
        ReadDispatchTable = 8,  // 0x00100
        ReadMethodTableWriteableData = 9,  // 0x00200
        ReadFieldMarshalers = 10, // 0x00400
                                  //  WriteDispatchTable            = 11, // 0x00800  // obsolete
                                  //  WriteMethodTable              = 12, // 0x01000  // obsolete
        WriteMethodTableWriteableData = 13, // 0x02000
        ReadTypeDesc = 14, // 0x04000
        WriteTypeDesc = 15, // 0x08000
        ReadTypeHashTable = 16, // 0x10000
                                //  WriteTypeHashTable            = 17, // 0x20000  // obsolete
                                //  ReadDictionary                = 18, // 0x40000  // obsolete
                                //  WriteDictionary               = 19, // 0x80000  // obsolete
        ReadNonVirtualSlots = 20, // 0x100000
    }
}
