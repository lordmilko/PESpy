using PESpy.View;

namespace PESpy.ViewMap
{
    /// <summary>
    /// Stores information that pertains to a given pixel in the <see cref="ViewMapPanel"/>.
    /// </summary>
    internal unsafe struct ViewMapPixel
    {
        /// <summary>
        /// Gets the target address of the start of the entity that this pixel pertains to.
        /// </summary>
        public long StartAddress;

        /// <summary>
        /// Gets the target address that this pixel represents inside the target entity.
        /// </summary>
        public long PixelAddress;

        /// <summary>
        /// Gets the <see cref="ViewByte"/> that is associated with the <see cref="StartAddress"/> of the entity that this pixel represents.
        /// </summary>
        public ViewByte* pStartViewByte;

        public int SectionAccessorIndex;

        internal ViewMapPixel(long startAddress, long pixelAddress, ViewByte* pStartViewByte, int sectionAccessorIndex)
        {
            StartAddress = startAddress;
            PixelAddress = pixelAddress;
            this.pStartViewByte = pStartViewByte;
            SectionAccessorIndex = sectionAccessorIndex;
        }
    }
}
