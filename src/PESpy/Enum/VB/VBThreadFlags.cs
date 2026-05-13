using System;

namespace PESpy.VB
{
    //Name is made up
    [Flags]
    public enum VBThreadFlags
    {
        /// <summary>
        /// Specifies multi-threading using an apartment model.
        /// </summary>
        ApartmentModel = 0x1,

        /// <summary>
        /// Specifies to do license validation (OCX only).
        /// </summary>
        RequireLicense = 0x2,

        /// <summary>
        /// Specifies that no GUI elements should be initialized.
        /// </summary>
        Unattended = 0x4,

        /// <summary>
        /// Specifies that the image is single-threaded.
        /// </summary>
        SingleThreaded = 0x8,

        /// <summary>
        /// Specifies to keep the file in memory (Unattended only)
        /// </summary>
        Retained = 0x10
    }
}
