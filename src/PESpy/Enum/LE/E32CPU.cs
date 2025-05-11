namespace PESpy.LE
{
    public enum E32CPU : ushort
    {
        /// <summary>
        /// Intel 80286 or upwardly compatibile
        /// </summary>
        E32CPU286 =	0x001,

        /// <summary>
        /// Intel 80386 or upwardly compatibile
        /// </summary>
        E32CPU386 =	0x002,

        /// <summary>
        /// Intel 80486 or upwardly compatibile
        /// </summary>
        E32CPU486 =	0x003

        //Apparently there are other types? https://faydoc.tripod.com/formats/exe-LE.htm
    }
}
