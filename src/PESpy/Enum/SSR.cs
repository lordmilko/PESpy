namespace PESpy
{
    //CxxIL symbol type (from phx.dll)
    public enum SSR : byte
    {
        SSR_UNDEFINED = 0,
        SSR_NAME = 1,
        SSR_ARRAY = 2,
        SSR_LABEL = 3,
        SSR_ENTRY = 4,
        SSR_RETURN = 5,
        SSR_BLOCK = 6,
        SSR_COMEQU = 7,
        SSR_CONSTANT = 8,
        SSR_SEG = 9,
        SSR_TRAILER = 10,
        SSR_TYPEDEF = 11,
        SSR_INFO = 12,
        SSR_START = 13,
        SSR_EXTENTRY = 14,
        SSR_LOOP = 15,
        SSR_WEAKENTRY = 16,
        SSR_HEADER = 17,
        SSR_FILENAME = 18,
        SSR_UPTYPE = 19,
        SSR_RESCOPE = 20,
        SSR_NESTEDENTRY = 21,
        SSR_ILMOD = 22,
        SSR_PDB_IL = 23,
        SSR_WARNING = 24,
        SSR_UNUSED = 25,
        SSR_ALLOTEMP = 26,
        SSR_REFTI = 27
    }
}
