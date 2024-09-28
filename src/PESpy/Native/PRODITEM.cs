using System.Runtime.InteropServices;

namespace PESpy.Native
{
    //ProdItem
    [StructLayout(LayoutKind.Sequential)]
    internal struct PRODITEM
    {
        /// <summary>
        /// Product identity
        /// </summary>
        public int dwProdid;

        /// <summary>
        /// Count of objects built with that product
        /// </summary>
        public int dwCount;
    }
}
