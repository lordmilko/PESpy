namespace PESpy
{
    public enum EH_MAGIC_NUMBER
    {
        //The original. I believe this gives you fields up to IPToStateMap
        EH_MAGIC_NUMBER1 = 0x19930520, //Apparently up to VC6

        //Indicates that (fields up to) FuncInfo::psESTypeList is valid
        EH_MAGIC_NUMBER2 = 0x19930521, //Apparently VC7.x (2002-2003)

        //Indicates that (fields up to) FuncInfo::EHFlags is valid
        EH_MAGIC_NUMBER3 = 0x19930522, //Apparently VC8+ (2005)

        //Note that FuncInfo4 does not include a magic number, as it was deemed
        //to take up too much space https://devblogs.microsoft.com/cppblog/making-cpp-exception-handling-smaller-x64/
    }
}
